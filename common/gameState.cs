using System;
using System.Collections.Generic;
using System.Linq;
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

        public gameState(card[] table, card[] garbage,int[] id, int[] cardNumber, int currentPlayer)
        {
            this.table = table;
            this.id = id;
            this.cardNumber = cardNumber;
            this.garbage = garbage;
            this.currentPlayer = currentPlayer;
        }
    }
}
