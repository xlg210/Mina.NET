using System;
using Mina.Core.Buffer;
using Mina.Transport.File;

namespace FileReader
{
    internal class Program
    {
        private static void Main(string[] args)
        {
            var connector = new FileConnector(new FileSessionConfig() { BothIdleTime =20});
            connector.MessageReceived += Connector_MessageReceived;
            connector.SessionCreated += Connector_SessionCreated;
            connector.SessionOpened += Connector_SessionOpened;
            connector.SessionIdle += Connector_SessionIdle;
            connector.SessionConfig.BothIdleTime = 10;
            connector.Connect(new FileEndPoint("ams", @"F:\AmsData\1.txt"));
            Console.Read();
        }

        private static void Connector_SessionIdle(object sender, Mina.Core.Session.IoSessionIdleEventArgs e)
        {
            Console.WriteLine("Connector_SessionIdle");
        }

        private static void Connector_SessionOpened(object sender, Mina.Core.Session.IoSessionEventArgs e)
        {
            Console.WriteLine("Connector_SessionOpened");
        }

        private static void Connector_SessionCreated(object sender, Mina.Core.Session.IoSessionEventArgs e)
        {
            Console.WriteLine("Connector_SessionCreated");
        }

        private static void Connector_MessageReceived(object sender, Mina.Core.Session.IoSessionMessageEventArgs e)
        {
            var msg = e.Message as IoBuffer;
            Console.WriteLine(msg.GetHexDump());
        }
    }
}