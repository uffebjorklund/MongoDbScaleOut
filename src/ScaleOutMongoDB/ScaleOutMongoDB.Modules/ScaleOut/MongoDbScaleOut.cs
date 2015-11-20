using MongoDB.Bson;
using MongoDB.Driver;
using System;
using System.Threading.Tasks;
using XSockets.Core.Common.Enterprise;
using XSockets.Core.Common.Protocol;
using XSockets.Core.Common.Socket.Event.Interface;
using XSockets.Core.Common.Utility.Logging;
using XSockets.Enterprise.Scaling;
using XSockets.Plugin.Framework;
using XSockets.Plugin.Framework.Attributes;

namespace ScaleOutMongoDB.Modules.ScaleOut
{
    [Export(typeof(IXSocketsScaleOut), Rewritable = Rewritable.No, InstancePolicy = InstancePolicy.Shared)]
    public partial class MongoScaleOut : BaseScaleOut
    {
        /// <summary>
        /// To know if the setup was a success
        /// </summary>
        private bool InitSuccess;

        /// <summary>
        /// For being able to filter so that only messages from other servers arrive
        /// </summary>
        private ServerInfo SI;

        /// <summary>
        /// The id of the last message received from the ScaleOut
        /// </summary>
        private BsonValue lastId;

        private MongoClient mongoClient;

        private IMongoDatabase db;

        /// <summary>
        /// The capped collection to monitor
        /// </summary>
        private IMongoCollection<BsonDocument> collection;

        /// <summary>
        /// Update the lastId so that we only get new messages when restarting
        /// </summary>
        ~MongoScaleOut()
        {
            SI.DocumentId = lastId.AsBsonValue.ToString();
            MongoScaleOutHelpers.SetServerInfo(this.SI);
        }
        
        /// <summary>
        /// Called at startup, setup/prepare your scaleout
        /// </summary>
        public override void Init()
        {
            try
            {
                SetupServerInfo();

                SetupDatabase();

                EnsureCappedCollection();

                InitSuccess = true;               
            }
            catch (Exception ex)
            {
                Composable.GetExport<IXLogger>().Error(ex, "Failed to initialize MongoDB ScaleOut");
            }
        }        

        /// <summary>
        /// Publish the message to the scaleout so that other servers can receive it
        /// </summary>
        /// <param name="message"></param>
        public override async Task Publish(MessageDirection direction, IMessage message, ScaleOutOrigin scaleOutOrigin)
        {
            if (!InitSuccess) return;
            try
            {
                await ScaleToMongoDb(message);
            }
            catch (Exception ex)
            {
                Composable.GetExport<IXLogger>().Error(ex, "Failed to publish in MongoDB ScaleOut");
            }
        }        

        /// <summary>
        /// Subscribe for messages published from other servers by using AzureServiceBus or similar techniques
        /// 
        /// You can ofcourse do polling to a data source, but performance will be suffering from that.
        /// </summary>
        public override async Task Subscribe()
        {
            if (!InitSuccess) return;
            //Setup listener for new docs from other servers            
            await Watch<BsonDocument>(collection);
        }        
    }
}

