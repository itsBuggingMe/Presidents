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
using System.Net.Mail;

namespace client
{
    internal class client
    {
        banner banner;
        byte whichButtonPressed;
        display display;
        private void tickGame(bool focus)
        {
            MouseState mouseState = Mouse.GetState();
            //TODO: write game tick
            /*
            string bannerMSG = "";
            if (focus)
            {
                move? movea = thisPlayer.update(out bannerMSG);

                if (!string.IsNullOrEmpty(bannerMSG))
                {
                    banner.addMsg(20, bannerMSG);
                    whichButtonPressed = 0;
                    if (bannerMSG == "pass" && mouseState.LeftButton == ButtonState.Pressed)
                    {
                        whichButtonPressed = 1;
                    }
                    if (bannerMSG == "go" && mouseState.LeftButton == ButtonState.Pressed)
                    {
                        whichButtonPressed = 2;
                    }
                }
            }*/
            display.alert = 0;
            for (int i = packetBuffer.Count - 1; i >= 0; i--)
            {
                if (packetEncodeDecode.tryDecodeObject(packetBuffer[i], out int playerID, out gameState obj, "state") && playerID == 0)
                {
                    thisPlayer.table = obj.table.ToList();
                    thisPlayer.garbage = obj.garbage.ToList();
                    thisPlayer.currentPlayerID = obj.currentPlayer;
                    packetBuffer.RemoveAt(i);
                    if(obj.alert != 0)
                    {
                        display.alert = obj.alert;
                    }
                }
            }

            banner.tick();

            move? move = display.tick(focus);


            if (move.HasValue)
            {
                sendMessage(packetEncodeDecode.encodeObject(move, id, "move"));
            }
        }
            
        private void initialise(initalisationPacket initalPacket)
        {
            //TODO: write initalisation code
            thisPlayer = new player(id, name, textures, sounds, screenSize, initalPacket.cards.ToList());
            display = new display(thisPlayer, textures);
        }

        public void draw(SpriteBatch spriteBatch, GraphicsDevice graphicsDevice)
        {
            //TODO: add draw code

            if (initalised)
            {
                //thisPlayer.draw(spriteBatch);
                display.draw(spriteBatch);
                spriteBatch.Draw(textures.get("pass"), new Rectangle(274,42, 24,24), new Rectangle(24, whichButtonPressed == 2 ? 24 : 0, 24, 24),Color.White);
                spriteBatch.Draw(textures.get("pass"), new Rectangle(274,42+28, 24,24), new Rectangle(0, whichButtonPressed == 1 ? 24 : 0, 24, 24),Color.White);
            }
            else
            {
                spriteBatch.Draw(textures.get("table"), Vector2.Zero, lobby ? Color.Gray : Color.White);
                spriteBatch.DrawString(font, $"Players: {playerCount}", new Vector2(64, 64), Color.White);
            }
            banner.draw(spriteBatch, screenSize);
        }

        player thisPlayer;

        NetClient _client;

        string appName;
        string name;
        int id;
        int playerCount;

        //TODO: update player threshold
        const int playerThreshold = 3;

        string ip;
        int port;

        bool lobby;

        List<string> packetBuffer = new List<string>();

        //connect -> start
        bool connected = false;
        bool initalised = false;

        int ticks = 0;

        DateTime lastServerPing;

        Textures textures;
        SoundFX sounds;
        SpriteFont font;

        Point screenSize;

        public client(string ip, int port, string appname, Point screenSize, Textures textures, SoundFX sounds, SpriteFont font)
        {
            this.ip = ip; this.port = port;
            this.appName = appname;
            this.textures = textures;
            this.sounds = sounds;
            this.font = font;
            this.screenSize = screenSize;
            banner = new banner(font, textures.get("banner"));
            restartConnection();
        }

        public void tick(bool focus)
        {
            ticks++;
            readMessages();


            if (connected)
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
                    if(initalised)
                    {
                        tickGame(focus);
                    }
                    else
                    {
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
                }
                
                return;
            }

            if (ticks % 10 == 0)
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
            packetBuffer.Clear();
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
