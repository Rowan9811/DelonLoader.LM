using LiteNetLib;
using LiteNetLib.Utils;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
//using MelonLoader;


namespace console_hook
{
    public class Core
    {

        public static NetManager client;
        public static NetPeer serverConnection;
        public static void ints()
        {
            EventBasedNetListener listener = new EventBasedNetListener();
            client = new NetManager(listener);
            client.Start();
            //client.Connect("localhost" /* host ip or name */, 9050 /* port */, "DL" /* text key or NetDataWriter */);
            listener.NetworkReceiveEvent += NetworkHander.Receive;

            // todo stop using melon load dependency
            //MelonCoroutines.Start(DiscoverServer()); 
            listener.NetworkReceiveUnconnectedEvent += (endPoint, reader, messageType) =>
            {
                if (reader.TryGetString(out string data) && data == "DLSV")
                {
                    //FusionLogger.Log("Found the proxy server!");
                    client.Connect(endPoint, "DL");
                }

                reader.Recycle();
            };

        }

        public static void stop()
        {
            client.Stop();
        }


        public void up()
        {
            client.PollEvents();
        }

        public static IEnumerator DiscoverServer()
        {
            int port = 28340;

            float timeElapsed;

            NetDataWriter writer = new NetDataWriter();
            writer.Put("DL_DISCOVERY");

            while (serverConnection == null)
            {
                var ts = DateTime.Now.Second;
                timeElapsed = 0;
                client.SendBroadcast(writer, port);

                while (timeElapsed < 5)
                {
                    timeElapsed += DateTime.Now.Second - ts;
                    yield return null;
                }
            }
        }
    }
}
