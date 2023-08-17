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
        public int[] cc;
        public string[] name;

        public initalisationPacket(card[] cards, int[] id, string[] name, int[] cardC)
        {
            this.cards = cards;
            this.id = id;
            this.cc = cardC;
            this.name = name;
        }
    }
}
