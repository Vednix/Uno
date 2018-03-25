using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TerrariaApi.Server;
using Terraria;
using TShockAPI;
using System.Data;
using MySql.Data.MySqlClient;
using Mono.Data.Sqlite;
using System.IO;
using TShockAPI.DB;
using System.Collections;
using TShockAPI.Hooks;

namespace Uno
{
	public class UPlayer
	{
		public int _userid;
		public int _totalgames;
		public int _totalwins;
		public int _totalpoints;
		public double _winpercent;
	}

	[ApiVersion(2, 1)]
	public class UnoMain : TerrariaPlugin
	{
		public override string Name { get { return "UnoPlugin"; } }
		public override string Author { get { return "Zaicon"; } }
		public override string Description { get { return "Plays a game of Uno!"; } }
		public override Version Version { get { return new Version(1, 1, 0, 0); } }

		private static IDbConnection db;

		public UnoMain(Main game)
			: base(game)
		{
			base.Order = 1;
		}

		public override void Initialize()
		{
			ServerApi.Hooks.GameInitialize.Register(this, OnInitialize);
			ServerApi.Hooks.ServerLeave.Register(this, OnLeave);
			AccountHooks.AccountDelete += OnAccDelete;
		}

		protected override void Dispose(bool Disposing)
		{
			if (Disposing)
			{
				ServerApi.Hooks.GameInitialize.Deregister(this, OnInitialize);
				ServerApi.Hooks.ServerLeave.Deregister(this, OnLeave);
				AccountHooks.AccountDelete -= OnAccDelete;
			}
			base.Dispose(Disposing);
		}

		private void OnInitialize(EventArgs args)
		{
			DBConnect();

			Commands.ChatCommands.Add(new Command("uno.play", UnoClass, "uno"));
			Commands.ChatCommands.Add(new Command("uno.play", UnoDraw, "draw"));
			Commands.ChatCommands.Add(new Command("uno.play", UnoPass, "pass"));
			Commands.ChatCommands.Add(new Command("uno.play", UnoPlay, "play"));
			Commands.ChatCommands.Add(new Command("uno.play", UnoCount, "count"));
			Commands.ChatCommands.Add(new Command("uno.play", UnoCards, "cards"));
			Commands.ChatCommands.Add(new Command("uno.play", UnoStats, "unostats"));
			Commands.ChatCommands.Add(new Command("uno.view", UnoView, "unoview") { AllowServer = false });
		}

		private void OnLeave(LeaveEventArgs args)
		{
			if (UnoGame.state != "inactive" && UnoGame.isPlaying(args.Who))
			{
				UnoGame.LeaveGame(args.Who);
			}
		}

		private void OnAccDelete(AccountDeleteEventArgs args)
		{
			db.Query("DELETE FROM Uno WHERE UserID=@0;", args.User.ID);
		}

		#region unogame
		private void UnoClass(CommandArgs args)
		{
			if (args.Parameters.Count > 0)
			{
				if (args.Parameters[0] == "help")
				{
					UnoGame.displayHelp(args.Player, args.Parameters);
					return;
				}
			}
			if (UnoGame.state == "inactive")
			{
				if (args.Parameters.Count == 1 && args.Parameters[0] == "start")
				{
					UnoGame.StartVote(args.Player);
					UnoGame.JoinGame(args.Player);
				}
				else
				{
					if (args.Parameters.Count > 0 && (args.Parameters[0] == "join" || args.Parameters[0] == "quit"))
						args.Player.SendErrorMessage("[Uno] No game running! Use {0}uno start to start a game of Uno!", TShock.Config.CommandSpecifier);
					else if (args.Parameters.Count > 0 && args.Parameters[0] == "stop" && args.Player.Group.HasPermission("uno.mod"))
						args.Player.SendErrorMessage("[Uno] No game running!");
					else
						args.Player.SendErrorMessage("[Uno] Invalid syntax! Use \"{0}uno start\" to start a game of Uno!", TShock.Config.CommandSpecifier);
				}
			}
			else if (UnoGame.state == "voting")
			{
				if (args.Parameters.Count == 1 && args.Parameters[0] == "join")
				{
					if (!UnoGame.isPlaying(args.Player.Index, args.Player.Name))
					{
						UnoGame.JoinGame(args.Player);
					}
					else
						args.Player.SendErrorMessage("[Uno] You have already joined this game of Uno!");
				}
				else if (args.Parameters.Count == 1 && args.Parameters[0] == "quit" && UnoGame.isPlaying(args.Player.Index, args.Player.Name))
				{
					UnoGame.LeaveGame(args.Player.Index, args.Player.Name);
				}
				else
				{
					if (args.Parameters.Count > 0 && args.Parameters[0] == "start")
						args.Player.SendErrorMessage("[Uno] A game of Uno is already started! Use {0}uno join to join the game!", TShock.Config.CommandSpecifier);
					else if (args.Parameters.Count > 0 && args.Parameters[0] == "stop" && args.Player.Group.HasPermission("uno.mod"))
					{
						UnoGame.toStartGame.Enabled = false;
						UnoGame.stopGame(args.Player);
					}
					else
						args.Player.SendErrorMessage("[Uno] Invalid syntax! Use \"{0}game join\" to join the game of Uno!", TShock.Config.CommandSpecifier);
				}
			}
			else if (UnoGame.state == "active")
			{
				if (args.Parameters.Count == 1 && args.Parameters[0] == "stop" && args.Player.Group.HasPermission("uno.mod"))
				{
					UnoGame.stopGame(args.Player);
				}
				else if (args.Parameters.Count == 1 && args.Parameters[0] == "quit" && UnoGame.isPlaying(args.Player.Index, args.Player.Name))
				{
					UnoGame.LeaveGame(args.Player.Index, args.Player.Name);
				}
				else if (args.Parameters.Count == 2 && args.Parameters[0] == "kick" && args.Player.Group.HasPermission("uno.mod"))
				{
					List<TSPlayer> listplayers = TShock.Utils.FindPlayer(args.Parameters[1]);

					if (listplayers.Count == 0)
						args.Player.SendErrorMessage("No players matched.");
					else if (listplayers.Count > 1)
						TShock.Utils.SendMultipleMatchError(args.Player, listplayers.Select(p => p.Name));
					else
					{
						TSPlayer kicked = listplayers[0];
						UnoGame.LeaveGame(kicked.Index, kicked.Name);
						UnoGame.broadcast(args.Player.Name + " has kicked " + kicked.Name + " from the game of Uno.");
					}
				}
				else
				{
					if (args.Parameters.Count > 0 && args.Parameters[0] == "start")
					{
						if (UnoGame.isPlaying(args.Player.Index, args.Player.Name))
							args.Player.SendErrorMessage("[Uno] You are already playing a game of Uno!");
						else
							args.Player.SendErrorMessage("[Uno] A game of Uno is already in progress!");
					}
					else if (args.Parameters.Count > 0 && args.Parameters[0] == "join")
						args.Player.SendErrorMessage("[Uno] You cannot join an ongoing game!");
					else
						args.Player.SendErrorMessage("[Uno] Invalid syntax!");
				}
			}
		}

		private void UnoDraw(CommandArgs args)
		{
			if (UnoGame.state == "active" && UnoGame.isPlaying(args.Player.Index, args.Player.Name) && UnoGame.isCurrentTurn(args.Player.Index, args.Player.Name) && !UnoGame.players[UnoGame.turnindex].hasdrawn)
			{
				UnoGame.broadcast(args.Player.Name + " draws a card.");
				var drawnCard = Deck.DrawCard(UnoGame.turnindex);
				args.Player.SendSuccessMessage("[Uno] You have drawn: {0}", drawnCard);
				UnoGame.players[UnoGame.turnindex].hasdrawn = true;
			}
			else if (UnoGame.state != "active" || !UnoGame.isPlaying(args.Player.Index, args.Player.Name))
				args.Player.SendErrorMessage("[Uno] You are not in a game!");
			else if (!UnoGame.isCurrentTurn(args.Player.Index, args.Player.Name))
				args.Player.SendErrorMessage("[Uno] It is not your turn!");
			else if (UnoGame.players[UnoGame.turnindex].hasdrawn)
				args.Player.SendErrorMessage("[Uno] You have already drawn! You must {0}pass if you cannot play a card!", TShock.Config.CommandSpecifier);
			else
			{
				args.Player.SendErrorMessage("[Uno] An error occured.");
				TShock.Log.Error("An error with Uno occurred!");
			}
		}

		private void UnoPass(CommandArgs args)
		{
			if (UnoGame.state == "active" && UnoGame.isPlaying(args.Player.Index, args.Player.Name) && UnoGame.isCurrentTurn(args.Player.Index, args.Player.Name) && UnoGame.players[UnoGame.turnindex].hasdrawn)
			{
				UnoGame.broadcast(args.Player.Name + " passes " + (args.Player.TPlayer.Male ? "his" : "her") + " turn.");
				args.Player.SendSuccessMessage("[Uno] You have passed your turn.");
				UnoGame.turnTimer.Enabled = false;
				UnoGame.goToNextTurn();
			}
			else if (UnoGame.state != "active" || !UnoGame.isPlaying(args.Player.Index, args.Player.Name))
				args.Player.SendErrorMessage("[Uno] You are not in a game!");
			else if (!UnoGame.isCurrentTurn(args.Player.Index, args.Player.Name))
				args.Player.SendErrorMessage("[Uno] It is not your turn!");
			else if (!UnoGame.players[UnoGame.turnindex].hasdrawn)
				args.Player.SendErrorMessage("[Uno] You must {0}draw before you can {0}pass!", TShock.Config.CommandSpecifier);
			else
			{
				args.Player.SendErrorMessage("[Uno] An error occured.");
				TShock.Log.Error("An error with Uno occurred!");
			}
		}

		private void UnoPlay(CommandArgs args)
		{
			if (UnoGame.state == "active" && UnoGame.isPlaying(args.Player.Index, args.Player.Name) && UnoGame.isCurrentTurn(args.Player.Index, args.Player.Name))
			{
				if (args.Parameters.Count > 0 && args.Parameters.Count < 3)
				{
					if (!Deck.IsValid(args.Parameters[0]))
					{
						args.Player.SendErrorMessage("[Uno] That is not a valid card.");
						return;
					}
					if (!UnoGame.players[UnoGame.turnindex].hasCard(args.Parameters[0]))
					{
						args.Player.SendErrorMessage("[Uno] You do not have that card.");
						return;
					}
					Card card = Deck.Parse(args.Parameters[0]);
					if (card.color != Deck.color && card.value != Deck.faceup.value && card.value != "wild" && card.value != "wdr4")
					{
						args.Player.SendErrorMessage("[Uno] You cannot play that card!");
						return;
					}

					UnoGame.playCard(args.Parameters[0], (args.Parameters.Count == 2 ? args.Parameters[1] : null));
				}
				else
				{
					args.Player.SendErrorMessage("[Uno] Invalid Syntax: {0}play <card> [color]", TShock.Config.CommandSpecifier);
					args.Player.SendErrorMessage("[Uno] Use {0}uno help <card> for help on how to play each card.", TShock.Config.CommandSpecifier);
				}
			}
			else if (UnoGame.state != "active" || !UnoGame.isPlaying(args.Player.Index, args.Player.Name))
				args.Player.SendErrorMessage("[Uno] You are not in a game!");
			else if (UnoGame.isCurrentTurn(args.Player.Index, args.Player.Name))
				args.Player.SendErrorMessage("[Uno] It is not your turn!");
			else
			{
				args.Player.SendErrorMessage("[Uno] An error occured.");
				TShock.Log.Error("An error with Uno occurred!");
			}
		}

		private void UnoCount(CommandArgs args)
		{
			if (UnoGame.state == "active")
			{
				string msg = "";

				if (UnoGame.forward)
					for (int i = 0; i < UnoGame.players.Count; i++)
					{
						msg += "[";
						msg += UnoGame.players[i].tsplayer.Name;
						msg += ", ";
						msg += UnoGame.players[i].hand.Count.ToString();
						msg += "] ";
					}
				else
					for (int i = UnoGame.players.Count - 1; i >= 0; i--)
					{
						msg += "[";
						msg += UnoGame.players[i].tsplayer.Name;
						msg += ", ";
						msg += UnoGame.players[i].hand.Count.ToString();
						msg += "] ";
					}

				args.Player.SendInfoMessage("[Uno] " + msg);
			}
			else
			{
				args.Player.SendErrorMessage("[Uno] No game is running!");
			}
		}

		private void UnoCards(CommandArgs args)
		{
			if (UnoGame.state == "active" && UnoGame.isPlaying(args.Player.Index, args.Player.Name))
			{
				for (int i = 0; i < UnoGame.players.Count; i++)
				{
					if (UnoGame.players[i].tsplayer.Name == args.Player.Name)
					{
						args.Player.SendInfoMessage("[Uno] Your current cards are {0}. The current card is {1}{2}.", string.Join(" ", UnoGame.players[i].hand.Select(p => p.ToOutput())), Deck.faceup.ToOutput(), (Deck.faceup.value == "wild" || Deck.faceup.value == "wdr4" ? " " + Deck.color : ""));
					}
				}
			}
		}
		#endregion

		private void UnoView(CommandArgs args)
		{
			if (UnoGame.state != "active")
			{
				args.Player.SendErrorMessage("No ongoing game of Uno to view!");
				return;
			}

			if (!UnoGame.isPlaying(args.Player.Index))
			{
				for (int i = 0; i < UnoGame.watchers.Count; i++)
					if (UnoGame.watchers[i].Index == args.Player.Index)
					{
						UnoGame.watchers.RemoveAt(i);
						args.Player.SendSuccessMessage("You are no longer viewing the game of Uno.");
						return;
					}
				UnoGame.watchers.Add(args.Player);
				args.Player.SendSuccessMessage("You are now viewing the game of Uno!");
			}
			else
				args.Player.SendErrorMessage("You're already playing in the game!");
		}

		#region database

		private void UnoStats(CommandArgs args)
		{
			if (args.Parameters.Count == 0)
			{
				TShockAPI.DB.User user = TShock.Users.GetUserByName(args.Player.User.Name);
				displayStats(args.Player, user);
			}
			else if (args.Parameters.Count == 1 && args.Parameters[0] == "all")
			{
				List<UPlayer> unsorted = new List<UPlayer>();
				using (QueryResult reader = db.QueryReader("SELECT * FROM Uno"))
				{
					while (reader.Read())
					{
						unsorted.Add(new UPlayer() { _totalgames = reader.Get<int>("TotalGames"), _totalpoints = reader.Get<int>("TotalPoints"), _totalwins = reader.Get<int>("TotalWins"), _userid = reader.Get<int>("UserID"), _winpercent = ((double)reader.Get<int>("TotalWins") / (double)reader.Get<int>("TotalGames") * 100) });
					}
				}

				unsorted.OrderBy(p => p._winpercent);
				unsorted.Reverse();

				args.Player.SendInfoMessage("[Uno] Top three UnoStats players with more than 5 games:");

				int displayed = 0;
				for (int i = 0; i < unsorted.Count; i++)
				{
					if (unsorted[i]._totalwins <= 5)
						continue;
					displayed++;

					string winpercentage = unsorted[i]._winpercent.ToString();
					if (winpercentage.Length > 6)
						winpercentage = winpercentage.Remove(6);

					args.Player.SendInfoMessage("[Uno] {0} has won {1} games out of {2} ({3}%), with a point total of {4}.", TShock.Users.GetUserByID(unsorted[i]._userid).Name, unsorted[i]._totalwins.ToString(), unsorted[i]._totalgames.ToString(), winpercentage, unsorted[i]._totalpoints.ToString());
				}
			}
			else
			{
				string para = string.Join(" ", args.Parameters.Select(p => p));
				TShockAPI.DB.User user = TShock.Users.GetUserByName(para);
				if (user != null)
					displayStats(args.Player, user);
				else
					args.Player.SendErrorMessage("Unknown Player!");
			}
		}

		private void displayStats(TSPlayer who, TShockAPI.DB.User user)
		{
			string query = "SELECT * FROM Uno WHERE UserID = " + user.ID.ToString();
			int totalgames = -1;
			int totalwins = -1;
			int totalpoints = -1;
			using (QueryResult reader = db.QueryReader(query))
			{
				while (reader.Read())
				{
					totalgames = reader.Get<int>("TotalGames");
					totalwins = reader.Get<int>("TotalWins");
					totalpoints = reader.Get<int>("TotalPoints");
				}
			}
			if (totalgames == -1)
				who.SendErrorMessage("No Uno stats to display.");
			else
			{
				who.SendInfoMessage("{0} has won {1} game(s) out of {2} with a point total of {3}.", user.Name, totalwins.ToString(), totalgames.ToString(), totalpoints.ToString());
			}
		}

		public static void Update(int userid, int totalPoints)
		{
			string query = "SELECT * FROM Uno WHERE UserID = @0;";

			int currentPoints = 0;
			int totalGames = 0;
			int totalWins = 0;

			using (var reader = db.QueryReader(query, userid))
			{
				if (reader.Read())
				{
					currentPoints = reader.Get<int>("TotalPoints");
					totalGames = reader.Get<int>("TotalGames");
					totalWins = reader.Get<int>("TotalWins");
				}
			}

			//if they don't have an entry and they won the game
			if (totalGames == 0 && totalPoints == 0)
				db.Query("INSERT INTO Uno (UserID, TotalGames, TotalWins, TotalPoints) VALUES (@0, @1, @2, @3);", userid, 1, 1, 0);
			//if they don't have an entry and they lost the game
			else if (totalGames == 0)
				db.Query("INSERT INTO Uno (UserID, TotalGames, TotalWins, TotalPoints) VALUES (@0, @1, @2, @3);", userid, 1, 0, totalPoints);
			//if they won the game
			else if (totalPoints == 0)
				db.Query($"UPDATE Uno SET TotalWins = @0, TotalGames = @1 WHERE UserID = @2;", totalWins + 1, totalGames + 1, userid);
			//if they lost the game
			else
				db.Query($"UPDATE Uno SET TotalPoints = @0, TotalGames = @1 WHERE UserID = @2;", currentPoints + totalPoints, totalGames + 1, userid);
		}

		private void DBConnect()
		{
			switch (TShock.Config.StorageType.ToLower())
			{
				case "mysql":
					string[] dbHost = TShock.Config.MySqlHost.Split(':');
					db = new MySqlConnection()
					{
						ConnectionString = string.Format("Server={0}; Port={1}; Database={2}; Uid={3}; Pwd={4};",
							dbHost[0],
							dbHost.Length == 1 ? "3306" : dbHost[1],
							TShock.Config.MySqlDbName,
							TShock.Config.MySqlUsername,
							TShock.Config.MySqlPassword)

					};
					break;

				case "sqlite":
					string sql = Path.Combine(TShock.SavePath, "Uno.sqlite");
					db = new SqliteConnection(string.Format("uri=file://{0},Version=3", sql));
					break;

			}

			SqlTableCreator sqlcreator = new SqlTableCreator(db, db.GetSqlType() == SqlType.Sqlite ? (IQueryBuilder)new SqliteQueryCreator() : new MysqlQueryCreator());

			sqlcreator.EnsureTableStructure(new SqlTable("Uno",
				new SqlColumn("UserID", MySqlDbType.Int32) { Primary = true, Unique = true, Length = 4 },
				new SqlColumn("TotalGames", MySqlDbType.Int32) { Length = 5 },
				new SqlColumn("TotalWins", MySqlDbType.Int32) { Length = 5 },
				new SqlColumn("TotalPoints", MySqlDbType.Int32) { Length = 8 }));
		}
		#endregion
	}
}


