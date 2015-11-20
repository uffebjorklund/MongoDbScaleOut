using System;
using System.IO;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters.Binary;
using XSockets.Core.Common.Utility.Logging;
using XSockets.Plugin.Framework;

namespace ScaleOutMongoDB.Modules.ScaleOut
{
    public static class MongoScaleOutHelpers
    {
        private static string directory = "MongoScaleOut";
        private static string uniqueName = string.Format("{0}\\ServerId.bin", directory);

        static MongoScaleOutHelpers()
        {
            if (!Directory.Exists(directory))
                Directory.CreateDirectory(directory);
        }
        public static ServerInfo GetServerInfo()
        {
            return ReadServerInfo();
        }

        public static void SetServerInfo(ServerInfo si)
        {
            WriteServerInfo(si);
        }

        private static void WriteServerInfo(ServerInfo si)
        {

            using (var fs = new FileStream(uniqueName, FileMode.OpenOrCreate, FileAccess.ReadWrite))
            {
                try
                {
                    var formatter = new BinaryFormatter();
                    formatter.Serialize(fs, si);
                }
                catch (SerializationException e)
                {
                    Composable.GetExport<IXLogger>().Error(e, "Failed to serialize. Reason: {e}" + e.Message);
                }
            }
        }

        private static ServerInfo ReadServerInfo()
        {
            ServerInfo i = null;
            if (!File.Exists(uniqueName))
            {
                WriteServerInfo(new ServerInfo { ServerId = Guid.NewGuid().ToString(), DocumentId=null });
                return ReadServerInfo();
            }

            using (var fs = new FileStream(uniqueName, FileMode.Open, FileAccess.Read))
            {
                try
                {
                    var formatter = new BinaryFormatter();
                    i = ((ServerInfo)formatter.Deserialize(fs));
                }
                catch (SerializationException e)
                {
                    Composable.GetExport<IXLogger>().Error(e, "Failed to deserialize. Reason: {e}" + e.Message);
                }
            }
            return i;
        }
    }
}
