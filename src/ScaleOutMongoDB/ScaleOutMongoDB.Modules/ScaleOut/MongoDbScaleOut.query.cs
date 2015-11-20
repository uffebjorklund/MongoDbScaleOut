using MongoDB.Bson;
using MongoDB.Driver;
using System;
using System.Linq;
using System.Threading.Tasks;
using XSockets.Core.Common.Socket;
using XSockets.Core.Common.Socket.Event.Interface;
using XSockets.Core.Common.Utility.Logging;
using XSockets.Core.XSocket.Model;
using XSockets.Enterprise.Scaling;
using XSockets.Plugin.Framework;
using XSockets.Protocol;

namespace ScaleOutMongoDB.Modules.ScaleOut
{
    public partial class MongoScaleOut : BaseScaleOut
    {
        private void SetupServerInfo()
        {
            //Get the serverInfo
            SI = MongoScaleOutHelpers.GetServerInfo();

            //Setup the last document id
            if (string.IsNullOrEmpty(SI.DocumentId))
                this.lastId = BsonMinKey.Value;
            else
                this.lastId = new BsonObjectId(new ObjectId(SI.DocumentId));
        }

        private void SetupDatabase()
        {
            //MongoDB connection
            mongoClient = new MongoClient("mongodb://127.0.0.1");
            db = mongoClient.GetDatabase("xscaleout");
        }
        private void EnsureCappedCollection()
        {
            //Create collection
            //Weird way to see if a collection exists, do let me know if there is a better way!
            if (!CollectionExistsAsync("messages").Result)
            {
                db.CreateCollectionAsync("messages", new CreateCollectionOptions { Capped = true, MaxDocuments = 100, MaxSize = 10000 }).Wait();
                //Add init message, needed for the capped awaitable colleciton to work
                var document = new BsonDocument
                    {
                        { "sid" , this.SI.ServerId },
                        { "data", new BsonDocument()
                                        {
                                            { "t","init" },
                                            { "d","" },
                                            { "c","fake" }
                                        }
                        }
                    };
                //Set the capped colletion
                collection = db.GetCollection<BsonDocument>("messages");
                collection.InsertOneAsync(document);
            }
            else
            {
                collection = db.GetCollection<BsonDocument>("messages");
            }
        }

        private async Task<bool> CollectionExistsAsync(string collectionName)
        {
            var filter = new BsonDocument("name", collectionName);
            //filter by collection name
            var collections = await db.ListCollectionsAsync(new ListCollectionsOptions { Filter = filter });
            //check for existence
            return (await collections.ToListAsync()).Any();
        }        

        private async Task ScaleToMongoDb(IMessage message)
        {
            //Create doc with serverid so that we only send this to other servers
            var document = new BsonDocument
                {
                    { "sid" , this.SI.ServerId },
                    { "data", new BsonDocument()
                                    {
                                        { "t",message.Topic },
                                        { "d",message.Data },
                                        { "c",message.Controller }
                                    }
                    }
                };

            //Insert into mongo capped collection
            await collection.InsertOneAsync(document);
        }        

        /// <summary>
        /// Get documents continously form mongodb as messages are scaled
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="collection"></param>
        /// <returns></returns>
        private async Task Watch<T>(IMongoCollection<T> collection) where T : class
        {
            try
            {

                while (true)
                {
                    var builder = Builders<T>.Filter;
                    //Do not get message that we send from this server (only SID that is not equal to this SID)
                    var filter = builder.Gt("_id", lastId) & builder.Ne("sid", this.SI.ServerId);
                    FindOptions<T> options = new FindOptions<T>
                    {
                        CursorType = CursorType.TailableAwait,
                        //Take desc to only get the latest and not all from the first.
                        Sort = Builders<T>.Sort.Ascending("$natural")                        
                    };
                    
                    using (var cursor = await collection.FindAsync(filter, options))
                    {
                        while (await cursor.MoveNextAsync())
                        {
                            var batch = cursor.Current;
                            foreach (var document in batch)
                            {
                                lastId = document.ToBsonDocument()["_id"];
                                ProcessDocument(document);
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Composable.GetExport<IXLogger>().Error(ex, "Failed to query data from MongoDB ScaleOut");
                Watch(collection);
            }
        }

        /// <summary>
        /// When a message arrives deserialize it to a IMessage
        /// Then dispatch the message to the correct controller
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="document"></param>
        private void ProcessDocument<T>(T document) where T : class
        {
            try
            {
                var json = document.ToBsonDocument()["data"].ToJson();
                Composable.GetExport<IXLogger>().Debug("Message Arrived {@m}", json);
                var m = this.Serializer.DeserializeFromString<Message>(json);
                //Check if it was the init message in the capped collection
                if (m.Controller == "fake" && m.Topic == "init") return;
                var pipe = Composable.GetExport<IXSocketPipeline>();
                var ctrl = Composable.GetExports<IXSocketController>().First(p => p.Alias == m.Controller);
                ctrl.ProtocolInstance = new XSocketInternalProtocol();
                pipe.OnIncomingMessage(ctrl, m);

            }
            catch (Exception ex)
            {
                Composable.GetExport<IXLogger>().Error(ex, "Failed to process data from MongoDB ScaleOut");
            }
        }
    }
}

