using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Timers;
using TShockAPI;

namespace Uno
{
	public static class UnoGame
	{
		public static Timer toStartGame;
		public static Timer turnTimer;

		public static string state = "inactive";
		public static List<UnoPlayer> players = new List<UnoPlayer>();
		public static List<TSPlayer> watchers = new List<TSPlayer>();
		public static int turnindex = 0;
		public static bool forward = true;
		private static bool playerleave = false;
		public static bool debug = false;

		#region gamestates
		public static void StartVote(TSPlayer starter)
		{
			state = "voting";
			TSPlayer.All.SendInfoMessage("[Uno] A game of Uno has been started by {0}! Use {1}uno join to join the game!", starter.Name, TShock.Config.CommandSpecifier);
			toStartGame = new Timer() { AutoReset = false, Enabled = false, Interval = TimeSpan.FromSeconds(30).TotalMilliseconds };
			toStartGame.Elapsed += whileVoting;
			toStartGame.Enabled = true;
			turnTimer = new Timer() { AutoReset = false, Enabled = false, Interval = TimeSpan.FromSeconds(60).TotalMilliseconds };
			turnTimer.Elapsed += EndOfTurn;
		}

		private static void whileVoting(object sender, ElapsedEventArgs args)
		{
			startGame();
		}

		private static void startGame()
		{
			if (players.Count < 2)
			{
				TSPlayer.All.SendErrorMessage("[Uno] Not enough players joined. The game will not start.");
				endGame("notenoughplayers");
				return;
			}
			TSPlayer.All.SendInfoMessage("[Uno] The joining period has ended! The game has begun.");
			state = "active";
			Deck.NewDeck();
			dealCards();
			goToNextTurn();
		}

		public static void JoinGame(TSPlayer joiner)
		{
			players.Add(new UnoPlayer(joiner));
			TSPlayer.All.SendInfoMessage("[Uno] {0} has joined the game!", joiner.Name);
			joiner.SendSuccessMessage("[Uno] You have joined the game!");
		}

		public static void LeaveGame(int leaver, string uname = null)
		{
			int removedIRCplayer = -1;

			if (leaver == -1)
			{
				for (int i = 0; i < players.Count; i++)
				{
					if (!players[i].tsplayer.RealPlayer)
					{
						if (uname != null)
						{
							if (uname == players[i].tsplayer.Name)
								removedIRCplayer = i;
							else
								continue;
						}
						try
						{
							players[i].tsplayer.SendInfoMessage("//Debug: Checking if you have left the game.");
						}
						catch
						{
							removedIRCplayer = i;
						}
					}
				}
			}

			for (int i = 0; i < players.Count; i++)
			{
				if (players[i].tsplayer.Index == leaver || i == removedIRCplayer)
				{
					playerleave = true;
					broadcast(players[i].tsplayer.Name + " has left the game.");
					if (state == "active" && CheckForWinner())
						return;
					players.RemoveAt(i);
					if (state == "active" && turnindex == i)
					{
						goToNextTurn();
					}
					else
						playerleave = false;
					return;
				}
			}


		}

		public static void stopGame(TSPlayer stopper)
		{
			foreach (UnoPlayer player in players)
			{
				player.tsplayer.SendInfoMessage("[Uno] The game has been force-ended by {0}.", stopper.Name);
			}

			endGame("stopped");
		}
		#endregion

		private static Random rand = new Random();

		private static void dealCards()
		{
			int index;

			for (int i = 0; i < players.Count; i++)
			{
				for (int j = 0; j < 7; j++)
				{
					index = rand.Next(Deck.thedeck.Count);
					players[i].hand.Add(Deck.thedeck[index]);
					Deck.thedeck.RemoveAt(index);
				}
				players[i].hand.Sort(Card.SortCards);
			}

			index = rand.Next(Deck.thedeck.Count);
			Deck.faceup = Deck.thedeck[index];
			Deck.color = Deck.thedeck[index].color;
			Deck.thedeck.RemoveAt(index);
		}

		public static void goToNextTurn(bool skip = false, int drawcards = 0)
		{
			if (!(playerleave && turnindex > players.Count - 1))
			{
				players[turnindex].hasdrawn = false;
			}

			if (forward)
			{
				if (turnindex + 1 >= players.Count)
					turnindex = 0;
				else if (!playerleave)
					turnindex++;
			}
			else
			{
				if (turnindex <= 0)
					turnindex = players.Count - 1;
				else
					turnindex--;
			}

			playerleave = false;

			if (skip)
			{
				return;
			}

			if (drawcards != 0)
				for (int i = 0; i < drawcards; i++)
				{
					Deck.DrawCard(turnindex);
				}

			broadcast("It is now " + players[turnindex].tsplayer.Name + "'s turn!");
			if (Deck.faceup.value == "wild" || Deck.faceup.value == "wdr4")
				players[turnindex].tsplayer.SendMessage("[Uno] It is now your turn! The current card is " + Deck.faceup.ToString() + ". The current color is " + Deck.color.ToString() + ".", Color.ForestGreen);
			else
				players[turnindex].tsplayer.SendMessage("[Uno] It is now your turn! The current card is " + Deck.faceup.ToString() + ".", Color.ForestGreen);
			string hand = string.Join(", ", players[turnindex].hand.Select(p => p));
			players[turnindex].tsplayer.SendMessage("[Uno] Your current cards are: " + hand, Color.ForestGreen);
			players[turnindex].tsplayer.SendMessage("[Uno] You have one minute to play a card ({0}play <card> [color]) or draw a card ({0}draw).".SFormat(TShock.Config.CommandSpecifier), Color.ForestGreen);
			turnTimer.Enabled = true;
		}

		private static void EndOfTurn(object sender, ElapsedEventArgs args)
		{
			players[turnindex].tsplayer.SendMessage("[Uno] You ran out of time! You are now drawing a card and passing your turn.", Color.ForestGreen);
			if (!players[turnindex].hasdrawn)
			{
				Deck.DrawCard(turnindex);
			}
			broadcast(players[turnindex].tsplayer.Name + " has timed out and automatically draws a card.");
			goToNextTurn();
		}

		public static bool CheckForWinner()
		{
			if ((!playerleave ? players.Count : (players.Count - 1)) < 2)
			{
				endGame("notenoughplayers");
				return true;
			}
			for (int i = 0; i < players.Count; i++)
			{
				if (players[i].hand.Count == 1 && i == turnindex)
					broadcast(players[i].tsplayer.Name + " has UNO!");
				if (players[i].hand.Count == 0)
				{
					broadcast(players[i].tsplayer.Name + " wins the game!");
					endGame("winner");
					return true;
				}
			}

			return false;
		}

		public static void endGame(string reason)
		{
			if (reason == "notenoughplayers")
			{
				broadcast("Not enough players to continue.");
			}
			if (reason == "winner")
			{
				for (int i = 0; i < players.Count; i++)
				{
					string cards = string.Join(", ", players[i].hand.Select(p => p.ToString()));
					players[i].totalpoints = 0;
					foreach (Card card in players[i].hand)
					{
						players[i].totalpoints += card.getValuePoint();
					}
					if (players[i].hand.Count != 0)
						broadcast(players[i].tsplayer.Name + "'s cards: " + cards + " (" + players[i].totalpoints.ToString() + " points)");

					UnoMain.Update(players[i].tsplayer.User.ID, players[i].totalpoints);
				}
			}
			turnTimer.Stop();
			watchers.Clear();
			players.Clear();
			state = "inactive";
			TSPlayer.All.SendInfoMessage("[Uno] A game of Uno is complete.");
		}

		public static void playCard(string card, string color)
		{
			Card playcard = new Card('r', "s");
			int cardindex = 0;
			int drawcards = 0;
			bool skip = false;
			for (int i = 0; i < players[turnindex].hand.Count; i++)
			{
				if (card == players[turnindex].hand[i].ToString())
				{
					playcard = players[turnindex].hand[i];
					cardindex = i;
				}
			}

			if (playcard.value == "wdr4")
			{
				if (color == null || (color != "r" && color != "g" && color != "y" && color != "b"))
				{
					players[turnindex].tsplayer.SendErrorMessage("[Uno] You must play a color with this card: {0}play wdr4 <r/g/b/y>", TShock.Config.CommandSpecifier);
					return;
				}
				broadcast(players[turnindex].tsplayer.Name + " plays " + playcard.ToString() + " and chooses the color " + color + "!");
				broadcast("The next player draws four cards.");
				drawcards = 4;
				Deck.color = color[0];
			}

			else if (playcard.value == "wild")
			{
				if (color == null || (color != "r" && color != "g" && color != "y" && color != "b"))
				{
					players[turnindex].tsplayer.SendErrorMessage("[Uno] You must play a color with this card: {0}play wild <r/g/b/y>", TShock.Config.CommandSpecifier);
					return;
				}
				broadcast(players[turnindex].tsplayer.Name + " plays " + playcard.ToString() + " and chooses the color " + color + "!");
				Deck.color = color[0];
			}

			else if (playcard.value == "r")
			{
				forward = !forward;
				broadcast(players[turnindex].tsplayer.Name + " plays " + playcard.ToString() + "! The turn order is reversed!");
				Deck.color = playcard.color;
			}

			else if (playcard.value == "dr2")
			{
				broadcast(players[turnindex].tsplayer.Name + " plays " + playcard.ToString() + "!");
				broadcast("The next player draws two cards.");
				drawcards = 2;
				Deck.color = playcard.color;
			}

			else if (playcard.value != "s")
			{
				broadcast(players[turnindex].tsplayer.Name + " plays " + playcard.ToString() + "!");
				Deck.color = playcard.color;
			}

			if (playcard.value == "s")
			{
				broadcast(players[turnindex].tsplayer.Name + " plays " + playcard.ToString() + "!");
				broadcast(players[turnindex].tsplayer.Name + " skips the next player's turn!");
				skip = true;
				Deck.color = playcard.color;
			}

			Deck.faceup = playcard;

			turnTimer.Enabled = false;

			players[turnindex].hand.RemoveAt(cardindex);

			if (CheckForWinner())
				return;

			if (skip)
			{
				goToNextTurn(true);
			}
			goToNextTurn(false, drawcards);
		}

		public static void broadcast(string message)
		{
			for (int i = 0; i < players.Count; i++)
			{
				players[i].tsplayer.SendMessage("[Uno] " + message, Color.ForestGreen);
			}

			foreach (TSPlayer player in watchers)
			{
				player.SendMessage("[Uno] " + message, Color.ForestGreen);
			}
		}

		public static bool isPlaying(int index, string uname = null)
		{
			for (int i = 0; i < players.Count; i++)
			{
				if (index == -1)
				{
					if (players[i].tsplayer.Name == uname)
						return true;
				}
				else if (index == players[i].tsplayer.Index)
					return true;
			}
			return false;
		}

		public static bool isCurrentTurn(int index, string uname)
		{
			if (index == -1)
			{
				if (uname == players[turnindex].tsplayer.Name)
					return true;
				else
					return false;
			}
			if (index == players[turnindex].tsplayer.Index)
				return true;
			else
				return false;
		}

		public static void displayHelp(TSPlayer player, List<string> param)
		{
			if (param.Count > 1)
			{
				if (Deck.IsValid(param[1]) || Deck.IsValid("g" + param[1]))
				{
					if (Deck.IsValid(param[1]))
					{
						Card card = Deck.Parse(param[1]);
						if (card.value == "wild")
						{
							player.SendInfoMessage("[Uno] 'wild' card syntax: {0}play wild <r/g/b/y>", TShock.Config.CommandSpecifier);
							player.SendInfoMessage("[Uno] Changes the color of play.");
						}
						else if (card.value == "wdr4")
						{
							player.SendInfoMessage("[Uno] 'wdr4' card syntax: {0}play wdr4 <r/g/b/y>", TShock.Config.CommandSpecifier);
							player.SendInfoMessage("[Uno] Changes the color of play and gives four cards to the next player.");
						}
						else if (card.value == "s")
						{
							player.SendInfoMessage("[Uno] '{0}s' card syntax: {1}play {0}s", card.color, TShock.Config.CommandSpecifier);
							player.SendInfoMessage("[Uno] Skips the next player's turn.");
						}
						else if (card.value == "r")
						{
							player.SendInfoMessage("[Uno] '{0}r' card syntax: {1}play {0}r", card.color, TShock.Config.CommandSpecifier);
							player.SendInfoMessage("[Uno] Reverses the order of play.");
						}
						else if (card.value == "dr2")
						{
							player.SendInfoMessage("[Uno] '{0}dr2' card syntax: {1}play {0}dr2", card.color, TShock.Config.CommandSpecifier);
							player.SendInfoMessage("[Uno] Gives two cards to the next player.");
						}
						else
						{
							player.SendInfoMessage("[Uno] '{0}{1}' card syntax: {2}play {0}{1}", card.color, card.value, TShock.Config.CommandSpecifier);
							player.SendInfoMessage("[Uno] No special ability.");
						}
					}
				}
				else
				{
					player.SendInfoMessage("Available commands:");
					if (player.Group.HasPermission("uno.mod"))
						player.SendInfoMessage("{0}uno <start/join/stop>: Starts, joins, or stops a game of Uno.", TShock.Config.CommandSpecifier);
					else
						player.SendInfoMessage("{0}uno <start/join>: Starts or joins a game of Uno.", TShock.Config.CommandSpecifier);
					player.SendInfoMessage("{0}play <card> [color]: Plays the selected card with the optional selected color.", TShock.Config.CommandSpecifier);
					player.SendInfoMessage("{0}draw: Draws a card. Cannot {0}pass without using this command.", TShock.Config.CommandSpecifier);
					player.SendInfoMessage("{0}pass: Passes a turn. Must {0}draw before using this command.", TShock.Config.CommandSpecifier);
					player.SendInfoMessage("{0}uno help [card]: Gives information about how to use the selected card.", TShock.Config.CommandSpecifier);
				}
			}
			else
			{
				player.SendInfoMessage("Available commands:");
				if (player.Group.HasPermission("uno.mod"))
					player.SendInfoMessage("{0}uno <start/join/stop>: Starts, joins, or stops a game of Uno.", TShock.Config.CommandSpecifier);
				else
					player.SendInfoMessage("{0}uno <start/join>: Starts or joins a game of Uno.", TShock.Config.CommandSpecifier);
				player.SendInfoMessage("{0}play <card> [color]: Plays the selected card with the optional selected color.", TShock.Config.CommandSpecifier);
				player.SendInfoMessage("{0}draw: Draws a card. Cannot {0}pass without using this command.", TShock.Config.CommandSpecifier);
				player.SendInfoMessage("{0}pass: Passes a turn. Must {0}draw before using this command.", TShock.Config.CommandSpecifier);
				player.SendInfoMessage("{0}uno help [card]: Gives information about how to use the selected card.", TShock.Config.CommandSpecifier);
			}
		}

		public static void writeToLog(string msg)
		{
			if (!debug)
				return;

			if (!File.Exists(@"tshock\uno.log"))
				File.Create(@"tshock\uno.log");
			using (StreamWriter write = new StreamWriter(@"tshock\uno.log", true))
			{
				write.WriteLine(msg);
			}
		}
	}
}
