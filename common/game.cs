using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;
using System.Reflection;

namespace common
{
    public class player
    {
        public int id;
        public string name;
        public List<card> cards;

        public int ServerCardCount;
        public int currentPlayerID = 0;

        public List<int> selectedCards = new List<int>();

        Textures textures;
        SoundFX sfx;

        MouseState mouseState;
        MouseState prevMouseState = Mouse.GetState();
        public Point screenSize;
        public bool isPlaying = true;
        public List<card> table = new List<card>();
        public List<card> garbage = new List<card>();

        

        public player(int id, string name, Textures textures, SoundFX sounds, Point screenSize, List<card> cards)
        {
            this.id = id;
            this.name = name;

            this.textures = textures;
            this.sfx = sounds;

            this.screenSize = screenSize;

            this.cards = cards;
            this.cards.Sort((card1, card2) => card2.cardValue.CompareTo(card1.cardValue));
        }

        int cardIndexHoverOver = 0;

        public move? update(out string bannerMSG)
        {
            prevMouseState = mouseState;
            mouseState = Mouse.GetState();

            //cardLocations = new Point[cards.Count];

            int mouseLocalX = (int)func.getMouseLoc(mouseState, screenSize).X;
            int cardLoc = 160 - (cards.Count * cardSpacing / 2) + 4;
            cardIndexHoverOver = -1;

            bannerMSG = "";
            if (new Rectangle(274, 42, 24, 24).Contains(func.getMouseLoc(mouseState, screenSize)))
            {
                bannerMSG = "go";
            }
            if (new Rectangle(274, 42 + 28, 24, 24).Contains(func.getMouseLoc(mouseState, screenSize)))
            {
                bannerMSG = "pass";
            }

            /*
            for (int i = 0; i < cards.Count; i++)
            {
                if(selectedCards.Contains(i))
                {
                    cardLocations[i] = new Point(cardLoc, 120);
                    if (mouseLocalX > cardLoc - 21 && mouseLocalX < cardLoc + 21 && func.getMouseLoc(mouseState, screenSize).Y > 130)
                    {
                        cardLoc += cardSpacingSelect;
                        cardIndexHoverOver = i;
                    }
                    else
                    {
                        cardLoc += cardSpacing;
                    }
                }
                else
                {
                    cardLocations[i] = new Point(cardLoc, 160);
                    if (mouseLocalX > cardLoc - 21 && mouseLocalX < cardLoc + 21 && func.getMouseLoc(mouseState, screenSize).Y > 130)
                    {
                        cardLoc += cardSpacingSelect;
                        cardIndexHoverOver = i;
                    }
                    else
                    {
                        cardLoc += cardSpacing;
                    }
                }

            }

            if(cardIndexHoverOver != -1 && leftMouserising())
            {
                selectedCards.Add(cardIndexHoverOver);
            }

            if(mouseState.RightButton == ButtonState.Pressed)
            {
                selectedCards.Clear();
            }
            if(leftMouserising() && selectedCards.Count > 0 && bannerMSG == "go")
            {
                List<card> activeCards = new List<card>();
                foreach(int index in selectedCards)
                {
                    activeCards.Add(cards[index]);
                }
                move outPut = new move(id, activeCards);

                //removeSelectedCards();
                this.cards.Sort((card1, card2) => card2.cardValue.CompareTo(card1.cardValue));

                selectedCards.Clear();
                return outPut;
            }
            else if(leftMouserising() && bannerMSG == "pass")
            {
                return new move(id, new List<card>());
            }*/

            return null;
        }
        const int cardSpacing = 14;
        const int cardSpacingSelect = 42;
        const int cardSize = 64;
        //320, 180
        //42,60
        public void draw(SpriteBatch spriteBatch)
        {
            Vector2 offset = new Vector2(0, (id == currentPlayerID ? 0 : 24));
            for (int i = 0; i < cards.Count; i++)
            {
                //drawCard(cards[i], cardLocations[i].ToVector2() + offset, spriteBatch, 0, 0, 0);
            }

            for(int i = 0; i < table.Count; i++)
            {
                drawCard(table[i], new Vector2(79 + i * 48, 67), spriteBatch, 0, 0, 0);
            }


        }

        private void removeSelectedCards()
        {
            selectedCards.Sort((index1, index2) => index1.CompareTo(index2));

            for (int i = selectedCards.Count - 1; i >= 0; i--)
            {
                cards.RemoveAt(selectedCards[i]);
            }
        }


        private bool leftMouserising()
        {
            return mouseState.LeftButton == ButtonState.Pressed && prevMouseState.LeftButton == ButtonState.Released;
        }



        private void drawCard(card card, Vector2 location, SpriteBatch spriteBatch, float rotationX, float rotationY, float rotationZ)
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

            Texture2D texture = textures.get("cardmap");

            float colorUse = Math.Min(distanceFrom90Z, distanceFrom90Y) * 255;
            Color color = new Color((byte)colorUse, (byte)colorUse, (byte)colorUse);

            Rectangle sourceRectangle = rotationY > 90 || rotationZ > 90 ? new Rectangle(832, 64, 64, 64) : new Rectangle(card.cardValue * 64, (int)card.suit * 64, 64, 64);


            spriteBatch.Draw(texture, new Rectangle((int)location.X, (int)location.Y, (int)(cardSize * distanceFrom90Y), (int)(cardSize * distanceFrom90Z)), sourceRectangle, color, MathHelper.ToRadians(rotationX), new Vector2(sourceRectangle.Width / 2f, sourceRectangle.Height / 2f), SpriteEffects.None, 1);
        }
    }

    public struct card
    {
        public int cardID;

        public card(cardTypes suit, int value)
        {
            cardID = (int)suit * 13 + value;
        }

        public card(int cardID)
        {
            this.cardID = cardID;
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
    }

    public enum cardTypes
    {
        diamonds = 0,
        hearts = 1,
        spade = 2,
        clubs = 3,
    }

    public struct move
    {
        public int ID;
        public List<card> cards;
        public int cardsLeft;

        public move(int id, List<card> cards, int cardsLeft)
        {
            this.ID = id;
            this.cards = cards;
            this.cardsLeft = cardsLeft;
        }
    }
}
