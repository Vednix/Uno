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
            TSPlayer.All.SendInfoMessage("[Uno] A game of Uno has been started by {0}! Use /uno join to join the game!", starter.Name);
            toStartGame = new Timer() { AutoReset = false, Enabled = false, Interval = TimeSpan.FromSeconds(30).TotalMilliseconds };
            toStartGame.Elapsed += whileVoting;
            toStartGame.Enabled = true;
            turnTimer = new Timer() { AutoReset = false, Enabled = false, Interval = TimeSpan.FromSeconds(60).TotalMilliseconds };
            turnTimer.Elapsed += endOfTurn;
            //debug
            writeToLog("StartVote() called by " + starter.UserAccountName + ". toStartGame enabled.");
        }

        private static void whileVoting(object sender, ElapsedEventArgs args)
        {
            startGame();
            //debug
            writeToLog("Joining period ended. Starting game.");
        }

        private static void startGame()
        {
            if (players.Count < 2)
            {
                //debug
                writeToLog("Not enough players to join. Ending game.");
                TSPlayer.All.SendErrorMessage("[Uno] Not enough players joined. The game will not start.");
                endGame("notenoughplayers");
                return;
            }
            //debug
            writeToLog("Game starting. Enough players have joined.");
            TSPlayer.All.SendInfoMessage("[Uno] The joining period has ended! The game has begun.");
            state = "active";
            Deck.newDeck();
            //debug
            writeToLog("A new deck has been created.");
            dealCards();
            //debug
            writeToLog("Cards have been dealt.");
            goToNextTurn();
        }

        public static void JoinGame(TSPlayer joiner)
        {
            //debug
            writeToLog(joiner.UserAccountName + " has joined the game. Index: " + joiner.Index.ToString());
            players.Add(new UnoPlayer(joiner));
            TSPlayer.All.SendInfoMessage("[Uno] {0} has joined the game!", joiner.Name);
            joiner.SendSuccessMessage("[Uno] You have joined the game!");
        }

        public static void LeaveGame(int leaver)
        {
            for (int i = 0; i < players.Count; i++)
            {
                if (players[i].tsplayer.Index == leaver)
                {
                    playerleave = true;
                    //debug
                    writeToLog(players[i].tsplayer.Name + " has left the game. Index: " + players[i].tsplayer.Index.ToString());
                    broadcast(players[i].tsplayer.Name + " has left the game.");
                    if (state == "active" && checkForWinner())
                        return;
                    //debug
                    writeToLog("Removing index " + players[i].tsplayer.Index.ToString());
                    players.RemoveAt(i);
                    if (state == "active" && turnindex == i)
                    {
                        //debug
                        writeToLog("Going to next turn; turnindex = i");
                        goToNextTurn();
                    }
                    else
                        playerleave = false;
                    return;
                }
                //debug
                writeToLog("Checking LeaveGame, leaver " + leaver.ToString() + " != player index " + players[i].tsplayer.Index.ToString() + " at turnindex " + i.ToString());
            }

            
        }

        public static void stopGame(TSPlayer stopper)
        {
            foreach (UnoPlayer player in players)
            {
                player.tsplayer.SendInfoMessage("[Uno] The game has been force-ended by {0}.", stopper.Name);
            }

            //debug
            writeToLog("stopGame called.");
            
            endGame("stopped");
        }
        #endregion

        private static void dealCards()
        {
            Random rand = new Random();

            int index;

            for (int i = 0; i < players.Count; i++)
            {
                for (int j = 0; j < 7; j++)
                {
                    index = rand.Next(Deck.thedeck.Count);
                    players[i].hand.Add(Deck.thedeck[index]);
                    Deck.thedeck.RemoveAt(index);
                    writeToLog("Adding card " + j.ToString() + " to player turnindex #" + i.ToString());
                }
            }

            index = rand.Next(Deck.thedeck.Count);
            Deck.faceup = Deck.thedeck[index];
            Deck.color = Deck.thedeck[index].color;
            Deck.thedeck.RemoveAt(index);
            writeToLog("Adding a faceup card.");
        }

        public static void goToNextTurn(bool skip = false, int drawcards = 0)
        {
            if (!(playerleave && turnindex > players.Count - 1))
            {
                players[turnindex].hasdrawn = false;
                writeToLog("Setting players[" + turnindex.ToString() + "].hasdrawn = false");
            }

            if (forward)
            {
                writeToLog("pre-turnindex = " + turnindex.ToString() + "; forward; playerleave = " + (playerleave ? "true" : "false"));

                if (turnindex + 1 >= players.Count)
                    turnindex = 0;
                else if (!playerleave)
                    turnindex++;
                    
                writeToLog("turnindex now = " + turnindex.ToString());
            }
            else
            {
                writeToLog("pre-turnindex = " + turnindex.ToString() + "; reversed; playerleave = " + (playerleave ? "true" : "false"));

                if (turnindex <= 0)
                    turnindex = players.Count - 1;
                else
                    turnindex--;

                writeToLog("turnindex now = " + turnindex.ToString());
            }

            playerleave = false;

            writeToLog("Playerleave = " + (playerleave ? "true" : "false"));

            if (skip)
            {
                writeToLog("Skipped turn, returning to start.");
                return;
            }

            if (drawcards != 0)
                for (int i = 0; i < drawcards; i++)
                {
                    writeToLog("Forcing drawcard.");
                    Deck.drawCard(turnindex);
                }

            broadcast("It is now " + players[turnindex].tsplayer.Name + "'s turn!");
            if (Deck.faceup.value == "wild" || Deck.faceup.value == "wdr4")
                players[turnindex].tsplayer.SendMessage("[Uno] It is now your turn! The current card is" + Deck.faceup.ToString() + ". The current color is " + Deck.color.ToString() + ".", Color.ForestGreen);
            else
                players[turnindex].tsplayer.SendMessage("[Uno] It is now your turn! The current card is " + Deck.faceup.ToString() + ".", Color.ForestGreen);
            string hand = string.Join(", ", players[turnindex].hand.Select(p => p));
            players[turnindex].tsplayer.SendMessage("[Uno] Your current cards are: " + hand, Color.ForestGreen);
            players[turnindex].tsplayer.SendMessage("[Uno] You have one minute to play a card (/play <card> [color]) or draw a card (/draw).", Color.ForestGreen);
            turnTimer.Enabled = true;
            writeToLog("TurnTimer enabled, start of turn message sent.");
        }

        private static void endOfTurn(object sender, ElapsedEventArgs args)
        {
            writeToLog("End of turn timer elapsed.");
            players[turnindex].tsplayer.SendMessage("[Uno] You ran out of time! You are now drawing a card and passing your turn.", Color.ForestGreen);
            if (!players[turnindex].hasdrawn)
            {
                Deck.drawCard(turnindex);
                writeToLog("Drawing a card.");
            }
            broadcast(players[turnindex].tsplayer.Name + " has timed out and automatically draws a card.");
            goToNextTurn();
        }

        public static bool checkForWinner()
        {
            writeToLog("Checking for winner");
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
                    writeToLog(players[i].tsplayer.Name + "'s hand is empty. They win the game.");
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
                writeToLog("Not enough players to continue game.");
            }
            if (reason == "winner")
            {
                for (int i = 0; i < players.Count; i++)
                {
                    string cards = string.Join(", ", players[i].hand.Select(p => p.ToString()));
                    foreach (Card card in players[i].hand)
                    {
                        players[i].totalpoints += card.getValuePoint();
                    }
                    if (players[i].hand.Count != 0)
                    {
                        broadcast(players[i].tsplayer.Name + "'s cards: " + cards + " (" + players[i].totalpoints.ToString() + " points)");
                        UnoMain.updatepoints(players[i].tsplayer.UserID, players[i].totalpoints);
                    }
                    else
                        UnoMain.updatewinner(players[i].tsplayer.UserID);
                }
            }
            turnTimer.Enabled = false;
            watchers.Clear();
            players.Clear();
            state = "inactive";
            TSPlayer.All.SendInfoMessage("[Uno] A game of Uno is complete.");
            writeToLog("TurnTimer cleared, watchers & players cleared, state inactive.");
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
                    writeToLog("Found played card in hand.");
                }
            }

            if (playcard.value == "wdr4")
            {
                if (color == null || (color != "r" && color != "g" && color != "y" && color != "b"))
                {
                    players[turnindex].tsplayer.SendErrorMessage("[Uno] You must play a color with this card: /play wdr4 <r/g/b/y>");
                    return;
                }
                broadcast(players[turnindex].tsplayer.Name + " plays " + playcard.ToString() + " and chooses the color " + color + "!");
                broadcast("The next player draws four cards.");
                drawcards = 4;
                Deck.color = color[0];
                writeToLog("wdr4 played.");
            }

            else if (playcard.value == "wild")
            {
                if (color == null || (color != "r" && color != "g" && color != "y" && color != "b"))
                {
                    players[turnindex].tsplayer.SendErrorMessage("[Uno] You must play a color with this card: /play wild <r/g/b/y>");
                    return;
                }
                broadcast(players[turnindex].tsplayer.Name + " plays " + playcard.ToString() + " and chooses the color " + color + "!");
                Deck.color = color[0];
                writeToLog("wild played.");
            }

            else if (playcard.value == "r")
            {
                forward = !forward;
                broadcast(players[turnindex].tsplayer.Name + " plays " + playcard.ToString() + "! The turn order is reversed!");
                Deck.color = playcard.color;
                writeToLog("reverse played.");
            }

            else if (playcard.value == "dr2")
            {
                broadcast(players[turnindex].tsplayer.Name + " plays " + playcard.ToString() + "!");
                broadcast("The next player draws two cards.");
                drawcards = 2;
                Deck.color = playcard.color;
                writeToLog("dr2 played.");
            }

            else if (playcard.value != "s")
            {
                broadcast(players[turnindex].tsplayer.Name + " plays " + playcard.ToString() + "!");
                Deck.color = playcard.color;
                writeToLog("non-skip played.");
            }

            if (playcard.value == "s")
            {
                broadcast(players[turnindex].tsplayer.Name + " plays " + playcard.ToString() + "!");
                broadcast(players[turnindex].tsplayer.Name + " skips the next player's turn!");
                skip = true;
                Deck.color = playcard.color;
                writeToLog("skip played.");
            }

            Deck.faceup = playcard;

            turnTimer.Enabled = false;

            writeToLog("turnTimer disabled.");

            players[turnindex].hand.RemoveAt(cardindex);
            writeToLog("card removed from hand");

            if (checkForWinner())
                return;
            writeToLog("Winner not found");

            if (skip)
            {
                writeToLog("go to next turn: skip");
                goToNextTurn(true);
            }
            writeToLog("go to next turn");
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

        public static bool isPlaying(int index)
        {
            for (int i = 0; i < players.Count; i++)
                if (index == players[i].tsplayer.Index)
                    return true;
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
                        Card card = Deck.parse(param[1]);
                        if (card.value == "wild")
                        {
                            player.SendInfoMessage("[Uno] 'wild' card syntax: /play wild <r/g/b/y>");
                            player.SendInfoMessage("[Uno] Changes the color of play.");
                        }
                        else if (card.value == "wdr4")
                        {
                            player.SendInfoMessage("[Uno] 'wdr4' card syntax: /play wdr4 <r/g/b/y>");
                            player.SendInfoMessage("[Uno] Changes the color of play and gives four cards to the next player.");
                        }
                        else if (card.value == "s")
                        {
                            player.SendInfoMessage("[Uno] '{0}s' card syntax: /play {0}s", card.color);
                            player.SendInfoMessage("[Uno] Skips the next player's turn.");
                        }
                        else if (card.value == "r")
                        {
                            player.SendInfoMessage("[Uno] '{0}r' card syntax: /play {0}r", card.color);
                            player.SendInfoMessage("[Uno] Reverses the order of play.");
                        }
                        else if (card.value == "dr2")
                        {
                            player.SendInfoMessage("[Uno] '{0}dr2' card syntax: /play {0}dr2", card.color);
                            player.SendInfoMessage("[Uno] Gives two cards to the next player.");
                        }
                        else
                        {
                            player.SendInfoMessage("[Uno] '{0}{1}' card syntax: /play {0}{1}", card.color, card.value);
                            player.SendInfoMessage("[Uno] No special ability.");
                        }
                    }
                }
                else
                {
                    player.SendInfoMessage("Available commands:");
                    if (player.Group.HasPermission("uno.mod"))
                        player.SendInfoMessage("/uno <start/join/stop>: Starts, joins, or stops a game of Uno.");
                    else
                        player.SendInfoMessage("/uno <start/join>: Starts or joins a game of Uno.");
                    player.SendInfoMessage("/play <card> [color]: Plays the selected card with the optional selected color.");
                    player.SendInfoMessage("/draw: Draws a card. Cannot /pass without using this command.");
                    player.SendInfoMessage("/pass: Passes a turn. Must /draw before using this command.");
                    player.SendInfoMessage("/uno help [card]: Gives information about how to use the selected card.");
                }
            }
            else
            {
                player.SendInfoMessage("Available commands:");
                if (player.Group.HasPermission("uno.mod"))
                    player.SendInfoMessage("/uno <start/join/stop>: Starts, joins, or stops a game of Uno.");
                else
                    player.SendInfoMessage("/uno <start/join>: Starts or joins a game of Uno.");
                player.SendInfoMessage("/play <card> [color]: Plays the selected card with the optional selected color.");
                player.SendInfoMessage("/draw: Draws a card. Cannot /pass without using this command.");
                player.SendInfoMessage("/pass: Passes a turn. Must /draw before using this command.");
                player.SendInfoMessage("/uno help [card]: Gives information about how to use the selected card.");
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
