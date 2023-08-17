﻿using common;
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

        //other players
        public int[] id;
        public string[] name;
        public int[] cardCount;

        Textures textures;
        SpriteFont font;

        List<displayCard> displayPlayer;

        List<displayCard> displayTable;

        List<displayCard> displayGarbage;

        byte explosionDecay = 0;
        const byte explosionTime = 24;

        int activePlayerID;
        MouseState preMouseState = Mouse.GetState();
        MouseState mouseState = Mouse.GetState();
        public byte alert;

        Point[] playerPositions = new Point[] { new Point(287,24), new Point(287,82), new Point(31,82), new Point(31,24), new Point(86,0), new Point(191,0)};
        Point[] playerCardPositions = new Point[] { new Point(287+24,24), new Point(287+24,82), new Point(31-24,82), new Point(31-24,24), new Point(86,-32), new Point(191,-32)};

        Point lastFramePlayerLocation;

        public display(player parent, Textures textures, SpriteFont font, int[] id, string[] names)
        {
            this.name = names;
            this.id = id;
            this.parent = parent;
            this.font = font;
            this.textures = textures;

            displayTable = new List<displayCard>();
            displayGarbage = new List<displayCard>();

            displayPlayer = new List<displayCard>();
            int cardLoc = 160 - (parent.cards.Count * 14 / 2) + 4;
            for (int i = 0; i < parent.cards.Count; i++)
            {
                displayPlayer.Add(new displayCard(parent.cards[i], new Animation(20, 0, new Vector3(150, -30, 0), new Vector3(cardLoc, 180, 0))));
                displayPlayer[i].animation.delay = (byte)(i * 2);
                cardLoc += 14;
            }
        }

        int lastBombCardValue = 0;
        public move? tick(bool focus)
        {
            preMouseState = mouseState;
            mouseState = Mouse.GetState();
            Point mouseLoc = func.getMouseLoc(mouseState, parent.screenSize).ToPoint();

            checkArrays(out List<displayCard> tableAdditions, out List<displayCard> garbageAdditions);

            foreach(displayCard card in tableAdditions)
            {
                if (card.cardValue == 0)
                {
                    explosionDecay = explosionTime;

                    blowUpGarbage();
                    break;
                }
            }

            if(tableAdditions.Count > 4)
            {
                foreach(displayCard card in tableAdditions)
                {
                    if (has4OfType(card.cardValue, tableAdditions))
                    {
                        lastBombCardValue = card.cardValue;
                        explosionDecay = explosionTime;

                        blowUpGarbage();
                        break;
                    }
                }
            }


            foreach(displayCard dispCard in tableAdditions)
            {
                dispCard.animation = new Animation(10, 1, new Vector3(dispCard.location.X, dispCard.location.Y, 0), new Vector3(78 + displayTable.Count * 24, 67, 0));
            }
            foreach (displayCard dispCard in garbageAdditions)
            {
                if(dispCard.cardValue == 0)
                {
                    dispCard.animation = new Animation(48, 2, new Vector3(dispCard.location.ToVector2(), 0), new Vector3(238, 66, 0));
                }
            }

            clearGarbage();
            setCardLocationsAndGenMoves(mouseLoc, focus, out move? output);
            setIdealTableLocation();
            tickAnimations();

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
        private void blowUpGarbage()
        {
            foreach (displayCard dispCard in displayGarbage)
            {
                if (!dispCard.animation.active && dispCard.location != new Point(238, 66))
                {
                    Vector3 end = new Vector3(random.Next(23, 297), random.Next(23, 147), dispCard.rotationX + random.Next(720) - 360);
                    dispCard.animation = new Animation(24, 3, new Vector3(dispCard.location.X, dispCard.location.Y, dispCard.rotationX), end);
                    dispCard.animation.animation = new Animation(48, 2, end, new Vector3(238, 66, 0));
                }
            }
        }
        private void setCardLocationsAndGenMoves(Point mouseLoc, bool focus, out move? output)
        {
            int selectedIndex = -1;
            output = null;

            for (int i = 0; i < parent.cards.Count; i++)
            {//ideal locations for player card
                displayCard currentCard = displayPlayer[i];
                int cardLoc = 160 - (parent.cards.Count * 14 / 2) + 4;
                int wantXlocation = i * 14 + cardLoc;
                int sideLeft = wantXlocation - 21;
                int sideRight = wantXlocation - 7;

                int wantYlocation = activePlayerID == parent.id ? 160 : 180;

                if (currentCard.selected)
                {
                    wantYlocation -= 28;
                }

                if (mouseLoc.Y > wantYlocation - 80 && mouseLoc.X < sideLeft)
                {
                    wantXlocation += 28;
                    if (selectedIndex == -1 && i != 0)
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

            if (risingEdgeLeft() && selectedIndex != -1)
            {//SELECT
                displayPlayer[selectedIndex].selected = true;
            }

            if (risingEdgeRight() || activePlayerID != parent.currentPlayerID)
            {//DESELECT
                foreach (displayCard card in displayPlayer)
                {
                    card.selected = false;
                }
            }

            if (Keyboard.GetState().IsKeyDown(Keys.Space) && focus)
            {//GENERATEE MOVE
                List<card> cards = new List<card>();
                foreach (displayCard card in displayPlayer)
                {
                    if (card.selected)
                    {
                        card.selected = false;
                        cards.Add(new card(card.cardID));
                    }
                }
                if (cards.Count > 0)
                    output = new move(parent.id, cards, parent.cards.Count - cards.Count);
            }

            if (Keyboard.GetState().IsKeyDown(Keys.Enter) && focus)
            {//GENERATEE MOVE
                List<card> cards = new List<card>();
                output = new move(parent.id, cards, parent.cards.Count);
            }

        }

        public static bool has4OfType(int cardValue, List<displayCard> cards)
        {
            int count = 0;
            foreach (displayCard card in cards)
            {
                if (card.cardValue == cardValue)
                {
                    count++;
                }
            }
            if (count == 4)
            {
                return true;
            }
            else
            {
                return false;
            }
        }

        private void setIdealTableLocation()
        {
            for (int i = 0; i < displayTable.Count(); i++)
            {
                displayCard currentCard = displayTable[i];
                Point wantLoc = new Point(78 + i * 24, 67);

                if (currentCard.location != wantLoc && !currentCard.animation.active)
                {
                    currentCard.animation = new Animation(10, 4, new Vector3(currentCard.location.X, currentCard.location.Y, 0), new Vector3(wantLoc.X, wantLoc.Y, 0));
                }
            }
        }

        private void clearGarbage()
        {
            if (alert == 1)
            {
                foreach (displayCard card in displayGarbage)
                {
                    if (card.location != new Point(238, 66) && !card.animation.active)
                    {
                        card.animation = new Animation(48, 2, new Vector3(card.location.ToVector2(), 0), new Vector3(238, 66, 0));
                    }
                }
            }
        }

        private void tickAnimations()
        {
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
        }

        private bool tableHasBomb()
        {
            foreach(displayCard card in displayTable)
            {
                if(card.cardValue == 0)
                {
                    return true;
                }
            }
            foreach (displayCard card in displayTable)
            {
                if(has4OfType(card.cardValue, displayTable))
                {
                    return true;
                }
            }
            return false;
        }

        private void checkArrays(out List<displayCard> tableAdditions, out List<displayCard> garbageAdditions)
        {
            tableAdditions = new List<displayCard>();
            garbageAdditions = new List<displayCard>();
            //player -> table
            List<byte> cardsToRemove = new List<byte>();
            
            foreach (displayCard dispCard in displayPlayer)
            {
                if (func.listContainsCard(parent.table, dispCard.cardID))
                {
                    cardsToRemove.Add((byte)dispCard.cardID);
                    displayTable.Add(dispCard);
                    tableAdditions.Add(dispCard);
                }
            }


            if (displayTable.Count > 0 && displayTable[displayTable.Count - 1].cardValue == 0)
            {
                displayTable.Insert(0, displayTable[displayTable.Count - 1]);
                displayTable.RemoveAt(displayTable.Count - 1);
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
                    garbageAdditions.Add(dispCard);
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
                    Vector2 playerlocation = lastFramePlayerLocation.ToVector2();
                    displayCard tableAddition = new displayCard(card, new Animation());
                    tableAddition.location = playerlocation.ToPoint();
                    tableAdditions.Add(tableAddition);
                    displayTable.Add(tableAddition);
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


            //gui
            int indexOfSelf = -1;
            for (int i = 0; i < id.Length; i++)
            {
                if (id[i] == parent.id)
                {
                    indexOfSelf = i;
                    break;
                }
            }
            int placardsDrawn = 0;
            for (int i = 0; i < id.Length; i++)
            {
                if (indexOfSelf == i)
                {
                    continue;
                }//1>2
                int localIndex = placardsDrawn + 2 - indexOfSelf;
                drawPlacardAtIndex(i, (localIndex + 6 * 2) % 6, spriteBatch, activePlayerID == id[i]);
                placardsDrawn++;
            }
            spriteBatch.DrawString(font, parent.name, new Vector2(1, 125), Color.Black, 0, Vector2.Zero, 0.5f, SpriteEffects.None, 1);
        }

        private int idToLocation(int idOfActivePlayer)
        {
            int indexOfSelf = -1;
            for (int i = 0; i < id.Length; i++)
            {
                if (id[i] == parent.id)
                {
                    indexOfSelf = i;
                    break;
                }
            }
            int placardsDrawn = 0;
            for (int i = 0; i < id.Length; i++)
            {
                if (indexOfSelf == i)
                {
                    continue;
                }//1>2

                if (idOfActivePlayer == id[i])
                {
                    int localIndex = placardsDrawn + 2 - indexOfSelf;
                    int localIndexClamped = (localIndex + 6 * 2) % 6;
                    return localIndexClamped;
                }

                placardsDrawn++;
            }
            return -1;
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

        private void drawPlacardAtIndex(int indexList, int indexScreen, SpriteBatch spriteBatch, bool isTurn)
        {

            Texture2D cardMap = textures.get("cardmap");


            if (id[indexList] == activePlayerID)
            {
                lastFramePlayerLocation = playerCardPositions[indexScreen];
            }

            if (indexScreen < 2)
            {
                spriteBatch.Draw(cardMap, new Rectangle(new Point(playerPositions[indexScreen].X + 1, playerPositions[indexScreen].Y + 42), new Point(42, 32)), new Rectangle(843, 66, 42, 32), (isTurn ? Color.White : Color.Gray), MathHelper.ToRadians(270), new Vector2(0, 0), SpriteEffects.None, 1);
                int stringLength = (int)font.MeasureString(name[indexList]).X / 2;
                spriteBatch.DrawString(font, name[indexList], new Vector2(playerPositions[indexScreen].X + 34 - stringLength, playerPositions[indexScreen].Y-8), Color.Black, 0, Vector2.Zero, 0.5f, SpriteEffects.None, 1);

                spriteBatch.DrawString(font, cardCount[indexList].ToString(), new Vector2(playerPositions[indexScreen].X + 12, playerPositions[indexScreen].Y + 16), Color.Black, 0, Vector2.Zero, 0.5f, SpriteEffects.None, 1);
            }
            else if (indexScreen < 4)
            {
                spriteBatch.Draw(cardMap, new Rectangle(playerPositions[indexScreen], new Point(42, 32)), new Rectangle(843, 66, 42, 32), (isTurn ? Color.White : Color.Gray), MathHelper.ToRadians(90), new Vector2(0, 0), SpriteEffects.None, 1);
                spriteBatch.DrawString(font, name[indexList], new Vector2(playerPositions[indexScreen].X-30, playerPositions[indexScreen].Y-8), Color.Black, 0, Vector2.Zero, 0.5f, SpriteEffects.None, 1);

                spriteBatch.DrawString(font, cardCount[indexList].ToString(), new Vector2(playerPositions[indexScreen].X - 24, playerPositions[indexScreen].Y+16), Color.Black, 0, Vector2.Zero, 0.5f, SpriteEffects.None, 1);
            }
            else
            {
                spriteBatch.Draw(cardMap, new Rectangle(new Point(playerPositions[indexScreen].X, playerPositions[indexScreen].Y), new Point(42, 32)), new Rectangle(843, 66, 42, 32), (isTurn ? Color.White : Color.Gray), MathHelper.ToRadians(0), new Vector2(0, 0), SpriteEffects.FlipVertically, 1);
                spriteBatch.DrawString(font, name[indexList], new Vector2(playerPositions[indexScreen].X + 2, playerPositions[indexScreen].Y +1), Color.Black, 0, Vector2.Zero, 0.5f, SpriteEffects.None, 1);

                spriteBatch.DrawString(font, cardCount[indexList].ToString(), new Vector2(playerPositions[indexScreen].X + 16, playerPositions[indexScreen].Y + 12), Color.Black, 0, Vector2.Zero, 0.5f, SpriteEffects.None, 1);
            }
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

        public void drawCard(SpriteBatch spriteBatch, Texture2D cardMap, Color color)
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
        public int delay = 0;

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
