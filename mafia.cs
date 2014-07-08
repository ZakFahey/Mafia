using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using TShockAPI;
using Terraria;
using TerrariaApi.Server;
using System.Timers;

namespace mafia {
    [ApiVersion(1, 16)]
    public class mafia : TerrariaPlugin {
        bool MafiaStarted { get; set; }
        bool Mafia3P { get; set; }
        int TimerMode { get; set; }
        string whoMafia { get; set; }//perhaps make multiple mafias
        string whoSheriff { get; set; }
        string whoDoctor { get; set; }
        string killMafia { get; set; }
        string accuseSheriff { get; set; }
        string saveDoctor { get; set; }
        bool saveHimselfDoctor { get; set; }
        bool teleportingSheriff { get; set; }
        
        Random R = new Random();
        Timer timer = new Timer();
        Timer check = new Timer();
        Timer forcePVP = new Timer();

        public mafia(Main game) : base(game) {
        }
        public override void Initialize() {
            Commands.ChatCommands.Add(new Command("mafia.admin.start", mafiaStart, "mafiastart") {
                HelpText = "Start the game 'Mafia.'"
            });
            Commands.ChatCommands.Add(new Command("mafia.admin.start", mafiaStop, "mafiastop")
            {
                HelpText = "Ends the game 'Mafia.'"
            });
            Commands.ChatCommands.Add(new Command("mafia.guest.play", anonChat, "anon", "an")
            {
                HelpText = "Chat as whoever you are anonymously."
            });
            Commands.ChatCommands.Add(new Command("mafia.guest.play", mafiaWho, "mafiawho", "whomafia")
            {
                HelpText = "Says who is who."
            });
            Commands.ChatCommands.Add(new Command("mafia.guest.play", mafKill, "mkill")
            {
                HelpText = "Kill a player."
            });
            Commands.ChatCommands.Add(new Command("mafia.guest.play", mafAccuse, "maccuse")
            {
                HelpText = "Accuse a player."
            });
            Commands.ChatCommands.Add(new Command("mafia.guest.play", mafTP, "mtp")
            {
                HelpText = "Teleport to a player."
            });
            Commands.ChatCommands.Add(new Command("mafia.guest.play", mafSave, "msave")
            {
                HelpText = "Saves a player."
            });
            Commands.ChatCommands.Add(new Command("mafia.guest.play", mafCancel, "mcancel")
            {
                HelpText = "Cancels a selection."
            });
            Commands.ChatCommands.Add(new Command("mafia.guest.play", whosAlive, "whosalive")
            {
                HelpText = "Cancels a selection."
            });
            Commands.ChatCommands.Add(new Command(Permissions.canchat, deadChat, "deadchat", "d")
            {
                HelpText = "Cancels a selection."
            });

            MafiaStarted = false;

            check.Elapsed += new ElapsedEventHandler(checkTimer);
            timer.Elapsed += new ElapsedEventHandler(OnTimedEvent);
            forcePVP.Elapsed += new ElapsedEventHandler(PVPTimer);

            ServerApi.Hooks.NetGreetPlayer.Register(this, OnGreetPlayer);
        }
        public override Version Version {
            get { return new Version("1.0"); }
        }
        public override string Name {
            get { return "Mafia"; }
        }
        public override string Author {
            get { return "GameRoom"; }
        }
        public override string Description {
            get { return "Automates the game 'Mafia.'"; }
        }
        protected override void Dispose(bool disposing) {
            base.Dispose(disposing);
        }

        public void mafiaStart(CommandArgs e) {
            if (MafiaStarted) {
                bc("The game has already started.");
            } else if (goodPlayers() < 7) {
                e.Player.SendErrorMessage(string.Format("There aren't enough players to start yet. (7 minimum; {0} current)", goodPlayers()));
            } else {
                bc("The game is starting.");
                TShock.Config.HardcoreOnly = false;
                MafiaStarted = true;
                Mafia3P = false;
                TimerMode = 0;
                timer.Interval = 1000;
                timer.AutoReset = true;
                timer.Start();
                check.Interval = 500;
                check.AutoReset = true;
                check.Start();
                forcePVP.Interval = 12000;
                forcePVP.AutoReset = true;
                forcePVP.Start();
            }
        }

        private void OnTimedEvent(object source, ElapsedEventArgs e) {
            if (TimerMode != 5 && TimerMode != 0) checkGame();
            if (TimerMode == 0) {
                TimerMode = 1;
                timer.Interval = 2000;
                var length = goodPlayers();
                var mafiaList = new string[length];
                var i = 0;
                foreach (TSPlayer player in TShock.Players) {
                    if (player != null && player.Active && !player.Dead && player.Difficulty == 2) {
                        mafiaList[i] = player.Name;
                        i++;
                    }
                }
                whoMafia = mafiaList[R.Next(0, length - 1)];
                whoSheriff = mafiaList[R.Next(0, length - 1)];
                while (whoMafia == whoSheriff)
                    whoSheriff = mafiaList[R.Next(0, length - 1)];
                whoDoctor = mafiaList[R.Next(0, length - 1)];
                while (whoDoctor == whoMafia || whoDoctor == whoSheriff)
                    whoDoctor = mafiaList[R.Next(0, length - 1)];
                killMafia = "";
                accuseSheriff = "";
                saveDoctor = "";
                saveHimselfDoctor = false;
                teleportingSheriff = false;
                TShock.Utils.FindPlayer(whoMafia)[0].SendInfoMessage("You are the mafia.");
                TShock.Utils.FindPlayer(whoSheriff)[0].SendInfoMessage("You are the sheriff.");
                TShock.Utils.FindPlayer(whoDoctor)[0].SendInfoMessage("You are the doctor.");
                foreach (TSPlayer player in TShock.Players) {
                    if (player != null && player.Active && !player.Dead && player.Difficulty == 2 && player.Name != whoMafia && player.Name != whoSheriff && player.Name != whoDoctor)
                        player.SendInfoMessage("You are a civilian.");
                }
            } else if (TimerMode == 1) {
                string[] txt = {
                                   "Go, but watch out, for the Mafia is about.",
                                   "You may leave now, but is it really safer when the Mafia is on the loose?",
                                   "What God lets these Mafia roam!? Anyway, you can go now."
                               };
                bc(String.Format("{0} And turn PvP on!.", txt[R.Next(0, 2)]));
                TSPlayer.Server.SetTime(true, 20000.0);
                TShock.Config.DisableBuild = false;
                TimerMode = 2;
                timer.Interval = 150000;
            } else if (TimerMode == 2) {
                TimerMode = 3;
                timer.Interval = 15000;
                bc("Judgement time!");
                if (!TShock.Utils.FindPlayer(whoMafia)[0].Dead && TShock.Utils.FindPlayer(whoMafia)[0].Difficulty == 2)
                    TShock.Utils.FindPlayer(whoMafia)[0].SendInfoMessage("Type in a player to kill with /mkill <player>. Type /whosalive to see who's alive.");
                if (!TShock.Utils.FindPlayer(whoSheriff)[0].Dead && TShock.Utils.FindPlayer(whoSheriff)[0].Difficulty == 2)
                    TShock.Utils.FindPlayer(whoSheriff)[0].SendInfoMessage("Type in a player to accuse with /maccuse <player>, or teleport one player to the spawn with /mtp <player>. Type /whosalive to see who's alive.");
                if (!TShock.Utils.FindPlayer(whoDoctor)[0].Dead && TShock.Utils.FindPlayer(whoDoctor)[0].Difficulty == 2)
                    TShock.Utils.FindPlayer(whoDoctor)[0].SendInfoMessage("Type in a player to save with /msave <player>. Type /whosalive to see who's alive.");
            } else if (TimerMode == 3) {
                TimerMode = 4;
                if (killMafia == "" && !TShock.Utils.FindPlayer(whoMafia)[0].Dead && TShock.Utils.FindPlayer(whoMafia)[0].Difficulty == 2)
                    TShock.Utils.FindPlayer(whoMafia)[0].SendInfoMessage("Type in a player to kill with /mkill <player>. Type /whosalive to see who's alive. If you don't choose somebody in 15 second, yours turn will be skipped.");
                if (accuseSheriff == "" && !TShock.Utils.FindPlayer(whoSheriff)[0].Dead && TShock.Utils.FindPlayer(whoSheriff)[0].Difficulty == 2)
                    TShock.Utils.FindPlayer(whoSheriff)[0].SendInfoMessage("Type in a player to accuse with /maccuse <player>, or teleport one player to the spawn with /mtp <player>. Type /whosalive to see who's alive. If you don't choose somebody in 15 seconds, your turn will be skipped.");
                if (saveDoctor == "" && !TShock.Utils.FindPlayer(whoDoctor)[0].Dead && TShock.Utils.FindPlayer(whoDoctor)[0].Difficulty == 2)
                    TShock.Utils.FindPlayer(whoDoctor)[0].SendInfoMessage("Type in a player to save with /msave <player>. Type /whosalive to see who's alive. If you don't choose somebody in 15 seconds, your turn will be skipped.");
            } else if (TimerMode == 4) {
                TimerMode = 2;
                timer.Interval = 150000;
                if (accuseSheriff != "" && !TShock.Utils.FindPlayer(whoSheriff)[0].Dead && TShock.Utils.FindPlayer(whoSheriff)[0].Difficulty == 2 && TShock.Utils.FindPlayer(accuseSheriff)[0].Difficulty == 2)
                    if (teleportingSheriff) {
                        if (TShock.Utils.FindPlayer(accuseSheriff)[0].Teleport(Main.spawnTileX * 16, (Main.spawnTileY * 16) - 48)) {
                            TShock.Utils.FindPlayer(whoSheriff)[0].SendInfoMessage(String.Format("{0} was teleported to the spawn point.", accuseSheriff));
                            if (whoSheriff == accuseSheriff)
                                TShock.Utils.FindPlayer(accuseSheriff)[0].SendInfoMessage("You teleported yourself to the spawn point.");
                                else TShock.Utils.FindPlayer(accuseSheriff)[0].SendInfoMessage("The sheriff teleported you to the spawn point.");
                        }
                    }
                    else {
                        if (accuseSheriff == whoMafia) TShock.Utils.FindPlayer(whoSheriff)[0].SendInfoMessage(String.Format("Yes, {0} is the mafia.", accuseSheriff));
                        else TShock.Utils.FindPlayer(whoSheriff)[0].SendInfoMessage(String.Format("No, {0} isn't the mafia.", accuseSheriff));
                    }
                if (killMafia != "" && !TShock.Utils.FindPlayer(whoMafia)[0].Dead && TShock.Utils.FindPlayer(whoMafia)[0].Difficulty == 2 && TShock.Utils.FindPlayer(killMafia)[0].Difficulty == 2)
                    if (killMafia == saveDoctor)
                        bc(String.Format("{0} was attacked by the mafia but was saved by the doctor.", killMafia));
                    else {
                        TShock.Utils.FindPlayer(killMafia)[0].DamagePlayer(999999);
                        if (killMafia != whoMafia)
                            bc(String.Format("{0} was killed by the mafia.", killMafia));
                    }
                if (saveDoctor != "" && !TShock.Utils.FindPlayer(whoDoctor)[0].Dead && TShock.Utils.FindPlayer(whoDoctor)[0].Difficulty == 2 && TShock.Utils.FindPlayer(saveDoctor)[0].Difficulty == 2)
                {
                    TShock.Utils.FindPlayer(saveDoctor)[0].Heal();
                    if (saveDoctor == whoDoctor) TShock.Utils.FindPlayer(saveDoctor)[0].SendInfoMessage("You just healed yourself.");
                    else TShock.Utils.FindPlayer(saveDoctor)[0].SendInfoMessage("The doctor just healed you.");
                }
                killMafia = "";
                accuseSheriff = "";
                saveDoctor = "";
            } else if (TimerMode == 5)
                TShock.Utils.StopServer(false, "Server shutting down!");
        }

        public void mafKill(CommandArgs e) {
            if (!MafiaStarted)
                e.Player.SendErrorMessage("No game of Mafia is being played.");
            else if (e.Player.Dead)
                e.Player.SendErrorMessage("You are dead!");
            else if (e.Player.Difficulty != 2)
                e.Player.SendErrorMessage("You are spectating.");
            else if (!e.Player.TPlayer.hostile)
                e.Player.SendErrorMessage("Turn on PvP!");
            else if (e.Player.Team != 0)
                e.Player.SendErrorMessage("Turn off teams!");
            else if (TimerMode != 3 && TimerMode != 4)
                e.Player.SendErrorMessage("It isn't judgement time.");
            else if (whoMafia == e.Player.Name) {
                if (e.Parameters.Count == 0)
                    e.Player.SendInfoMessage("Enter a player name.");
                else {
                    var players = TShock.Utils.FindPlayer(e.Parameters[0]);
                    if (players.Count == 0) e.Player.SendErrorMessage("Invalid player!");
                    else if (players[0].Dead) e.Player.SendErrorMessage(String.Format("{0} is dead!", players[0].Name));
                    else if (players[0].Difficulty != 2) e.Player.SendErrorMessage(String.Format("{0} is spectating.", players[0].Name));
                    else {
                        killMafia = players[0].Name;
                        e.Player.SendInfoMessage(String.Format("You just selected {0} to kill. Type /mcancel to cancel your request, or type /mkill again to choose a different person.", killMafia));
                    }
                }
            } else e.Player.SendErrorMessage("You aren't the mafia!");
        }

        public void mafAccuse(CommandArgs e) {
            if (!MafiaStarted)
                e.Player.SendErrorMessage("No game of Mafia is being played.");
            else if (e.Player.Dead)
                e.Player.SendErrorMessage("You are dead!");
            else if (e.Player.Difficulty != 2)
                e.Player.SendErrorMessage("You are spectating.");
            else if (!e.Player.TPlayer.hostile)
                e.Player.SendErrorMessage("Turn on PvP!");
            else if (e.Player.Team != 0)
                e.Player.SendErrorMessage("Turn off teams!");
            else if (TimerMode != 3 && TimerMode != 4)
                e.Player.SendErrorMessage("It isn't judgement time.");
            else if (whoSheriff == e.Player.Name)
            {
                if (e.Parameters.Count == 0)
                    e.Player.SendInfoMessage("Enter a player name.");
                else
                {
                    var players = TShock.Utils.FindPlayer(e.Parameters[0]);
                    if (players.Count == 0) e.Player.SendErrorMessage("Invalid player!");
                    else if (players[0].Dead) e.Player.SendErrorMessage(String.Format("{0} is dead!", players[0].Name));
                    else if (players[0].Difficulty != 2) e.Player.SendErrorMessage(String.Format("{0} is spectating.", players[0].Name));
                    else {
                        accuseSheriff = players[0].Name;
                        teleportingSheriff = false;
                        e.Player.SendInfoMessage(String.Format("You just selected {0} to accuse. Type /mcancel to cancel your request, or type /maccuse again to choose a different person.", accuseSheriff));
                    }
                }
            }
            else e.Player.SendErrorMessage("You aren't the sheriff!");
        }

        public void mafTP(CommandArgs e)
        {
            if (!MafiaStarted)
                e.Player.SendErrorMessage("No game of Mafia is being played.");
            else if (e.Player.Dead)
                e.Player.SendErrorMessage("You are dead!");
            else if (e.Player.Difficulty != 2)
                e.Player.SendErrorMessage("You are spectating.");
            else if (!e.Player.TPlayer.hostile)
                e.Player.SendErrorMessage("Turn on PvP!");
            else if (e.Player.Team != 0)
                e.Player.SendErrorMessage("Turn off teams!");
            else if (TimerMode != 3 && TimerMode != 4)
                e.Player.SendErrorMessage("It isn't judgement time.");
            else if (whoSheriff == e.Player.Name)
            {
                if (e.Parameters.Count == 0)
                    e.Player.SendInfoMessage("Enter a player name.");
                else
                {
                    var players = TShock.Utils.FindPlayer(e.Parameters[0]);
                    if (players.Count == 0) e.Player.SendErrorMessage("Invalid player!");
                    else if (players[0].Dead) e.Player.SendErrorMessage(String.Format("{0} is dead!", players[0].Name));
                    else if (players[0].Difficulty != 2) e.Player.SendErrorMessage(String.Format("{0} is spectating.", players[0].Name));
                    else
                    {
                        accuseSheriff = players[0].Name;
                        teleportingSheriff = true;
                        e.Player.SendInfoMessage(String.Format("You just selected {0} to teleport. Type /mcancel to cancel your request, or type /maccuse again to choose a different person.", accuseSheriff));
                    }
                }
            }
            else e.Player.SendErrorMessage("You aren't the sheriff!");
        }

        public void mafSave(CommandArgs e) {
            if (!MafiaStarted)
                e.Player.SendErrorMessage("No game of Mafia is being played.");
            else if (e.Player.Dead)
                e.Player.SendErrorMessage("You are dead!");
            else if (e.Player.Difficulty != 2)
                e.Player.SendErrorMessage("You are spectating.");
            else if (!e.Player.TPlayer.hostile)
                e.Player.SendErrorMessage("Turn on PvP!");
            else if (e.Player.Team != 0)
                e.Player.SendErrorMessage("Turn off teams!");
            else if (TimerMode != 3 && TimerMode != 4)
                e.Player.SendErrorMessage("It isn't judgement time.");
            else if (whoDoctor == e.Player.Name)
            {
                if (e.Parameters.Count == 0)
                    e.Player.SendInfoMessage("Enter a player name.");
                else {
                    var players = TShock.Utils.FindPlayer(e.Parameters[0]);
                    if (players.Count == 0) e.Player.SendErrorMessage("Invalid player!");
                    else if (players[0].Dead) e.Player.SendErrorMessage(String.Format("{0} is dead!", players[0].Name));
                    else if (players[0].Difficulty != 2) e.Player.SendErrorMessage(String.Format("{0} is spectating.", players[0].Name));
                    else if (e.Player.Name == players[0].Name && saveHimselfDoctor)
                        e.Player.SendErrorMessage("You can't save yourself twice in a row.");
                    else {
                        saveDoctor = players[0].Name;
                        saveHimselfDoctor = (e.Player.Name == players[0].Name);
                        e.Player.SendInfoMessage(String.Format("You just selected {0} to save. Type /mcancel to cancel your request, or type /msave again to choose a different person.", saveDoctor));
                    }
                }
            }
            else e.Player.SendErrorMessage("You aren't the doctor!");
        }

        private void bc(string text) {
            TSPlayer.All.SendInfoMessage(text);
            Console.WriteLine(text);
        }

        private void mafCancel(CommandArgs e) {
            if (e.Player.Difficulty != 2) {
                if (whoMafia == e.Player.Name && killMafia != "") {
                    killMafia = "";
                    e.Player.SendInfoMessage("Kill request cleared.");
                }
                if (whoSheriff == e.Player.Name && accuseSheriff != "") {
                    accuseSheriff = "";
                    e.Player.SendInfoMessage("Accuse request cleared.");
                }
                if (whoDoctor == e.Player.Name && saveDoctor != "") {
                    if (saveDoctor == whoDoctor) saveHimselfDoctor = false;
                    saveDoctor = "";
                    e.Player.SendInfoMessage("Save request cleared.");
                }
            }
        }

        private void checkTimer(object source, ElapsedEventArgs e) {
            checkGame();
            if (goodPlayers() == 3 && !Mafia3P) {
                Mafia3P = true;
                bc("3 players are left. If one more civilian dies, the Mafia wins.");
            }
            foreach (TSPlayer player in TShock.Players) {
                if (player != null && player.Active && player.Dead && player.Difficulty == 2) {
                    if (!player.mute) {
                        player.SendInfoMessage("Players are muted on death. Use /deathchat or /d to talk with other dead players. Type /whomafia to see who is who. You may join as a NEW softcore character to spectate.");
                        player.mute = true;
                    }
                }
            }
        }
        private bool checkGame() {
            if (TShock.Utils.FindPlayer(whoMafia)[0] == null || TShock.Utils.FindPlayer(whoMafia)[0].Dead) {
                endGame(false);
                return true;
            } else {
                if (goodPlayers() <= 2) {
                    endGame(true);
                    return true;
                }
                else return false;
            }
        }

        private void endGame(bool mafWon) {
            if (TimerMode != 5) {
                timer.Interval = 13000;
                TimerMode = 5;
                check.AutoReset = false;
                timer.AutoReset = false;
                check.Stop();
                timer.Stop();
                timer.Start();
                var ITM = TShock.Utils.GetItemByIdOrName("confetti gun")[0];
                if (mafWon) {
                    bc(String.Format("The mafia, {0}, killed everyone. Game over; mafia wins!", whoMafia));
                    if (TShock.Utils.FindPlayer(whoMafia)[0].InventorySlotAvailable)
                        TShock.Utils.FindPlayer(whoMafia)[0].GiveItem(ITM.type, ITM.name, ITM.width, ITM.height, 999);
                }
                else {
                    bc(String.Format("The mafia, {0}, was killed. Game over; civilians win!", whoMafia));
                    foreach(TSPlayer player in TShock.Players)
                        if (player != null && player.Active && player.Dead && player.Difficulty == 2 && player.InventorySlotAvailable)
                            player.GiveItem(ITM.type, ITM.name, ITM.width, ITM.height, 999);
                }
            }
        }

        public void mafiaStop(CommandArgs e) {
            if (MafiaStarted) {
                check.Stop();
                timer.Stop();
                MafiaStarted = false;
                bc("Game canceled.");
                TShock.Config.HardcoreOnly = true;
            }
        }
        public void whosAlive(CommandArgs args) {
            var sb = new StringBuilder();
            var count = 0;
            foreach (TSPlayer player in TShock.Players) {
                if (player != null && player.Active && !player.Dead && player.Difficulty == 2) {
                    if (sb.Length != 0)
                        sb.Append(", ");
                    sb.Append(player.Name);
                    count++;
                }
            }
            args.Player.SendInfoMessage(String.Format("Current players ({0}): {1}", count, sb.ToString()));
        }

        public void deadChat(CommandArgs e) {
            if (!e.Player.Dead && e.Player.Difficulty == 2) e.Player.SendErrorMessage("You're not dead.");
            else {
                var message = String.Format("(Dead) {0}: {1}", e.Player.Name, String.Join(" ", e.Parameters));
                foreach (TSPlayer player in TShock.Players) {
                    if (player != null && player.Active && (player.Dead || player.Difficulty != 2))
                        player.SendMessage(message, Color.Purple);
                }
            }
        }

        public void PVPTimer(object source, ElapsedEventArgs e) {
            forcePVP.Interval = 2000;
            foreach (TSPlayer player in TShock.Players) {
                if (player != null && player.Active && !player.Dead) {
                    if (player.Difficulty == 2) {
                        var penalized = false;
                        if (!player.TPlayer.hostile) {
                            penalized = true;
                            player.SendWarningMessage("Turn PvP on!");
                        }
                        if (player.Team != 0) {
                            penalized = true;
                            player.SendWarningMessage("Get off teams!");
                        }
                        if (penalized) {
                            player.SetBuff(23, 180);//Cursed
                            player.SetBuff(32, 180);//Slow
                        }
                    } else {
                        player.SetBuff(23, 180);//Cursed
                        player.SetBuff(11, 180);//Shine
                        player.SetBuff(12, 180);//Night owl
                        if (player.TPlayer.hostile) {
                            player.SetBuff(32, 180);//Slow
                            player.SendWarningMessage("Turn PvP off!");
                        }
                    }
                }
            }
        }

        public void mafiaWho(CommandArgs e) {
            if (e.Player.Group.ToString() != "superadmin" && !e.Player.Dead && e.Player.Difficulty == 2)
                e.Player.SendErrorMessage("You don't have permission to use that command.");
            else if (MafiaStarted)
                e.Player.SendInfoMessage(String.Format("Mafia: {0}  Doctor: {1}  Sheriff: {2}", whoMafia, whoDoctor, whoSheriff));
            else e.Player.SendErrorMessage("No game of Mafia has started.");
        }

        public void OnGreetPlayer(GreetPlayerEventArgs e) {//CHANGE- Make config so that this doesn't happen when Mafia isn't being played
            var plr = TShock.Players[e.Who];
            if (plr.Difficulty == 2)
                plr.SendMessage("Play the classic game, only in Terraria!", Color.White);
            else {
                plr.SendMessage("You are spectating. You may not talk, but you can use /deadchat or /d to talk to other spectators and dead players. Type /whomafia to see who is who.", Color.White);
                plr.mute = true;
                plr.GodMode = true;
                plr.SetBuff(32, 480);//Slow
                string[] items = { "steampunk wings", "lightning boots", "obsidian shield" };
                foreach(string itm in items) {
                    var ITM = TShock.Utils.GetItemByIdOrName(itm)[0];
                    if (plr.InventorySlotAvailable)
                        plr.GiveItem(ITM.type, ITM.name, ITM.width, ITM.height, 1, 76);
                }
            }
        }

        public int goodPlayers() {
            var plrs = 0;
            foreach (TSPlayer player in TShock.Players)
                if (player != null && player.Active && player.Difficulty == 2 && !player.Dead) plrs++;
            return plrs;
        }

        public void anonChat(CommandArgs e) {
            if (!e.Player.mute && MafiaStarted) {
                string pref;
                if (e.Player.Name == whoMafia) pref = "(Mafia)";
                else if (e.Player.Name == whoDoctor) pref = "(Doctor)";
                else if (e.Player.Name == whoSheriff) pref = "(Sheriff)";
                else pref = "(Civilian):";
                bc(String.Format("{0}: {1}", pref, e.Parameters[0]));
            }
        }
    }
}