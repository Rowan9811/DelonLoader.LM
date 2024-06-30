using LiteNetLib;
using LiteNetLib.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace DelonLoader_Console_app_Windows
{
    internal class Program
    {
        public static bool _hasBeenDiscovered;
        public static NetManager server;
        static Thread upThread = up();

        static Thread up()
        {
            while (true)
            {
                server.PollEvents();
                Thread.Sleep(15);
            }
        }
        static Thread comThread = com();

        public static void Receive(NetPeer peer, NetPacketReader reader, byte channel, DeliveryMethod deliveryMethod)
        {
            var command = reader.GetString();
            switch (command)
            {
                case "SPAWNED_CUBE":
                    {
                        Console.WriteLine("Spawned Cube!");
                        break;
                    }
                case "ERORR":
                    {
                        Console.WriteLine("an error has occurred");
                        break;
                    }
            }

            reader.Recycle();
        }

        static Thread com()
        {
            while (true)
            {
                string command = Console.ReadLine();
                if (command != null)
                {
                    switch (command)
                    {
                        case "spawn ":
                        case "spawn":
                            {
                                Console.WriteLine("Please put in a type");
                                break;
                            }
                        case "spawn cube":
                            {
                                Console.WriteLine("trying to spawn Cube");
                                NetDataWriter writer = new NetDataWriter();                 
                                writer.Put("SPAWN_CUBE");
                                server.FirstPeer.Send(writer, DeliveryMethod.ReliableOrdered);             
                                break;
                            }
                        case "quit":
                        case "exit":
                        case "stop":
                            {
                                server.Stop();
                                Environment.Exit(0);
                                break;
                            }
                        default:
                            Console.WriteLine($"invalid command ({command})");
                            break;
                    }
                }
            }
        }
        static void Main(string[] args)
        {
            
            EventBasedNetListener listener = new EventBasedNetListener();
            server = new NetManager(listener);
            listener.NetworkReceiveUnconnectedEvent += (endPoint, reader, messageType) =>
            {
                if (_hasBeenDiscovered) return;

                if (reader.TryGetString(out string data) && data == "DL_DISCOVERY")
                {
                    Console.WriteLine("Client has found the server");
                    NetDataWriter writer = new NetDataWriter();
                    writer.Put("DLSV");
                    server.SendUnconnectedMessage(writer, endPoint);
                    _hasBeenDiscovered = true;
                }
            };
            server.Start(28340 /* port */);

            listener.ConnectionRequestEvent += request =>
            {
                if (server.ConnectedPeersCount < 1 /* max connections */)
                    request.AcceptIfKey("DL");
                else
                    request.Reject();
            };

            listener.PeerConnectedEvent += peer =>
            {
                Console.WriteLine($"We got connection: {peer.Address}"); // Show peer ip
                //NetDataWriter writer = new NetDataWriter();                 // Create writer class
                //writer.Put("Hello client!");                                // Put some string
                //peer.Send(writer, DeliveryMethod.ReliableOrdered);             // Send with reliability
                
            };

            listener.NetworkReceiveEvent += Receive;

            upThread.Start();
            comThread.Start();

        }

       

    }
}
