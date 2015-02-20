using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TShockAPI;

namespace Uno
{
    public class UnoPlayer
    {
        public TSPlayer tsplayer;
        public List<Card> hand = new List<Card>();
        public int totalpoints;
        public bool hasdrawn;

        public UnoPlayer(TSPlayer player)
        {
            tsplayer = player;
            totalpoints = 0;
            hasdrawn = false;
        }

        public bool hasCard(string cardtxt)
        {
            foreach (Card card in hand)
            {
                if (card.ToString() == cardtxt)
                    return true;
            }

            return false;
        }
    }
}
