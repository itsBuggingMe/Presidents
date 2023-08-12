using common;
using Lidgren.Network;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading.Tasks;

namespace client
{
    internal class client
    {
        private void tickGame()
        {
            //TODO: write game tick
        }

        private void initialise(initalisationPacket initalPacket)
        {
            //TODO: write initalisation code

        }

        public void draw(SpriteBatch spriteBatch, GraphicsDevice graphicsDevice)
        {
            //TODO: write draw code

            if (!connected)
            {
                graphicsDevice.Clear(Color.DarkRed);
            }
            if (lobby)
            {
                graphicsDevice.Clear(Color.CornflowerBlue);
            }
            if (initalised)
            {
                graphicsDevice.Clear(Color.Green);
            }
        }



        NetClient _client;

        string appName;
        string name;
        int id;
        int playerCount;

        //TODO: update player threshold
        const int playerThreshold = 2;

        string ip;
        int port;

        bool lobby;

        List<string> packetBuffer = new List<string>();

        //connect -> start
        bool connected = false;
        bool initalised = false;

        int ticks = 0;

        DateTime lastServerPing;
        public client(string ip, int port, string appname)
        {
            this.ip = ip; this.port = port;
            this.appName = appname;

            restartConnection();
        }

        public void tick()
        {
            ticks++;
            readMessages();
            if(connected)
            {
                checkConnections();
                if(lobby)
                {
                    if(Keyboard.GetState().IsKeyDown(Keys.Enter) && playerCount >= playerThreshold)
                    {
                        sendMessage($"$S:{id}");
                    }

                    for (int i = packetBuffer.Count - 1; i >= 0; i--)
                    {
                        if (packetEncodeDecode.tryDecodeStart(packetBuffer[i], out initalisationPacket initalPacket))
                        {
                            initialise(initalPacket);
                            initalised = true;
                            packetBuffer.RemoveAt(i);
                            break;
                        }
                    }
                }
                else
                {
                    if(playerThreshold > playerCount)
                    {
                        lobby = true;
                        initalised = false;
                        return;
                    }
                    if(!initalised)
                    {
                        tickGame();
                    }
                    else
                    {
                        for (int i = packetBuffer.Count - 1; i >= 0; i--)
                        {
                            if (packetEncodeDecode.tryDecodeStart(packetBuffer[i], out Random a))
                            {
                                initalised = true;
                                packetBuffer.RemoveAt(i);
                                break;
                            }
                        }
                    }
                }
                
                return;
            }

            if(ticks % 10 == 0)
            {
                connectToServer();
            }
            foreach(string message in packetBuffer)
            {
                if(packetEncodeDecode.tryDecodeID(message, out int id))
                {
                    this.id = id;
                    connected = true;
                    lastServerPing = DateTime.Now;
                }
            }
        }

        public void connectToServer()
        {
            if (ip == "localhost")
            {
                _client.Connect(ip, port);
            }
            if (IPAddress.TryParse(ip, out IPAddress ipAddress))
            {
                _client.Connect(ip, port);
            }
        }
        private void checkConnections()
        {
            for(int i = packetBuffer.Count() - 1; i >= 0; i--)
            {
                if (packetEncodeDecode.tryDecodeUpdate(packetBuffer[i], out int playerCount, out bool lobby))
                {
                    this.lobby = lobby;
                    packetBuffer.RemoveAt(i);
                    lastServerPing = DateTime.Now;
                    this.playerCount = playerCount;
                }
            }

            if (DateTime.Now - lastServerPing > TimeSpan.FromSeconds(2))
            {
                restartConnection();
                return;
            }
            if(ticks % 10 == 0)
            {
                sendMessage(packetEncodeDecode.encodeID(id));
            }
        }

        public void sendMessage(string message)
        {
            NetOutgoingMessage sendMsg = _client.CreateMessage();
            sendMsg.Write(message);
            _client.SendMessage(sendMsg, NetDeliveryMethod.ReliableOrdered);
            _client.FlushSendQueue();
        }


        public void readMessages()
        {
            NetIncomingMessage incMsg;
            while ((incMsg = _client.ReadMessage()) != null)
            {
                if(incMsg.MessageType == NetIncomingMessageType.Data)
                {
                    packetBuffer.Add(incMsg.ReadString());
                }

                _client.Recycle(incMsg);
            }
        }

        public void restartConnection()
        {
            NetPeerConfiguration config = new NetPeerConfiguration(appName);
            config.AutoFlushSendQueue = false;

            _client = new NetClient(config);
            _client.Start();
        }
    }
}
