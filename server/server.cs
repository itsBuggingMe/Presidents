using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using Lidgren.Network;
using common;
using System.Security.Cryptography;
using System.Timers;
using System.Diagnostics.Contracts;

namespace server
{
    internal class server
    {
        private void tick()
        {
            if (lobby)
            {
                for (int i = packets.Count - 1; i >= 0; i--)
                {
                    if (packets[i][0] == '$' && packets[i][1] == 'S')
                    {
                        _console.write(0, "Game Started");
                        lobby = false;
                        initalise();
                        break;
                    }
                }
            }
            else
            {
                //TODO: write game tick
            }
        }

        private void initalise()
        {
            //TODO: write initalisation

            initalisationPacket packet;
            sendMessageAll(packetEncodeDecode.encodeStart(packet));
        }


        private NetServer _server;

        private List<(NetConnection connection, int playerIds, string name, DateTime lastPing)> connectedPlayers = new List<(NetConnection connection, int playerIds, string name, DateTime lastPing)>();
        private List<(string IP, int playerIds, string name)> disconnectedPlayers = new List<(string IP, int playerIds, string name)>();
        private consoleHelper _console;
        private Random _random = new Random();

        List<string> packets = new List<string>();
        int ticks = 0;
        int port;

        int playerThreshold = 2;

        bool lobby = true;

        public server(int port, string appName) 
        {
            NetPeerConfiguration config = new NetPeerConfiguration(appName);
            config.Port = port;
            this.port = port;
            _server = new NetServer(config);

            _console = new consoleHelper();
            _console.add(42, ConsoleColor.Red);//important notifs
            _console.add(6, ConsoleColor.White);//preformance
            _console.add(6, ConsoleColor.Yellow);//generic update
            _console.add(30, ConsoleColor.Blue);//incoming
            _console.add(30, ConsoleColor.Green);//outgoing
        }

        public void start()
        {
            _server.Start();
            _console.write(0, $"Server Started on Port {port}");
            DateTime lastTickTime = DateTime.Now;
            TimeSpan tickLength = TimeSpan.FromMilliseconds(100/6);
            while (true)
            {
                TimeSpan extraTime = DateTime.Now - lastTickTime;
                float percentCapacity = (float)Math.Round((extraTime / tickLength), 4);
                _console.write(1, string.Format("{0:P}", percentCapacity));
                while (DateTime.Now - lastTickTime < tickLength) 
                { Thread.Sleep(0); }
                lastTickTime = DateTime.Now;
                ticks++;
                readMessages();
                kickDisconnected();

                tick();
            }
        }




        private void kickDisconnected()
        {
            //set my last tick time
            for(int i = packets.Count - 1; i >= 0; i--)
            {
                if (packetEncodeDecode.tryDecodeID(packets[i], out int id))
                {
                    int index = findIndexFromID(id);
                    var tuple = (connectedPlayers[index].connection, connectedPlayers[index].playerIds, connectedPlayers[index].name, DateTime.Now);
                    connectedPlayers[index] = tuple;
                    packets.RemoveAt(i);
                }
            }
            //remove disconnections
            for (int i = connectedPlayers.Count - 1; i >= 0; i--)
            {
                if (DateTime.Now - connectedPlayers[i].lastPing > TimeSpan.FromSeconds(2))
                {
                    _console.write(0, $"Player {connectedPlayers[i].connection} Disconnected: 12 unresponded pings");
                    connectedPlayers.Remove(connectedPlayers[i]);

                    if(connectedPlayers.Count < playerThreshold)
                    {
                        lobby = true;
                    }
                    /*
                    if (lobby)
                    {

                    }
                    else
                    {
                        _console.write(0, $"Player {connectedPlayers[i].connection} Disconnected: 12 unresponded pings");
                        _console.write(0, $"Placed on disconnect list");
                        var tuple = (connectedPlayers[i].connection.ToString().Split(":")[0], connectedPlayers[i].playerIds, connectedPlayers[i].name);
                        disconnectedPlayers.Add(tuple);

                        connectedPlayers.Remove(connectedPlayers[i]);
                    }*/
                }
            }

            if (ticks % 10 == 0)
            {
                sendMessageAll(packetEncodeDecode.encodeUpdate(connectedPlayers.Count, lobby));
            }
        }



        private void readMessages()
        {
            NetIncomingMessage incomingMessage;

            while ((incomingMessage = _server.ReadMessage()) != null)
            {
                if(incomingMessage.MessageType == NetIncomingMessageType.Data)
                {
                    string msg = incomingMessage.ReadString();
                    packets.Add(msg);
                    _console.write(3, msg);
                }
                else if(incomingMessage.MessageType == NetIncomingMessageType.StatusChanged)
                {
                    NetConnectionStatus status = (NetConnectionStatus)incomingMessage.ReadByte();

                    if (status == NetConnectionStatus.Connected && lobby)
                    {
                        int id = _random.Next();
                        _console.write(0, $"Player {incomingMessage.SenderConnection} Joined with ID: {id}");

                        var tupleConnection = (incomingMessage.SenderConnection, id, "", DateTime.Now);
                        connectedPlayers.Add(tupleConnection);

                        sendMessage(packetEncodeDecode.encodeID(id), incomingMessage.SenderConnection);
                    }
                    /*
                    else if(status == NetConnectionStatus.Connected)
                    {
                        string ipOfJoiner = incomingMessage.SenderConnection.ToString().Split(":")[0];
                        for (int i = 0; i < disconnectedPlayers.Count; i++)
                        {
                            if (disconnectedPlayers[i].IP == ipOfJoiner)
                            {
                                _console.write(0, $"Player {incomingMessage.SenderConnection} Reconnected with ID: {disconnectedPlayers[i].playerIds}");

                                var tupleConnection = (incomingMessage.SenderConnection, disconnectedPlayers[i].playerIds, disconnectedPlayers[i].name, DateTime.Now);
                                connectedPlayers.Add(tupleConnection);

                                disconnectedPlayers.RemoveAt(i);

                                break;
                            }
                        }
                    }*/
                }
                _server.Recycle(incomingMessage);
            }
        }

        private void sendMessageAll(string message)
        {
            NetOutgoingMessage outgoingMessage;
            _console.write(4, message);
            for (int i = 0; i < connectedPlayers.Count; i++)
            {
                outgoingMessage = _server.CreateMessage();
                outgoingMessage.Write(message);
                _server.SendMessage(outgoingMessage, connectedPlayers[i].connection, NetDeliveryMethod.ReliableOrdered);
            }
            _server.FlushSendQueue();
        }

        private void sendMessage(string message, NetConnection connection)
        {
            _console.write(4, message);

            NetOutgoingMessage outgoingMessage = _server.CreateMessage();
            outgoingMessage.Write(message);
            _server.SendMessage(outgoingMessage, connection, NetDeliveryMethod.ReliableOrdered);
            _server.FlushSendQueue();
        }

        private int findIndexFromID(int id)
        {
            for(int i = 0; i < connectedPlayers.Count; i++)
            {
                if (connectedPlayers[i].playerIds == id)
                {
                    return i;
                }
            }
            _console.write(0, "Packet mismatch");
            return -1;
        }
    }

    public class consoleHelper
    {
        List<row> rows = new List<row>();

        public consoleHelper()
        {
        }

        public void add(int width)
        {
            int prevLines = 0;
            for (int i = 0; i < rows.Count; i++)
            {
                prevLines += rows[i].width + 1;
            }

            rows.Add(new row(width, prevLines, ConsoleColor.White));
        }

        public void add(int width, ConsoleColor color)
        {
            int prevLines = 0;
            for (int i = 0; i < rows.Count; i++)
            {
                prevLines += rows[i].width + 1;
            }

            rows.Add(new row(width, prevLines, color));
        }

        public void write(int row, string text)
        {
            rows[row].write(text);
        }

        internal class row
        {
            int alreadyWritten;
            int startW;
            public int width;
            string clear = "";
            ConsoleColor color;
            public row(int width, int start)
            {
                this.width = width;
                this.startW = start;
                alreadyWritten = 0;

                for (int i = 0; i < width; i++)
                {
                    clear += " ";
                }
            }

            public row(int width, int start, ConsoleColor color)
            {
                this.width = width;
                this.startW = start;
                alreadyWritten = 0;

                for (int i = 0; i < width; i++)
                {
                    clear += " ";
                }
                this.color = color;
            }
            public void write(string text)
            {
                Console.ForegroundColor = color;
                List<string> lines = new List<string>();
                while (text.Length > width)
                {
                    lines.Add(text.Substring(0, width));
                    text = text.Substring(width);
                }
                lines.Add(text);

                for (int i = 0; i < lines.Count; i++)
                {
                    Console.SetCursorPosition(startW, alreadyWritten);
                    Console.Write(lines[i]);
                    alreadyWritten++;
                    alreadyWritten = alreadyWritten % (Console.BufferHeight - 1);
                    Console.SetCursorPosition(startW, alreadyWritten);
                    Console.Write(clear);
                }
            }
        }
    }
}
