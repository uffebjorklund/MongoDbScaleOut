using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using System;

namespace ScaleOutMongoDB.Modules.ScaleOut
{
    [Serializable]
    public class ServerInfo
    {
        public string ServerId { get; set; }

        //[BsonId]
        //[BsonRepresentation(BsonType.ObjectId)]
        public string DocumentId { get; set; }
    }
}
