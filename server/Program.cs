using common;
using System.Security.AccessControl;

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