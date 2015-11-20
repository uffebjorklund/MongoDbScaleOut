using XSockets.Core.XSocket;
using XSockets.Core.XSocket.Helpers;
using System.Threading.Tasks;

namespace ScaleOutMongoDB.Modules
{
    public class Sensor : XSocketController
    {
        public async Task Temp(double v)
        {
            await this.InvokeToAll<Monitor>(v, "temp");
        }
    }
}
