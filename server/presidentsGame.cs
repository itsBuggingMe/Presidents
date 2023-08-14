using common;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using System.Diagnostics;
using System.Net.Sockets;

namespace server
{
    public class presidentsGame
    {
        public player[] players;
        public List<card> garbagePile;
        public List<card> table;

        public List<int> placements;

        int playerTurn = 0;
        int idOfLastGone;

        public presidentsGame(int[] ID, string[] name)
        {
            idOfLastGone = ID[0];
            placements = new List<int>();
            this.players = new player[ID.Length];
            for (int i = 0; i < this.players.Length; i++)
            {
                players[i] = new player(ID[i], name[i], null, null, Point.Zero, new List<card>());
            }

            garbagePile = new List<card>();
            table = new List<card>();
            for (int suit = 0; suit < 4; suit++)
            {
                for (int value = 0; value < 13; value++)
                {
                    garbagePile.Add(new card((cardTypes)suit, value));
                }
            }


            shuffle(garbagePile);

            for (int i = 0; i < 52; i++)
            {
                players[i % ID.Length].cards.Add(garbagePile[0]);
                garbagePile.RemoveAt(0);
            }
        }
        private bool hasGameEnded()
        {
            int activePlayers = 0;
            foreach(player player in players)
            {
                if(player.cards.Count != 0)
                {
                    activePlayers++;
                }
            }

            return activePlayers <= 1;
        }
        Random random = new Random();
        public string update(List<string> packets, string finalPacket)
        {
            for (int i = packets.Count - 1; i >= 0;i--)
            {
                if (packetEncodeDecode.tryDecodeObject(packets[i], out int id, out move move, "move"))
                {
                    packets.RemoveAt(i);

                    if (Move(move, out bool bombUsed))
                    {
                        if(move.cards.Count == 0)
                        {
                            if(players[playerTurn].id == idOfLastGone)
                            {
                                break;
                            }

                            idOfLastGone = players[playerTurn].id;
                        }

                        if (players[playerTurn].cards.Count == 0)
                        {
                            placements.Add(players[playerTurn].id * (bombUsed ? -1 : 1));
                        }
                        nextTurn();
                        hasGameEnded();

                        break;
                    }
                }

            }
            for (int i = packets.Count - 1; i >= 0; i--)
            {
                if (packets[i].StartsWith("$S:"))
                {
                    packets.RemoveAt(i);
                }
            }

            int[] idList = new int[players.Count()];
            int[] cardCount = new int[players.Count()];

            for (int i = 0; i < players.Count(); i++)
            {
                idList[i] = players[i].id;
                cardCount[i] = players[i].cards.Count();
            }
            gameState gameState = new gameState(table.ToArray(), garbagePile.ToArray(), idList, cardCount, players[playerTurn].id);
            return packetEncodeDecode.encodeObject(gameState, 0, "state");
        }

        public void nextTurn()
        {
            playerTurn++;
            playerTurn = playerTurn % players.Count();
            if (!players[playerTurn].isPlaying)
            {
                nextTurn();
            }

            if(idOfLastGone == players[playerTurn].id)
            {
                moveTableToGarbage();
            }
        }

        private void attemptFinish(move move)
        {
            if (table.Count == 0)
                return;

            foreach (card card in move.cards)
            {
                if (card.cardValue != move.cards[0].cardValue)
                {
                    return;
                }
            }
            foreach (card card in table)
            {
                if (card.cardValue != table[0].cardValue)
                {
                    return;
                }
            }
            if (table[0].cardValue != move.cards[0].cardValue)
                return;
            
            if(move.cards.Count + table.Count == 4)
            {
                moveTableToGarbage();
                while (players[playerTurn].id != move.ID)
                {
                    nextTurn();
                }
            }
        }

        public bool Move(move move, out bool bombUsed)
        {
            player activePlayer = players[playerTurn];
            bombUsed = false;

            if (move.cards.Count < 4 && move.cards.Count > 0)
            {
                //attemptFinish(move);
            }

            if (move.ID != activePlayer.id)
            {
                return false;
            }

            if (move.cards.Count == 0)
            {
                return true;
            }

            foreach (card requestCard in move.cards)
            {
                if (!func.listContainsCard(activePlayer.cards, requestCard.cardID))
                {
                    return false;
                }
            }//does he have the cards?


            move.cards.Sort((card1, card2) => card2.cardValue.CompareTo(card1.cardValue));
            //END MOVE VALIDATION

            //does he have a bomb?
            if (move.cards[move.cards.Count - 1].cardValue == 0)
            {
                List<card> bomb = new List<card>() { move.cards[move.cards.Count - 1] };
                move.cards.RemoveAt(move.cards.Count - 1);//HE HAS A BOMB WOOO
                if (move.cards.Count == 0)
                {
                    if (activePlayer.cards.Count > 1)//invalid move
                    {
                        return false;
                    }
                    else//he bombed self out
                    {
                        bombUsed = true;
                        moveTableToGarbage();
                        table.Add(bomb[0]);
                        return true;
                    }
                }//why only bomb?
                if (move.cards[move.cards.Count - 1].cardValue == 0)
                {
                    return false;//why two bombs??
                }
                moveTableToGarbage();

                return tryGenMove(move, bomb);
            }

            //does he have 4x bomb
            if (hasFourBomb(move, out move noBombMove, out List<card> fourBomb))
            {
                move = noBombMove;
                return tryGenMove(move, fourBomb);
            }

            //does the set match the table?
            return tryGenMove(move, new List<card>());
        }

        private static void shuffle<T>(List<T> list)
        {
            Random rng = new Random();
            int n = list.Count;

            while (n > 1)
            {
                n--;
                int k = rng.Next(n + 1);
                T value = list[k];
                list[k] = list[n];
                list[n] = value;
            }
        }


        private bool tryGenMove(move move, List<card> bomb)
        {
            if (move.cards.Count > 3)
            {
                return false;
            }

            if (move.cards.Count != table.Count && table.Count != 0)
            {

                return false;
            }

            foreach (card card in table)
            {
                Debug.WriteLine(card.cardValue);
            }

            switch (move.cards.Count)
            {
                case 1:
                    if (table.Count == 0 || move.cards[0].cardValue >= table[0].cardValue)
                    {
                        moveTableToGarbage();

                        foreach (card card in bomb)
                        {
                            table.Add(card);
                        }

                        table.Add(move.cards[0]);

                        players[playerTurn].cards.Remove(move.cards[0]);

                        table.Sort((card1, card2) => card2.cardValue.CompareTo(card1.cardValue));

                        return true;
                    }//single accepted
                    else
                    {
                        return false;
                    }//number not high enough
                case 2:
                    if (move.cards[0].cardValue == move.cards[1].cardValue && (table.Count == 0 || move.cards[0].cardValue >= table[0].cardValue))
                    {
                        moveTableToGarbage();

                        foreach (card card in bomb)
                        {
                            table.Add(card);
                        }

                        table.Add(move.cards[0]);
                        table.Add(move.cards[1]);


                        players[playerTurn].cards.Remove(move.cards[0]);
                        players[playerTurn].cards.Remove(move.cards[1]);

                        table.Sort((card1, card2) => card2.cardValue.CompareTo(card1.cardValue));

                        return true;
                    }
                    else
                    {
                        return false;
                    }//double does not match
                case 3:
                    if (threeOfaKind(move.cards[0].cardValue, move.cards[1].cardValue, move.cards[2].cardValue) && (table.Count == 0 || threeOfaKind(table[0].cardValue, table[1].cardValue, table[2].cardValue)))
                    {//3 of a kind
                        moveTableToGarbage();

                        foreach (card card in bomb)
                        {
                            table.Add(card);
                        }

                        table.Add(move.cards[0]);
                        table.Add(move.cards[1]);
                        table.Add(move.cards[2]);
                        players[playerTurn].cards.Remove(move.cards[0]);
                        players[playerTurn].cards.Remove(move.cards[1]);
                        players[playerTurn].cards.Remove(move.cards[2]);

                        table.Sort((card1, card2) => card2.cardValue.CompareTo(card1.cardValue));

                        return true;
                    }
                    else if (threeOfaKind(move.cards[0].cardValue, move.cards[1].cardValue + 1, move.cards[2].cardValue + 2) && (table.Count == 0 || (threeOfaKind(table[0].cardValue, table[1].cardValue + 1, table[2].cardValue + 2) && move.cards[0].cardValue >= table[0].cardValue)))
                    {//acendineg
                        moveTableToGarbage();

                        foreach (card card in bomb)
                        {
                            table.Add(card);
                        }

                        table.Add(move.cards[0]);
                        table.Add(move.cards[1]);
                        table.Add(move.cards[2]);
                        players[playerTurn].cards.Remove(move.cards[0]);
                        players[playerTurn].cards.Remove(move.cards[1]);
                        players[playerTurn].cards.Remove(move.cards[2]);

                        table.Sort((card1, card2) => card2.cardValue.CompareTo(card1.cardValue));

                        return true;
                    }
                    else
                    {
                        return false;
                    }
                default:
                    throw new Exception("Move has invalid card count");
            }
        }

        private void moveTableToGarbage()
        {
            while(table.Count > 0)
            {
                garbagePile.Add(table[0]);
                table.RemoveAt(0);
            }
        }

        private bool hasFourBomb(move move, out move moveNoBomb, out List<card> bomb)
        {
            bomb = new List<card>();

            if (move.cards.Count < 5 || move.cards.Count == 4)
            {
                moveNoBomb = new move();
                return false;
            }

            int cardType = -1;

            foreach (card card in move.cards)
            {
                bomb.Clear();
                foreach (card otherCard in move.cards)
                {
                    if (card.cardValue == otherCard.cardValue)
                    {
                        bomb.Add(otherCard);
                        if (bomb.Count == 4)
                        {
                            cardType = card.cardValue;
                            break;
                        }
                    }
                }

                if (cardType != -1)
                {
                    break;
                }
            }

            if (cardType == -1)
            {
                moveNoBomb = new move();
                return false;
            }

            moveNoBomb = new move(move.ID, new List<card>());
            foreach (var card in move.cards)
            {
                if (card.cardValue != cardType)
                {
                    moveNoBomb.cards.Add(card);
                }
            }

            return true;
        }

        private static bool threeOfaKind(int a, int b, int c)
        {
            return a == b && b == c;
        }
    }

}
