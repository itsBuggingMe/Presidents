using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Mail;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace common
{
    public struct gameState
    {
        public card[] table;
        public card[] garbage;
        public int[] id;
        public int[] cardNumber;
        public int currentPlayer;
        public byte alert;
        public gameState(card[] table, card[] garbage,int[] id, int[] cardNumber, int currentPlayer, byte alert)
        {
            this.table = table;
            this.id = id;
            this.cardNumber = cardNumber;
            this.garbage = garbage;
            this.currentPlayer = currentPlayer;
            this.alert = alert;
        }
    }
}
