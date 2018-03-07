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

		private static Random rand = new Random();

		public static void NewDeck()
		{
			thedeck = new List<Card>();
			for (int i = 0; i < Card.rankValues.Count; i++)
			{
				thedeck.Add(new Card('r', Card.rankValues[i]));
				thedeck.Add(new Card('r', Card.rankValues[i]));
				thedeck.Add(new Card('g', Card.rankValues[i]));
				thedeck.Add(new Card('g', Card.rankValues[i]));
				thedeck.Add(new Card('b', Card.rankValues[i]));
				thedeck.Add(new Card('b', Card.rankValues[i]));
				thedeck.Add(new Card('y', Card.rankValues[i]));
				thedeck.Add(new Card('y', Card.rankValues[i]));
			}
		}

		public static Card DrawCard(int index)
		{
			if (thedeck.Count == 0)
				NewDeck();

			int num;
			num = rand.Next(thedeck.Count);
			Card card = thedeck[num];
			UnoGame.players[index].hand.Add(card);
			UnoGame.players[index].hand.Sort(Card.SortCards);
			thedeck.RemoveAt(num);
			return card;
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

		public static Card Parse(string ucard)
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
