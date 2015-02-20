using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Uno
{
    public static class Deck
    {
        public static List<Card> thedeck = new List<Card>();
        public static Card faceup;
        public static char color;
        
        public static void newDeck()
        {
            thedeck = new List<Card>();
            for (int i = 0; i < 10; i++)
            {
                thedeck.Add(new Card('r', i.ToString()));
                thedeck.Add(new Card('r', i.ToString()));
                thedeck.Add(new Card('g', i.ToString()));
                thedeck.Add(new Card('g', i.ToString()));
                thedeck.Add(new Card('b', i.ToString()));
                thedeck.Add(new Card('b', i.ToString()));
                thedeck.Add(new Card('y', i.ToString()));
                thedeck.Add(new Card('y', i.ToString()));
            }
            thedeck.Add(new Card('r', "dr2"));
            thedeck.Add(new Card('r', "dr2"));
            thedeck.Add(new Card('g', "dr2"));
            thedeck.Add(new Card('g', "dr2"));
            thedeck.Add(new Card('b', "dr2"));
            thedeck.Add(new Card('b', "dr2"));
            thedeck.Add(new Card('y', "dr2"));
            thedeck.Add(new Card('y', "dr2"));

            thedeck.Add(new Card('r', "r"));
            thedeck.Add(new Card('r', "r"));
            thedeck.Add(new Card('g', "r"));
            thedeck.Add(new Card('g', "r"));
            thedeck.Add(new Card('b', "r"));
            thedeck.Add(new Card('b', "r"));
            thedeck.Add(new Card('y', "r"));
            thedeck.Add(new Card('y', "r"));

            thedeck.Add(new Card('r', "s"));
            thedeck.Add(new Card('r', "s"));
            thedeck.Add(new Card('g', "s"));
            thedeck.Add(new Card('g', "s"));
            thedeck.Add(new Card('b', "s"));
            thedeck.Add(new Card('b', "s"));
            thedeck.Add(new Card('y', "s"));
            thedeck.Add(new Card('y', "s"));

            thedeck.Add(new Card('r', "wild"));
            thedeck.Add(new Card('r', "wild"));
            thedeck.Add(new Card('g', "wild"));
            thedeck.Add(new Card('g', "wild"));
            thedeck.Add(new Card('b', "wild"));
            thedeck.Add(new Card('b', "wild"));
            thedeck.Add(new Card('y', "wild"));
            thedeck.Add(new Card('y', "wild"));

            thedeck.Add(new Card('r', "wdr4"));
            thedeck.Add(new Card('r', "wdr4"));
            thedeck.Add(new Card('g', "wdr4"));
            thedeck.Add(new Card('g', "wdr4"));
            thedeck.Add(new Card('b', "wdr4"));
            thedeck.Add(new Card('b', "wdr4"));
            thedeck.Add(new Card('y', "wdr4"));
            thedeck.Add(new Card('y', "wdr4"));
        }

        public static void drawCard(int index)
        {
            if (thedeck.Count == 0)
                newDeck();

            Random rand = new Random();
            int num;
            num = rand.Next(thedeck.Count);
            UnoGame.players[index].hand.Add(thedeck[num]);
            thedeck.RemoveAt(num);
            UnoGame.writeToLog("drawing card for turnindex " + index.ToString() + " from deck[" + num.ToString() + "]");
        }

        public static bool IsValid(string ucard)
        {
            if (ucard.Length < 2 || ucard.Length > 4)
                return false;
            if (ucard == "wild" || ucard == "wdr4")
                return true;
            if (ucard[0] != 'r' && ucard[0] != 'b' && ucard[0] != 'g' && ucard[0] != 'y')
                return false;
            if (ucard.EndsWith("dr2"))
                return true;

            int num = -1;
            bool parsed = int.TryParse(ucard[1].ToString(), out num);
            if (parsed)
                return true;
            if (ucard[1] == 'r' || ucard[1] == 's')
                return true;
            if (ucard.Length == 3 && ucard[1] == 'd' && ucard[2] == '2')
                return true;
            return false;
        }

        public static Card parse(string ucard)
        {
            if (ucard == "wild")
                return new Card('g', "wild");
            else if (ucard == "wdr4")
                return new Card('g', "wdr4");
            else if (ucard.EndsWith("dr2"))
                return new Card(ucard[0], "dr2");
            else
                return new Card(ucard[0], ucard[1].ToString());
        }

    }
}
