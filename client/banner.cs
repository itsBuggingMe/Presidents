using common;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace client
{
    public class banner
    {
        public bool visible = false;
        public string text;
        Texture2D bannerImage;
        SpriteFont font;
        int decayTimer;
        float opacity = 1;
        public banner(SpriteFont font, Texture2D banner)
        {
            this.font = font;
            this.bannerImage = banner;
        }

        public void tick()
        {
            if (decayTimer > 0)
            {
                decayTimer--;
                visible = true;
                if (decayTimer == 0)
                {
                    visible = false;
                }
            }

            if (decayTimer < 4)
            {
                opacity = decayTimer / 8f;
            }
            else
            {
                opacity = 1;
            }
        }

        public void addMsg(int time, string text)
        {
            if (time != -1)
            {
                decayTimer = time;
            }
            this.text = text;
            visible = true;

        }

        public void draw(SpriteBatch spriteBatch, Point screenSize)
        {
            MouseState mouseState = Mouse.GetState();
            Point Position = func.getMouseLoc(mouseState, screenSize).ToPoint();
            Position.X -= 1;
            if (visible)
            {
                string count = text;
                int length = (int)((font.MeasureString(count).X)/2);
                spriteBatch.Draw(bannerImage, new Rectangle(Position.X-(length+1), Position.Y-10, 1, 10), new Rectangle(0, 0, 1, 10), Color.White * opacity);
                spriteBatch.Draw(bannerImage, new Rectangle(Position.X-length, Position.Y-10, length, 10), new Rectangle(1, 0, 1, 10), Color.White * opacity);
                spriteBatch.Draw(bannerImage, new Rectangle(Position.X, Position.Y - 10, 1, 10), new Rectangle(0, 0, 1, 10), Color.White * opacity);

                spriteBatch.DrawString(font, count, new Vector2(Position.X - length + 2, Position.Y-9), Color.White * opacity, 0, Vector2.Zero, 0.5f, SpriteEffects.None, 1);
            }
        }
    }
}
