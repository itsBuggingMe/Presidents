using common;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;

namespace server
{
    internal class Program
    {
        static void Main(string[] args)
        {
            server server = new server(14242, "prez");
            server.start();
        }
    }
}