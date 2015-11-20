using System;

namespace ScaleOutMongoDB.ConsoleClient
{
    class Program
    {
        /// <summary>
        /// Will just open a connection to localhost:4502 and write on the topic "say"
        /// </summary>
        /// <param name="args"></param>
        static void Main(string[] args)
        {
            var c = new XSockets.Client40.XSocketClient("ws://localhost:4502", "http://localhost", "monitor");
            c.OnConnected += C_OnConnected;
            c.Open();

            var foo = c.Controller("monitor");
            foo.OnOpen += (s, e) => { Console.WriteLine("Controller Monitor open"); };
            foo.On<double>("temp", (s) => { Console.WriteLine(s); });

            Console.ReadLine();
        }

        private static void C_OnConnected(object sender, EventArgs e)
        {
            Console.WriteLine("MongoDB ScaleOut - 127.0.0.1:4502");
        }
    }
}
