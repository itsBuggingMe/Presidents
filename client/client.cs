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
using System.Text;
using System.Threading.Tasks.Dataflow;
using System.Xml.Serialization;

namespace client
{
    internal class client
    {
        banner banner;
        byte whichButtonPressed;
        display display;

        StringBuilder textBuffer = new StringBuilder();
        int connectionAttempts = 0;
        private void tickGame(bool focus)
        {
            display.alert = 0;
            for (int i = packetBuffer.Count - 1; i >= 0; i--)
            {
                if (packetEncodeDecode.tryDecodeObject(packetBuffer[i], out int playerID, out gameState obj, "state") && playerID == 0)
                {
                    thisPlayer.table = obj.table.ToList();
                    thisPlayer.garbage = obj.garbage.ToList();
                    display.cardCount = obj.cardNumber;
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
            display = new display(thisPlayer, textures,font, initalPacket.id, initalPacket.name);
            display.cardCount = initalPacket.cc;
        }

        public void draw(SpriteBatch spriteBatch, GraphicsDevice graphicsDevice)
        {
            //TODO: add draw code

            if (initalised)
            {//ingame
                //thisPlayer.draw(spriteBatch);
                if(purgatory)
                {
                    spriteBatch.Draw(textures.get("table"), new Vector2(0,0), Color.Gray);

                    for (int i = 0; i < purg.id.Count(); i++)
                    {
                        spriteBatch.DrawString(font, $"{i+1}. {display.name[display.id.ToList().IndexOf(purg.id[i])]}", new Vector2(60, 48 + 12 * i), Color.Gray);
                    }
                }
                else
                {
                    display.draw(spriteBatch);
                }
                //spriteBatch.Draw(textures.get("pass"), new Rectangle(274,42, 24,24), new Rectangle(24, whichButtonPressed == 2 ? 24 : 0, 24, 24),Color.White);
                //spriteBatch.Draw(textures.get("pass"), new Rectangle(274,42+28, 24,24), new Rectangle(0, whichButtonPressed == 1 ? 24 : 0, 24, 24),Color.White);
            }
            else
            {
                if (connected)
                {
                    spriteBatch.Draw(textures.get("table"), Vector2.Zero, Color.Gray);
                    foreach (displayCard card in backGroundCards)
                    {
                        card.drawCard(spriteBatch, textures.get("cardmap"), Color.Gray);
                    }

                    string connectingText = $"Players: {playerCount}";
                    Vector2 connectingTextSize = font.MeasureString(connectingText) / 2;

                    string ipAddressText = "Name: " + textBuffer.ToString();
                    Vector2 ipAddressTextSize = font.MeasureString(ipAddressText) / 2;

                    float connectingTextX = (320 - connectingTextSize.X) / 2;
                    Vector2 connectingTextPosition = new Vector2(connectingTextX, 64);

                    float ipAddressTextX = (320 - ipAddressTextSize.X) / 2;
                    Vector2 ipAddressTextPosition = new Vector2(ipAddressTextX, 96);

                    spriteBatch.DrawString(font, connectingText, connectingTextPosition, Color.White, 0, Vector2.Zero, 0.5f, SpriteEffects.None, 1);
                    spriteBatch.DrawString(font, ipAddressText + (connectionAttempts % 2 == 0 ? "_" : ""), ipAddressTextPosition , Color.White, 0, Vector2.Zero, 0.5f, SpriteEffects.None, 1);
                }
                else
                {
                    string dots = "";
                    for (int i = 0; i < connectionAttempts % 4; i++)
                    {
                        dots += ".";
                    }

                    spriteBatch.Draw(textures.get("table"), Vector2.Zero, Color.Gray);
                    foreach (displayCard card in backGroundCards)
                    {
                        card.drawCard(spriteBatch, textures.get("cardmap"), Color.Gray);
                    }
                    string connectingText = "Connecting" + dots;
                    Vector2 connectingTextSize = font.MeasureString(connectingText)/2;

                    string ipAddressText = "IP: " + textBuffer.ToString();
                    Vector2 ipAddressTextSize = font.MeasureString(ipAddressText)/2;

                    float connectingTextX = (320 - connectingTextSize.X) / 2;
                    Vector2 connectingTextPosition = new Vector2(connectingTextX, 64);

                    float ipAddressTextX = (320 - ipAddressTextSize.X) / 2;
                    Vector2 ipAddressTextPosition = new Vector2(ipAddressTextX, 96);

                    spriteBatch.DrawString(font, connectingText, connectingTextPosition, Color.White, 0, Vector2.Zero, 0.5f, SpriteEffects.None, 1);
                    spriteBatch.DrawString(font, ipAddressText + (connectionAttempts % 2 == 0 ? "_" : ""), ipAddressTextPosition, Color.White, 0, Vector2.Zero, 0.5f, SpriteEffects.None, 1);
                }
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
        Keys[] pressedKeys = new Keys[0];
        Random random  = new Random();

        purg purg;

        List<displayCard> backGroundCards = new List<displayCard>();

        public client(string ip, int port, string appname, Point screenSize, Textures textures, SoundFX sounds, SpriteFont font)
        {
            this.ip = ip; this.port = port;
            this.appName = appname;
            this.textures = textures;
            this.sounds = sounds;
            font.Spacing = -2.4f;
            this.font = font;
            this.screenSize = screenSize;
            banner = new banner(font, textures.get("banner"));
            textBuffer = new StringBuilder(ip);
            restartConnection();
        }
        bool purgatory = true;
        public void tick(bool focus)
        {
            ticks++;
            readMessages();


            if (connected)
            {
                checkConnections();
                if(lobby)
                {
                    if (ticks % 30 == 0)
                    {
                        connectionAttempts++;
                    }
                    tickBackGroundCards();
                    updateTextBuffer();
                    name = textBuffer.ToString();
                    if (Keyboard.GetState().IsKeyDown(Keys.Enter) && playerCount >= playerThreshold)
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
                        if(packetBufferContainsPurg(out purg purg) || this.purgatory)
                        {
                            this.purgatory = true;
                            this.purg = purg;



                            for (int i = packetBuffer.Count - 1; i >= 0; i--)
                            {
                                if (packetEncodeDecode.tryDecodeStart(packetBuffer[i], out initalisationPacket initalPacket))
                                {
                                    initialise(initalPacket);
                                    initalised = true;
                                    purgatory = false;
                                    packetBuffer.RemoveAt(i);
                                    break;
                                }
                            }
                        }
                        else
                        {
                            tickGame(focus);
                        }
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

            // ^ top returns
            updateTextBuffer();
            tickBackGroundCards();

            if (ticks % 30 == 0)
            {
                ip = textBuffer.ToString();
                connectToServer();
                connectionAttempts++;
            }

            foreach(string message in packetBuffer)
            {
                if(packetEncodeDecode.tryDecodeID(message, out int id, out string name))
                {
                    this.id = id;
                    connected = true;
                    lastServerPing = DateTime.Now;
                    textBuffer = new StringBuilder("prez#" + id.ToString().Substring(0, 3));
                }
            }

        }

        private bool packetBufferContainsPurg(out purg purg)
        {
            foreach(string packet in packetBuffer)
            {
                if(packetEncodeDecode.tryDecodeObject(packet, out int id, out purg obj, "purg"))
                {
                    purg = obj;
                    packetBuffer.Remove(packet);
                    return true;
                }
            }
            purg = new purg();
            return false;
        }

        private void tickBackGroundCards()
        {
            if(ticks % 90 == 0)
            {
                int cycle = (ticks / 90) % 3;
                int min = 24 + 45 * cycle;
                int max = min + 45;
                int y = random.Next(min, max);
                displayCard card = new displayCard(new card(random.Next(0, 53)), new Animation(840, 0, new Vector3(-42, y, 0), new Vector3(384, y, random.Next(480, 1440) * (random.Next(2) == 1 ? -1 : 1))));
                card.rotationZ = (random.Next(2) == 1 ? 180 : 0);
                backGroundCards.Add(card);

                if(backGroundCards.Count > 16)
                {
                    backGroundCards.RemoveAt(0);
                }
            }

            for (int i = 0; i < backGroundCards.Count; i++)
            {
                Vector3 location = backGroundCards[i].animation.tick();
                backGroundCards[i].location = new Vector2(location.X, location.Y).ToPoint();
                backGroundCards[i].rotationX = location.Z;
            }
        }


        private void updateTextBuffer()
        {
            KeyboardState keyboardState = Keyboard.GetState();
            Keys[] currentPressedKeys = keyboardState.GetPressedKeys();
            foreach (Keys key in currentPressedKeys)
            {
                if (!pressedKeys.Contains(key))
                {
                    if (key == Keys.Back && textBuffer.Length > 0)
                    {
                        textBuffer.Length--;
                    }
                    else
                    {
                        char character = ConvertKeyToChar(key, keyboardState.IsKeyDown(Keys.LeftShift) || keyboardState.IsKeyDown(Keys.RightShift));
                        if (character != '\0' && textBuffer.Length < 16)
                        {
                            textBuffer.Append(character);
                        }
                    }
                }
            }
            pressedKeys = currentPressedKeys;
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
                sendMessage(packetEncodeDecode.encodeID(id, name));
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

        private char ConvertKeyToChar(Keys key, bool isShiftPressed)
        {
            switch (key)
            {
                case Keys.A:
                    return isShiftPressed ? 'A' : 'a';
                case Keys.B:
                    return isShiftPressed ? 'B' : 'b';
                case Keys.C:
                    return isShiftPressed ? 'C' : 'c';
                case Keys.D:
                    return isShiftPressed ? 'D' : 'd';
                case Keys.E:
                    return isShiftPressed ? 'E' : 'e';
                case Keys.F:
                    return isShiftPressed ? 'F' : 'f';
                case Keys.G:
                    return isShiftPressed ? 'G' : 'g';
                case Keys.H:
                    return isShiftPressed ? 'H' : 'h';
                case Keys.I:
                    return isShiftPressed ? 'I' : 'i';
                case Keys.J:
                    return isShiftPressed ? 'J' : 'j';
                case Keys.K:
                    return isShiftPressed ? 'K' : 'k';
                case Keys.L:
                    return isShiftPressed ? 'L' : 'l';
                case Keys.M:
                    return isShiftPressed ? 'M' : 'm';
                case Keys.N:
                    return isShiftPressed ? 'N' : 'n';
                case Keys.O:
                    return isShiftPressed ? 'O' : 'o';
                case Keys.P:
                    return isShiftPressed ? 'P' : 'p';
                case Keys.Q:
                    return isShiftPressed ? 'Q' : 'q';
                case Keys.R:
                    return isShiftPressed ? 'R' : 'r';
                case Keys.S:
                    return isShiftPressed ? 'S' : 's';
                case Keys.T:
                    return isShiftPressed ? 'T' : 't';
                case Keys.U:
                    return isShiftPressed ? 'U' : 'u';
                case Keys.V:
                    return isShiftPressed ? 'V' : 'v';
                case Keys.W:
                    return isShiftPressed ? 'W' : 'w';
                case Keys.X:
                    return isShiftPressed ? 'X' : 'x';
                case Keys.Y:
                    return isShiftPressed ? 'Y' : 'y';
                case Keys.Z:
                    return isShiftPressed ? 'Z' : 'z';
                case Keys.Space:
                    return ' ';
                case Keys.OemPeriod:
                    return '.';
                case Keys.Decimal:
                    return '.';
                case Keys.NumPad0:
                case Keys.D0:
                    return '0';
                case Keys.NumPad1:
                case Keys.D1:
                    return '1';
                case Keys.NumPad2:
                case Keys.D2:
                    return '2';
                case Keys.NumPad3:
                case Keys.D3:
                    return '3';
                case Keys.NumPad4:
                case Keys.D4:
                    return '4';
                case Keys.NumPad5:
                case Keys.D5:
                    return '5';
                case Keys.NumPad6:
                case Keys.D6:
                    return '6';
                case Keys.NumPad7:
                case Keys.D7:
                    return '7';
                case Keys.NumPad8:
                case Keys.D8:
                    return '8';
                case Keys.NumPad9:
                case Keys.D9:
                    return '9';
                default:
                    return '\0';
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
