using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Reflection.Emit;
using System.Text;
using System.Threading;
using NetworkCommsDotNet;
using NetworkCommsDotNet.Connections;
using UnityEngine;
namespace console_hook_working
{
    public class console
    {

        public static console Console;
        public Thread update;
        ConnectionInfo targetServerConnectionInfo;
        public console()
        {
            update = new Thread(Update);
            update.Start();
        }

        public void Update()
        {
            while (true) 
            {



                Thread.Sleep(15);
            }
        }

        public void IncomingPacket(PacketHeader packetHeader, Connection connection, string command)
        {
            switch (command)
            {
                case "SPAWN_CUBE":
                    {
                        var c = GameObject.CreatePrimitive(PrimitiveType.Cube);
                        c.transform.position = Camera.main.transform.position;
                        c.AddComponent<Rigidbody>();
                        NetworkComms.SendObject("Message",
               ((IPEndPoint)targetServerConnectionInfo.RemoteEndPoint).Address.ToString(),
               ((IPEndPoint)targetServerConnectionInfo.RemoteEndPoint).Port,
               "SPAWNED_CUBE");
                        //NetDataWriter writer = new NetDataWriter();
                        //writer.Put("SPAWNED_CUBE");
                        //Core.serverConnection.Send(writer, DeliveryMethod.ReliableOrdered);
                        break;
                    }
            }

        }

        public static void ints()
        {
            Console = new console();
            NetworkComms.AppendGlobalIncomingPacketHandler<string>("command", Console.IncomingPacket);
            Console.targetServerConnectionInfo = NetworkComms.;
        }
    }
}
