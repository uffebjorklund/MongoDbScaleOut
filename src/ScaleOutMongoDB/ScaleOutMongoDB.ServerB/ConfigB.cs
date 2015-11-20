using XSockets.Core.Configuration;

namespace ScaleOutMongoDB.ServerB
{
    public class ConfigB : ConfigurationSetting
    {
        public ConfigB() : base("ws://127.0.0.1:4503") { }
    }
}
