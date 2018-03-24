using System.Collections.Generic;

namespace Uno
{
	public class Card
	{
		public char color;
		public string value;

		public static List<char> colorValues = new List<char>() { 'b', 'g', 'r', 'y' };
		public static List<string> rankValues = new List<string>() { "0", "1", "2", "3", "4", "5", "6", "7", "8", "9", "r", "s", "dr2", "wild", "wdr4" };

		public Card(char Color, string Value)
		{
			color = Color;
			value = Value;
		}

		public int getValuePoint()
		{
			switch (this.value)
			{
				case "0":
					return 0;
				case "1":
					return 1;
				case "2":
					return 2;
				case "3":
					return 3;
				case "4":
					return 4;
				case "5":
					return 5;
				case "6":
					return 6;
				case "7":
					return 7;
				case "8":
					return 8;
				case "9":
					return 9;
				case "dr2":
					return 20;
				case "r":
					return 20;
				case "wild":
					return 50;
				case "s":
					return 20;
				case "wdr4":
					return 50;
				default:
					return 0;
			}
		}

		public static int SortCards(Card card1, Card card2)
		{
			if (card1.value == "wild")
			{
				switch (card2.value)
				{
					case "wild":
						return 0;
					case "wdr4":
						return -1;
					default:
						return 1;
				}
			}
			else if (card1.value == "wdr4")
			{
				switch (card2.value)
				{
					case "wdr4":
						return 0;
					default:
						return 1;
				}
			}
			else if (card2.value == "wild" || card2.value == "wdr4")
				return -1;
			else if (card1.color != card2.color)
				return colorValues.IndexOf(card1.color).CompareTo(colorValues.IndexOf(card2.color));
			else
				return rankValues.IndexOf(card1.value).CompareTo(rankValues.IndexOf(card2.value));
		}

		public override string ToString()
		{
			if (value == "wild")
				return $"[c/ff0000:w][c/00ff00:i][c/4c4cff:l][c/ffff00:d]";
			if (value == "wdr4")
				return $"[c/ff0000:w][c/00ff00:d][c/4c4cff:r][c/ffff00:4]";

			switch (color)
			{
				case 'r':
					return $"[c/ff0000:r{value}]";
				case 'g':
					return $"[c/00ff00:g{value}]";
				case 'b':
					return $"[c/4c4cff:b{value}]";
				case 'y':
					return $"[c/ffff00:y{value}]";
			}

			return "??";
		}
	}
}
