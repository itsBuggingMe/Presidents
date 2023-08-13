using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace common
{
    public struct initalisationPacket
    {
        public card[] cards;
        public int[] id;
        public string[] name;

        public initalisationPacket(card[] cards, int[] id, string[] name)
        {
            this.cards = cards;
            this.id = id;
            this.name = name;
        }
    }
}
