using LiteNetLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using LiteNetLib.Utils;
using UnityEngine;
namespace console_hook
{
    public class NetworkHander
    {

        public static void Receive(NetPeer peer, NetPacketReader reader, byte channel, DeliveryMethod deliveryMethod)
        {
            var command = reader.GetString();
            switch (command)
            {
                case "SPAWN_CUBE":
                    {
                        var c = GameObject.CreatePrimitive(PrimitiveType.Cube);
                        c.transform.position = Camera.main.transform.position;
                        c.AddComponent<Rigidbody>();
                        NetDataWriter writer = new NetDataWriter();
                        writer.Put("SPAWNED_CUBE");
                        Core.serverConnection.Send(writer, DeliveryMethod.ReliableOrdered);
                        break;
                    }
            }

            reader.Recycle();
        }

    }
}
