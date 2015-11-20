using XSockets.Core.Configuration;

namespace ScaleOutMongoDB.ServerB
{
    public class ConfigA : ConfigurationSetting
    {
        public ConfigA() : base("ws://127.0.0.1:4502") { }
    }
}
