using Microsoft.Xna.Framework.Audio;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace common
{
    public class SoundFX
    {
        Dictionary<string, SoundEffect> soundDictionary = new Dictionary<string, SoundEffect>();
        public SoundFX()
        {

        }

        public void addSound(string name, SoundEffect sound)
        {
            soundDictionary[name] = sound;
        }

        public void play(string name)
        {
            if (soundDictionary.TryGetValue(name, out SoundEffect sound))
            {
                sound.Play();
            }
            else
            {
                throw new Exception($"Sound File: {name} does not exist");
            }
        }
    }

    public class Textures
    {
        Dictionary<string, Texture2D> textureDictionary = new Dictionary<string, Texture2D>();
        public Textures()
        {
        }

        public void addTexture(string name, Texture2D texture)
        {
            textureDictionary[name] = texture;
        }

        public Texture2D get(string name)
        {
            if (textureDictionary.TryGetValue(name, out Texture2D texture))
            {
                return texture;
            }
            else
            {
                throw new Exception($"Texture2D File: {name} does not exist");
            }
        }
    }
}
