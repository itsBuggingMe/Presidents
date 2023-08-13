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

namespace client
{
    internal class display
    {
        player parent;

        Textures textures;

        List<displayCard> displayPlayer;

        List<displayCard> displayTable;

        List<displayCard> displayGarbage;

        int activePlayerID;

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
                displayPlayer.Add(new displayCard(parent.cards[i], new Animation(20, 0, new Vector2(150, 24), new Vector2(cardLoc, 180))));
                cardLoc += 14;
            }
        }

        public void tick()
        {
            if(parent.currentPlayerID != activePlayerID)
            {
                if(parent.id == parent.currentPlayerID)
                {//start own turn
                    int cardLoc = 160 - (parent.cards.Count * 14 / 2) + 4;
                    for (int i = 0; i < parent.cards.Count; i++)
                    {
                        displayPlayer[i].animation = new Animation(10, 0, displayPlayer[i].location.ToVector2(), new Vector2(cardLoc, 160));
                        cardLoc += 14;
                    }
                }
                else if(activePlayerID == parent.id)
                {//end own turn
                    int cardLoc = 160 - (parent.cards.Count * 14 / 2) + 4;
                    for (int i = 0; i < parent.cards.Count; i++)
                    {
                        displayPlayer[i].animation = new Animation(10, 0, displayPlayer[i].location.ToVector2(), new Vector2(cardLoc, 180));

                        cardLoc += 14;
                    }
                }
            }
            for (int i = 0; i < parent.cards.Count; i++)
            {
                if (parent.selectedCards.Contains(displayPlayer[i].cardID) && !displayPlayer[i].animation.active && !displayPlayer[i].selected)
                {
                    displayPlayer[i].animation = new Animation(6, 0, displayPlayer[i].location.ToVector2(), new Vector2(displayPlayer[i].location.X, 120));
                    displayPlayer[i].selected = true;
                }
                else if(displayPlayer[i].selected)
                {
                    displayPlayer[i].animation = new Animation(6, 0, displayPlayer[i].location.ToVector2(), new Vector2(displayPlayer[i].location.X, 160));
                    displayPlayer[i].selected = false;
                }
            }

            checkArrays();

            for (int i = 0; i < displayPlayer.Count; i++)
            {
                displayPlayer[i].location = displayPlayer[i].animation.tick().ToPoint();
                Debug.WriteLine(displayPlayer[i].location);
            }
            for (int i = 0; i < displayTable.Count; i++)
            {
                displayTable[i].location = displayTable[i].animation.tick().ToPoint();
            }
            for (int i = 0; i < displayGarbage.Count; i++)
            {
                displayGarbage[i].location = displayGarbage[i].animation.tick().ToPoint();
            }
            activePlayerID = parent.currentPlayerID;
        }

        private void checkArrays()
        {
            List<byte> cardsToRemove = new List<byte>();
            foreach (displayCard dispCard in displayPlayer)
            {
                if (func.listContainsCard(parent.table, dispCard.cardID))
                {
                    cardsToRemove.Add((byte)dispCard.cardID);
                    displayTable.Add(dispCard);
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
        }
        
        public void draw(SpriteBatch spriteBatch)
        {
            Texture2D cardMap = textures.get("cardmap");
            for (int i = 0; i < displayPlayer.Count; i++)
            {
                displayPlayer[i].drawCard(spriteBatch, cardMap);
            }

            for (int i = 0; i < displayTable.Count; i++)
            {
                displayTable[i].drawCard(spriteBatch, cardMap);

            }

            for (int i = 0; i < displayGarbage.Count; i++)
            {
                displayGarbage[i].drawCard(spriteBatch, cardMap);
            }
        }

        const int cardSize = 64;
    }


    internal class displayCard
    {
        public int cardID;

        public Animation animation;
        public bool selected;
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
        Vector2 startPoint;
        Vector2 endPoint;

        byte type;
        public bool active;

        public Animation()
        {
            active = false;
        }

        public Animation(int maxFrames, byte type, Vector2 start, Vector2 end)
        {
                this.currentFrame = 0;
                this.maxFrames = maxFrames;
                this.startPoint = start;
                this.endPoint = end;
                this.type = type;
                active = true;
        }

        public Vector2 tick()
        {
            if(!active)
            {
                return endPoint;
            }

            if (currentFrame <= maxFrames)
            {
                float t = (float)currentFrame / maxFrames;
                Debug.WriteLine(t);
                currentFrame++;
                switch (type)
                {
                    case 0:
                        return Vector2.Lerp(startPoint, endPoint, t);
                    case 1:
                        return parabolic(t);
                    case 2:
                        return cubic(t);
                    case 3:
                        return cubicHermite(t);
                    default:
                        return Vector2.Lerp(startPoint, endPoint, t);
                }
            }
            else
            {
                active = false;
                return endPoint;
            }
        }

        private Vector2 parabolic(float t)
        {
            float tSquared = t * t;
            return new Vector2(tSquared * (endPoint.X - startPoint.X) + startPoint.X, tSquared * (endPoint.Y - startPoint.Y) + startPoint.Y);
        }
        private Vector2 cubic(float t)
        {
            float tSquared = t * t * t;
            return new Vector2(tSquared * (endPoint.X - startPoint.X) + startPoint.X, tSquared * (endPoint.Y - startPoint.Y) + startPoint.Y);
        }

        private Vector2 cubicHermite(float t)
        {
            Vector2 p0 = startPoint;
            Vector2 p1 = Vector2.Lerp(startPoint, endPoint, 1.0f / 3.0f);
            Vector2 p2 = Vector2.Lerp(startPoint, endPoint, 2.0f / 3.0f);
            Vector2 p3 = endPoint;

            float t2 = t * t;
            float t3 = t2 * t;

            Vector2 interpolatedPoint =
                (-t3 + 2 * t2 - t) * p0 +
                (3 * t3 - 5 * t2 + 2) * p1 +
                (-3 * t3 + 4 * t2 + t) * p2 +
                (t3 - t2) * p3;

            return interpolatedPoint;
        }
    }
}
