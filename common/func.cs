using Microsoft.Xna.Framework.Input;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using common;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace common
{
    public static class func
    {
        public static Vector2 getMouseLoc(MouseState mouseState, Point screenSize)
        {
            return new Vector2((mouseState.X / (float)screenSize.X) * 320f, (mouseState.Y / (float)screenSize.Y) * 180f);
        }
        public static bool listContainsCard(List<card> cards, int id)
        {
            foreach (card card in cards)
            {
                if (card.cardID == id)
                {
                    return true;
                }
            }
            return false;
        }

        public static bool has4OfType(int cardValue, List<card> cards)
        {
            int count = 0;
            foreach (card card in cards)
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
    }
}
