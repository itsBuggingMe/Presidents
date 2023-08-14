using common;
using System;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Microsoft.Xna.Framework;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Xml.XPath;
using System.Diagnostics;
using System.Security.Cryptography;
using System.Collections;

namespace client
{
    internal class display
    {
        player parent;

        Textures textures;

        List<displayCard> displayPlayer;

        List<displayCard> displayTable;

        List<displayCard> displayGarbage;

        byte explosionDecay = 0;
        const byte explosionTime = 24;

        int activePlayerID;
        MouseState preMouseState = Mouse.GetState();
        MouseState mouseState = Mouse.GetState();
        public display(player parent, Textures textures)
        {
            this.parent = parent;

            this.textures = textures;

            displayTable = new List<displayCard>();
            displayGarbage = new List<displayCard>();

            displayPlayer = new List<displayCard>();
            int cardLoc = 160 - (parent.cards.Count * 14 / 2) + 4;
            for (int i = 0; i < parent.cards.Count; i++)
            {
                displayPlayer.Add(new displayCard(parent.cards[i], new Animation(20, 0, new Vector3(150, 24, 0), new Vector3(cardLoc, 180, 0))));
                cardLoc += 14;
            }
        }

        public move? tick(bool focus)
        {
            move? output = null;

            preMouseState = mouseState;
            mouseState = Mouse.GetState();
            Point mouseLoc = func.getMouseLoc(Mouse.GetState(), parent.screenSize).ToPoint();
            
            checkArrays();

            if (risingEdgeRight() || activePlayerID != parent.currentPlayerID)
            {//DESELECT
                foreach (displayCard card in displayPlayer)
                {
                    card.selected = false;
                }
            }

            int selectedIndex = -1;

            for (int i = 0; i < parent.cards.Count; i++)
            {//ideal locations for player card
                displayCard currentCard = displayPlayer[i];
                int cardLoc = 160 - (parent.cards.Count * 14 / 2) + 4;
                int wantXlocation = i * 14 + cardLoc;
                int sideLeft = wantXlocation - 21;
                int sideRight = wantXlocation - 7;

                int wantYlocation = activePlayerID == parent.id ? 160 : 180;
                
                if(currentCard.selected)
                {
                    wantYlocation -= 28;
                }

                if (mouseLoc.Y > wantYlocation - 80 && mouseLoc.X < sideLeft)
                {
                    wantXlocation += 28;
                    if(selectedIndex == -1 && i != 0)
                    {
                        selectedIndex = i - 1;
                    }
                }
                if (mouseLoc.Y > wantYlocation - 80 && i == parent.cards.Count - 1)
                {
                    if (selectedIndex == -1)
                    {
                        selectedIndex = parent.cards.Count - 1;
                    }
                }
                if ((wantYlocation != currentCard.location.Y || wantXlocation != currentCard.location.X) && !currentCard.animation.active)
                {
                    currentCard.animation = new Animation(10, 4, new Vector3(currentCard.location.X, currentCard.location.Y, 0), new Vector3(wantXlocation, wantYlocation, 0));
                }
            }

            for(int i = 0; i < displayTable.Count(); i++)
            {
                displayCard currentCard = displayTable[i];
                Point wantLoc = new Point(78 + i * 24, 67);

                if (currentCard.location != wantLoc && !currentCard.animation.active)
                {
                    currentCard.animation = new Animation(10, 4, new Vector3(currentCard.location.X, currentCard.location.Y, 0), new Vector3(wantLoc.X, wantLoc.Y, 0));
                }
            }

            if (risingEdgeLeft() && selectedIndex != -1)
            {//SELECT
                displayPlayer[selectedIndex].selected = true;
            }

            if (Keyboard.GetState().IsKeyDown(Keys.Space) && focus)
            {//GENERATEE MOVE
                List<card> cards = new List<card>();
                foreach(displayCard card in displayPlayer)
                {
                    if(card.selected)
                    {
                        card.selected = false;
                        cards.Add(new card(card.cardID));
                    }
                }
                if(cards.Count > 0)
                    output = new move(parent.id, cards);
            }

            if (Keyboard.GetState().IsKeyDown(Keys.Enter) && focus)
            {//GENERATEE MOVE
                List<card> cards = new List<card>();
                output = new move(parent.id, cards);
            }

            for (int i = 0; i < displayPlayer.Count; i++)
            {
                Vector3 location = displayPlayer[i].animation.tick();
                displayPlayer[i].location = new Vector2(location.X, location.Y).ToPoint();
                displayPlayer[i].rotationX = location.Z;
            }
            for (int i = 0; i < displayTable.Count; i++)
            {
                Vector3 location = displayTable[i].animation.tick();
                displayTable[i].location = new Vector2(location.X, location.Y).ToPoint();
                displayTable[i].rotationX = location.Z;
            }
            for (int i = 0; i < displayGarbage.Count; i++)
            {
                Vector3 location = displayGarbage[i].animation.tick();
                displayGarbage[i].location = new Vector2(location.X, location.Y).ToPoint();
                displayGarbage[i].rotationX = location.Z;
            }
            activePlayerID = parent.currentPlayerID;


            return output;
        }

        private bool risingEdgeLeft()
        {
            return mouseState.LeftButton == ButtonState.Pressed && preMouseState.LeftButton == ButtonState.Released;
        }

        private bool risingEdgeRight()
        {
            return mouseState.RightButton == ButtonState.Pressed && preMouseState.RightButton == ButtonState.Released;
        }

        private void checkArrays()
        {
            //player -> table
            List<byte> cardsToRemove = new List<byte>();
            foreach (displayCard dispCard in displayPlayer)
            {
                if (func.listContainsCard(parent.table, dispCard.cardID))
                {
                    cardsToRemove.Add((byte)dispCard.cardID);
                    dispCard.animation = new Animation(10, 1, new Vector3(dispCard.location.X, dispCard.location.Y, 0), new Vector3(78 + displayGarbage.Count * 24, 67, 0));
                    displayTable.Add(dispCard);
                    if (dispCard.cardValue == 0)
                    {
                        explosionDecay = explosionTime;

                        foreach (displayCard garbCard in displayGarbage)
                        {
                            if (!garbCard.animation.active)
                            {
                                Vector3 end = new Vector3(random.Next(23, 297), random.Next(23, 147), garbCard.rotationX + random.Next(720) - 360);
                                garbCard.animation = new Animation(24, 3, new Vector3(garbCard.location.X, garbCard.location.Y, garbCard.rotationX), end);
                                garbCard.animation.animation = new Animation(48, 2, end, new Vector3(238, 66, 0));
                            }
                        }
                    }
                }
            }

            foreach (byte cardToRemove in cardsToRemove)
            {
                foreach (card card in parent.cards)
                {
                    if ((byte)card.cardID == cardToRemove)
                    {
                        parent.cards.Remove(card);
                        break;
                    }
                }
                foreach (displayCard card in displayPlayer)
                {
                    if ((byte)card.cardID == cardToRemove)
                    {
                        displayPlayer.Remove(card);
                        break;
                    }
                }
            }



            //table -> garb
            cardsToRemove = new List<byte>();
            foreach (displayCard dispCard in displayTable)
            {
                if (func.listContainsCard(parent.garbage, dispCard.cardID))
                {
                    cardsToRemove.Add((byte)dispCard.cardID);
                    displayGarbage.Add(dispCard);
                }
            }

            foreach (byte cardToRemove in cardsToRemove)
            {
                foreach (card card in parent.table)
                {
                    if ((byte)card.cardID == cardToRemove)
                    {
                        parent.table.Remove(card);
                        break;
                    }
                }
                foreach (displayCard card in displayTable)
                {
                    if ((byte)card.cardID == cardToRemove)
                    {
                        displayTable.Remove(card);
                        break;
                    }
                }
            }

            //--> new table

            foreach (card card in parent.table)
            {
                if(!listContainsCard(displayTable, card.cardID))
                {
                    displayTable.Add(new displayCard(card, new Animation(10, 1, new Vector3(0, 0, 0), new Vector3(78, 67,0))));
                    if(card.cardValue == 0)
                    {
                        explosionDecay = explosionTime;

                        foreach (displayCard dispCard in displayGarbage)
                        {
                            if (!dispCard.animation.active)
                            {
                                Vector3 end = new Vector3(random.Next(23, 297), random.Next(23, 147), dispCard.rotationX + random.Next(720) - 360);
                                dispCard.animation = new Animation(24, 3, new Vector3(dispCard.location.X, dispCard.location.Y, dispCard.rotationX), end);
                                dispCard.animation.animation = new Animation(48, 2, end, new Vector3(238, 66, 0));
                            }
                        }
                    }
                }
            }
            
            for(int i = displayTable.Count - 1; i >= 0;i--)
            {
                if (!func.listContainsCard(parent.table, displayTable[i].cardID))
                {
                    displayTable.RemoveAt(i);
                }
            }
        }
        Random random = new Random();
        public void draw(SpriteBatch spriteBatch)
        {
            Texture2D cardMap = textures.get("cardmap");
            Texture2D explosion = textures.get("boom");

            Vector2 screenShake = Vector2.Zero;

            if (explosionDecay > 0)
            {
                explosionDecay--;
                int range = 4;
                screenShake = new Vector2(random.Next(-range, range+1), random.Next(-range, range + 1));
            }

            spriteBatch.Draw(textures.get("table"), screenShake, Color.White);

            for (int i = 0; i < displayGarbage.Count; i++)
            {
                displayGarbage[i].location += screenShake.ToPoint();
                displayGarbage[i].drawCard(spriteBatch, cardMap);
                displayGarbage[i].location -= screenShake.ToPoint();
            }

            for (int i = 0; i < displayPlayer.Count; i++)
            {
                displayPlayer[i].location += screenShake.ToPoint();
                displayPlayer[i].drawCard(spriteBatch, cardMap);
                displayPlayer[i].location -= screenShake.ToPoint();
            }

            for (int i = 0; i < displayTable.Count; i++)
            {
                displayTable[i].location += screenShake.ToPoint();
                displayTable[i].drawCard(spriteBatch, cardMap);
                displayTable[i].location -= screenShake.ToPoint();
            }
        }

        private displayCard getDisplayCard(List<displayCard> list, int cardID)
        {
            foreach (displayCard tableCardToRemove in list)
            {
                if (tableCardToRemove.cardID == cardID)
                {
                    return tableCardToRemove;
                }
            }
            throw new Exception($"List does not contain requested card: {cardID}");
        }

        const int cardSize = 64;

        public static bool listContainsCard(List<displayCard> cards, int id)
        {
            foreach (displayCard card in cards)
            {
                if (card.cardID == id)
                {
                    return true;
                }
            }
            return false;
        }
    }


    internal class displayCard
    {
        public int cardID;

        public Animation animation;
        public bool selected = false;
        public displayCard(cardTypes suit, int value, Animation animation)
        {
            cardID = (int)suit * 13 + value;
            rotationX = 0;
            rotationY = 0;
            rotationZ = 0;
            location = new Point(150, 24);
            this.animation = animation;
        }

        public displayCard(card card, Animation animation)
        {
            cardID = (int)card.suit * 13 + card.cardValue;
            rotationX = 0;
            rotationY = 0;
            rotationZ = 0;
            location = new Point(150, 24);
            this.animation = animation;
        }

        public cardTypes suit
        {
            get
            {
                return (cardTypes)(cardID / 13);
            }
        }

        public int cardValue
        {
            get
            {
                return cardID % 13;
            }
        }

        public Point location;
        public float rotationX;
        public float rotationY;
        public float rotationZ;

        public void drawCard(SpriteBatch spriteBatch, Texture2D cardMap)
        {
            rotationX %= 360;
            rotationY %= 360;
            rotationZ %= 360;

            if (rotationY > 180)
            {
                rotationY = 180 - (rotationY - 180);
            }
            if (rotationZ > 180)
            {
                rotationZ = 180 - (rotationZ - 180);
            }

            float distanceFrom90Y = Math.Abs(rotationY - 90) / 90;
            float distanceFrom90Z = Math.Abs(rotationZ - 90) / 90;

            float colorUse = Math.Min(distanceFrom90Z, distanceFrom90Y) * 255;
            Color color = new Color((byte)colorUse, (byte)colorUse, (byte)colorUse);

            Rectangle sourceRectangle = rotationY > 90 || rotationZ > 90 ? new Rectangle(832, 64, 64, 64) : new Rectangle(this.cardValue * 64, (int)this.suit * 64, 64, 64);


            spriteBatch.Draw(cardMap, new Rectangle(location.X, location.Y, (int)(64 * distanceFrom90Y), (int)(64 * distanceFrom90Z)), sourceRectangle, color, MathHelper.ToRadians(rotationX), new Vector2(sourceRectangle.Width / 2f, sourceRectangle.Height / 2f), SpriteEffects.None, 1);
        }
    }


    internal class Animation
    {
        int currentFrame;
        int maxFrames;
        Vector3 startPoint;
        Vector3 endPoint;

        public Animation? animation = null;

        byte type;
        public bool active;
        public byte delay = 0;

        public Animation()
        {
            active = false;
        }

        public Animation(int maxFrames, byte type, Vector3 start, Vector3 end)
        {
            this.currentFrame = 0;
            this.maxFrames = maxFrames;
            this.startPoint = start;
            this.endPoint = end;
            this.type = type;
            active = true;
        }

        public Vector3 tick()
        {
            if(!active)
            {
                return endPoint;
            }

            if (currentFrame <= maxFrames)
            {
                float t = (float)currentFrame / maxFrames;
                if(delay > 0)
                {
                    delay--;
                }
                else
                {
                    currentFrame++;
                }
                switch (type)
                {
                    case 0:
                        return Vector3.Lerp(startPoint, endPoint, t);
                    case 1:
                        return parabolic(t);
                    case 2:
                        return cubic(t);
                    case 3:
                        return inverseCubic(t);
                    case 4:
                        return sigmoid(t);
                    default:
                        return Vector3.Lerp(startPoint, endPoint, t);
                }
            }
            else
            {
                active = false;
                if(animation != null)
                {
                    this.currentFrame = 0;
                    this.maxFrames = animation.maxFrames;
                    this.startPoint = animation.startPoint;
                    this.endPoint = animation.endPoint;
                    this.type = animation.type;
                    active = true;

                    this.animation = null;

                    delay = 20;
                    return startPoint;
                }
                else
                {
                    return endPoint;
                }
            }
        }

        private Vector3 parabolic(float t) 
        {
            float tSquared = t * t;
            return new Vector3(tSquared * (endPoint.X - startPoint.X) + startPoint.X,
                               tSquared * (endPoint.Y - startPoint.Y) + startPoint.Y,
                               tSquared * (endPoint.Z - startPoint.Z) + startPoint.Z);
        }

        private Vector3 cubic(float t)
        {
            float tSquared = t * t * t;
            return new Vector3(tSquared * (endPoint.X - startPoint.X) + startPoint.X,
                               tSquared * (endPoint.Y - startPoint.Y) + startPoint.Y,
                               tSquared * (endPoint.Z - startPoint.Z) + startPoint.Z);
        }
        private Vector3 inverseCubic(float t)
        {
            float tSquared = 1 - (1 - t) * (1 - t) * (1 - t);
            return new Vector3(tSquared * (endPoint.X - startPoint.X) + startPoint.X,
                               tSquared * (endPoint.Y - startPoint.Y) + startPoint.Y,
                               tSquared * (endPoint.Z - startPoint.Z) + startPoint.Z);
        }

        private Vector3 sigmoid(float t)
        {
            float tSquared = (float)(1 / (1 + Math.Exp(10 * (-t + 0.5f))));
            return new Vector3(tSquared * (endPoint.X - startPoint.X) + startPoint.X,
                               tSquared * (endPoint.Y - startPoint.Y) + startPoint.Y,
                               tSquared * (endPoint.Z - startPoint.Z) + startPoint.Z);
        }
    }
}
