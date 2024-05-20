using EssentialsPlus.Extensions;
using Microsoft.Xna.Framework;
using MySqlX.XDevAPI.Common;
using System.Drawing;
using System.Timers;
using Terraria;
using Terraria.DataStructures;
using Terraria.ID;
using TerrariaApi.Server;
using TShockAPI;
using TShockAPI.DB;
using TShockAPI.Hooks;

namespace CommunityPlugin
{
    [ApiVersion(2, 1)]

    public class CommunityPlugin : TerrariaPlugin
    {
        public override string Author => "Ozz5581";
        public override string Description => "Community Plugin";
        public override string Name => "CommunityPlugin";
        public override Version Version => new Version(1, 0, 0);

        public CommunityPlugin(Main game) : base(game)
        {
        }



        public override void Initialize()
        {
            // Ensure the table exists in the database
            SetupDatabase();

            TShockAPI.Hooks.PlayerHooks.PlayerPostLogin += OnPostLogin;
            //ServerApi.Hooks.GameUpdate.Register(this, OnUpdate);

            Commands.ChatCommands.Add(new Command("community.use", CommCommand, "com", "comm"));
            PlayerHooks.PlayerCommand += OnPlayerCommand;
            // Add other commands and hooks as needed

            SecondTimer.Start();
            SecondTimer.Elapsed += ASecondElapsed;
        }

        private readonly System.Timers.Timer SecondTimer = new(1000);

        private Dictionary<string, string> playerTeams = new Dictionary<string, string>();
        private void OnPostLogin(PlayerPostLoginEventArgs args)
        {
            var db = TShock.DB;
            var playerTeam = db.QueryReader("SELECT Team FROM main_db.experiment1 WHERE Name = @0", args.Player.Account.Name);

            string team = "none";

            if (playerTeam.Read())
            {
                team = playerTeam.Get<string>("Team");
            }

            SetPlayerTeam(args.Player.Name, team);
        }

        public void SetPlayerTeam(string playerName, string team)
        {
            if (playerTeams.ContainsKey(playerName))
            {
                // Update the player's team if already in the dictionary
                playerTeams[playerName] = team;
            }
            else
            {
                // Add the player to the dictionary with their team
                playerTeams.Add(playerName, team);
            }
        }

        public string GetPlayerTeam(string playerName)
        {
            // Try to get the player's team from the dictionary
            if (playerTeams.TryGetValue(playerName, out string team))
            {
                return team;
            }

            // Return null if the player is not in the dictionary
            return null;
        }

        private void OnPlayerCommand(PlayerCommandEventArgs e)
        {
            if (e.Handled || e.Player == null)
                return;

            Command command = e.CommandList.FirstOrDefault();

            if (command == null)
                return;

            if (command.Name == "logout" ||
                command.Name == "home" ||
                command.Name == "tpnpc" ||
                command.Name == "tp" ||
                command.Name == "enableboss" ||
                command.Name == "enblb" ||
                command.Name == "rocket" ||
                command.Name == "annoy" ||
                command.Name == "tphere" ||
                command.Name == "slap" ||
                command.Name == "kill" ||
                command.Name == "go" ||
                command.Name == "tppos")
            {
                e.Player.SendErrorMessage("This command is disabled on this server.");
                e.Handled = true;
                return;
            }
        }
        public async void ASecondElapsed(object o, ElapsedEventArgs args)
        {

            foreach (TSPlayer player in TShock.Players.Where(p => p != null && p.Active && p.IsLoggedIn))
            {

                // Ensure that the player has a valid account
                if (player.Account != null)
                {

                    string team = GetPlayerTeam(player.Account.Name);

                    if (player.Team != 1 && team == "red")
                    {
                        player.SetTeam(1);
                        player.DamagePlayer(player.TPlayer.statLife / 4);
                    }

                    if (player.Team != 3 && team == "blue")
                    {
                        player.SetTeam(3);
                        player.DamagePlayer(player.TPlayer.statLife / 4);
                    }

                    if (!player.TPlayer.hostile)
                        player.SetPvP(true);

                    if (player.CurrentRegion != null && !player.Dead)
                    {
                        if (player.CurrentRegion.Name == "teamRed" && team != "red")
                        {
                            player.SetBuff(BuffID.Frostburn2, 80);
                            player.SetBuff(BuffID.Dazed, 80);
                            player.SetBuff(BuffID.Venom, 80);
                            player.SendData(PacketTypes.CreateCombatTextExtended, "Wrong Side!", (int)Microsoft.Xna.Framework.Color.Red.PackedValue, player.X, player.Y);
                        }

                        if (player.CurrentRegion.Name == "teamBlue" && team != "blue")
                        {
                            player.SetBuff(BuffID.Frostburn2, 80);
                            player.SetBuff(BuffID.Dazed, 80);
                            player.SetBuff(BuffID.Venom, 80);
                            player.SendData(PacketTypes.CreateCombatTextExtended, "Wrong Side!", (int)Microsoft.Xna.Framework.Color.Red.PackedValue, player.X, player.Y);
                        }
                    }

                    if (player.IsInRange(Main.spawnTileX, Main.spawnTileY, 4))
                    {
                        if (team == "none" || team == string.Empty)
                            return;

                        if (team == "red")
                        {
                            int x, y;

                            TShockAPI.DB.Region region = TShock.Regions.GetRegionByName("teamRed");

                            {
                                int currentLevel = 0;
                                bool empty = false;
                                int tilex = Math.Max(0, Math.Min(region.Area.Center.X, Main.maxTilesX - 2));
                                int tiley = Math.Max(0, Math.Min(region.Area.Top + 3, Main.maxTilesY - 3));

                                await Task.Run(() =>
                                {
                                    for (int j = tiley; currentLevel < 1 && j < Main.maxTilesY - 2; j++)
                                    {
                                        if (Main.tile[tilex, j].IsEmpty() && Main.tile[tilex + 1, j].IsEmpty() &&
                                            Main.tile[tilex, j + 1].IsEmpty() && Main.tile[tilex + 1, j + 1].IsEmpty() &&
                                            Main.tile[tilex, j + 2].IsEmpty() && Main.tile[tilex + 1, j + 2].IsEmpty())
                                        {
                                            empty = true;
                                        }
                                        else if (empty)
                                        {
                                            empty = false;
                                            currentLevel++;
                                            tiley = j;
                                        }
                                    }
                                });

                                player.Teleport(16 * tilex, 16 * tiley);
                                Projectile.NewProjectile(Projectile.GetNoneSource(), player.TPlayer.position.X, player.TPlayer.position.Y - 3, 0f, -0f, ProjectileID.StardustGuardianExplosion, 2000, 0, -1);

                                player.SendData(PacketTypes.CreateCombatTextExtended, "Warped!", (int)Microsoft.Xna.Framework.Color.Pink.PackedValue, player.X, player.Y);
                                player.SetBuff(BuffID.NebulaUpLife2, 1800);
                                player.SetBuff(BuffID.NebulaUpDmg2, 1800);
                                player.SetBuff(BuffID.Shine, 3600);
                            }

                        }

                        if (team == "blue")
                        {
                            int x, y;

                            TShockAPI.DB.Region region = TShock.Regions.GetRegionByName("teamBlue");

                            {
                                int currentLevel = 0;
                                bool empty = false;
                                int tilex = Math.Max(0, Math.Min(region.Area.Center.X, Main.maxTilesX - 2));
                                int tiley = Math.Max(0, Math.Min(region.Area.Top + 3, Main.maxTilesY - 3));

                                await Task.Run(() =>
                                {
                                    for (int j = tiley; currentLevel < 1 && j < Main.maxTilesY - 2; j++)
                                    {
                                        if (Main.tile[tilex, j].IsEmpty() && Main.tile[tilex + 1, j].IsEmpty() &&
                                            Main.tile[tilex, j + 1].IsEmpty() && Main.tile[tilex + 1, j + 1].IsEmpty() &&
                                            Main.tile[tilex, j + 2].IsEmpty() && Main.tile[tilex + 1, j + 2].IsEmpty())
                                        {
                                            empty = true;
                                        }
                                        else if (empty)
                                        {
                                            empty = false;
                                            currentLevel++;
                                            tiley = j;
                                        }
                                    }
                                });

                                player.Teleport(16 * tilex, 16 * tiley);
                                Projectile.NewProjectile(Projectile.GetNoneSource(), player.TPlayer.position.X, player.TPlayer.position.Y - 3, 0f, -0f, ProjectileID.StardustGuardianExplosion, 2000, 0, -1);

                                player.SendData(PacketTypes.CreateCombatTextExtended, "Warped!", (int)Microsoft.Xna.Framework.Color.Pink.PackedValue, player.X, player.Y);
                                player.SetBuff(BuffID.NebulaUpLife2, 1800);
                                player.SetBuff(BuffID.NebulaUpDmg2, 1800);
                                player.SetBuff(BuffID.Shine, 3600);
                            }

                        }

                    }
                }
            }
        }
        private void SetupDatabase()
        {
            var db = TShock.DB;
            var result = db.QueryReader("SHOW TABLES LIKE 'experiment1'");
            var tableExists = result.Read();

            if (!tableExists)
            {
                // Table does not exist, create it
                db.Query("CREATE TABLE IF NOT EXISTS `main_db`.`experiment1` (" +
                         "`ID` INT NOT NULL AUTO_INCREMENT, " +
                         "`Name` VARCHAR(255) NOT NULL, " +
                         "`Team` VARCHAR(50) NOT NULL, " +
                         "`Role` VARCHAR(255) NOT NULL, " +
                         "`Votes` INT NOT NULL DEFAULT 0, " +
                         "`HasVoted` BOOLEAN NOT NULL DEFAULT FALSE, " +
                         "PRIMARY KEY (`ID`), " +
                         "UNIQUE INDEX `Name_UNIQUE` (`Name` ASC));");
            }
        }

        private void CommCommand(CommandArgs args)
        {
            if (args.Parameters.Count < 1)
            {
                args.Player.SendErrorMessage("Invalid syntax! Proper usage: /comm <join/banish/elect/teamlist>");
                return;
            }

            string subcommand = args.Parameters[0].ToLower();
            switch (subcommand)
            {
                case "join":
                    JoinTeamCommand(args);
                    break;
                case "banish":
                    BanishPlayerCommand(args);
                    break;
                case "elect":
                    ElectLeaderCommand(args);
                    break;
                case "teamlist":
                    TeamListCommand(args);
                    break;
                default:
                    args.Player.SendErrorMessage("Unknown subcommand. Available subcommands: join, banish, elect, teamlist");
                    break;
            }
        }

        private void JoinTeamCommand(CommandArgs args)
        {
            if (!args.Player.IsLoggedIn)
            {
                args.Player.SendErrorMessage("You must be logged in to use this command.");
                return;
            }

            if (args.Parameters.Count < 2)
            {
                args.Player.SendErrorMessage("Invalid syntax. Use '/comm join <red/blue>'.");
                return;
            }

            string subCommand = args.Parameters[0].ToLower();
            string team = args.Parameters[1].ToLower();

            if (subCommand != "join")
            {
                args.Player.SendErrorMessage("Invalid subcommand. Use '/comm join <red/blue>'.");
                return;
            }

            if (team != "red" && team != "blue")
            {
                args.Player.SendErrorMessage("Invalid team. Use '/comm join red' or '/comm join blue'.");
                return;
            }

            var db = TShock.DB;
            var result = db.QueryReader("SELECT Team FROM main_db.experiment1 WHERE Name = @0", args.Player.Account.Name);
            if (result.Read())
            {
                args.Player.SendErrorMessage("You are already in a team.");
                return;
            }

            db.Query("INSERT INTO main_db.experiment1 (Name, Team, Role, Votes, HasVoted) VALUES (@0, @1, 'member', 0, false)",
                args.Player.Account.Name, team);

            args.Player.SendSuccessMessage($"You have joined the {team.ToUpper()} team as a member.");

            SetPlayerTeam(args.Player.Name, team);
        }

        private void ElectLeaderCommand(CommandArgs args)
        {
            if (args.Parameters.Count == 0)
            {
                args.Player.SendErrorMessage("Please specify a player to vote for.");
                return;
            }
            if (!args.Player.IsLoggedIn)
            {
                args.Player.SendErrorMessage("You must be logged in to use this command.");
                return;
            }

            var db = TShock.DB;
            var result = db.QueryReader("SELECT Team, HasVoted FROM main_db.experiment1 WHERE Name = @0", args.Player.Name);
            if (!result.Read())
            {
                args.Player.SendErrorMessage("You are not in a team. Use '/comm join red' or '/comm join blue' first.");
                return;
            }

            string team = result.Get<string>("Team");
            bool hasVoted = result.Get<bool>("HasVoted");
            if (hasVoted)
            {
                args.Player.SendErrorMessage("You have already voted. You can only vote once.");
                return;
            }

            string votedPlayerName = args.Parameters.Count > 0 ? args.Parameters[1] : "";
            if (votedPlayerName == args.Player.Name)
            {
                args.Player.SendErrorMessage("You cannot vote for yourself.");
                return;
            }


            var votedPlayerInfo = TShock.UserAccounts.GetUserAccountByName(votedPlayerName);
            if (votedPlayerInfo == null)
            {
                args.Player.SendErrorMessage($"Player '{votedPlayerName}' not found.");
                return;
            }

            var votedPlayerData = db.QueryReader("SELECT Team, Role FROM main_db.experiment1 WHERE Name = @0", votedPlayerInfo.Name);
            if (!votedPlayerData.Read())
            {
                args.Player.SendErrorMessage($"{votedPlayerInfo.Name} is not in any team.");
                return;
            }

            if (votedPlayerData.Get<string>("Team") != team)
            {
                args.Player.SendErrorMessage($"You can only vote for a player on the same team as you.");
                return;
            }

            if (votedPlayerData.Get<string>("Role").ToLower() == "leader")
            {
                args.Player.SendErrorMessage($"{votedPlayerName} is already the team leader.");
                return;
            }

            // Update HasVoted for the player who voted
            db.Query("UPDATE main_db.experiment1 SET HasVoted = true WHERE Name = @0", args.Player.Name);

            // Increment Votes for the target player
            db.Query("UPDATE main_db.experiment1 SET Votes = Votes + 1 WHERE Name = @0", votedPlayerName);

            args.Player.SendSuccessMessage($"You have successfully voted for {votedPlayerName}.");

            // Check if the voted player has been elected as a new leader
            int voteThreshold = 3; // Adjust the threshold as needed
            var electedPlayer = db.QueryReader("SELECT Name, Team, Votes, Role FROM main_db.experiment1 WHERE Votes >= @0 ORDER BY Votes DESC LIMIT 1", voteThreshold);
            if (electedPlayer.Read())
            {
                string electedPlayerName = electedPlayer.Get<string>("Name");
                string electedPlayerTeam = electedPlayer.Get<string>("Team");
                string electedPlayerRole = electedPlayer.Get<string>("Role");

                // Check if the elected player is already a leader
                if (electedPlayerRole.ToLower() == "leader")
                {
                    args.Player.SendErrorMessage($"{electedPlayerName} is already the team leader.");
                    return;
                }

                // Update the role to 'leader' and reset Votes to 0
                db.Query("UPDATE main_db.experiment1 SET Role = 'leader', Votes = 0 WHERE Name = @0", electedPlayerName);

                // Reset HasVoted for all players on the same team
                db.Query("UPDATE main_db.experiment1 SET HasVoted = false WHERE Team = @0", electedPlayerTeam);

                // Set the previous team leader's role back to 'member'
                db.Query("UPDATE main_db.experiment1 SET Role = 'member' WHERE Team = @0 AND Role = 'leader' AND Name != @1",
                    electedPlayerTeam, electedPlayerName);

                // Broadcast the message to the server
                TShock.Utils.Broadcast($"{electedPlayerName} has just been elected as the new team leader for the {electedPlayerTeam} team!", Microsoft.Xna.Framework.Color.Yellow);
            }
        }

        private void BanishPlayerCommand(CommandArgs args)
        {
            if (args.Parameters.Count == 0)
            {
                args.Player.SendErrorMessage("Please specify a player to banish.");
                return;
            }

            if (!args.Player.IsLoggedIn)
            {
                args.Player.SendErrorMessage("You must be logged in to use this command.");
                return;
            }

            var db = TShock.DB;
            var result = db.QueryReader("SELECT Team, Role FROM main_db.experiment1 WHERE Name = @0", args.Player.Name);
            if (!result.Read())
            {
                args.Player.SendErrorMessage("You are not in a team.");
                return;
            }

            string team = result.Get<string>("Team");
            string role = result.Get<string>("Role");

            if (role.ToLower() != "leader")
            {
                args.Player.SendErrorMessage("You must be a team leader to use this command.");
                return;
            }

            string playerName = args.Parameters.Count > 0 ? args.Parameters[1] : "";
            var playerInfo = TShock.UserAccounts.GetUserAccountByName(playerName);
            if (playerInfo == null)
            {
                args.Player.SendErrorMessage($"Player '{playerName}' not found.");
                return;
            }

            var playerTeam = db.QueryReader("SELECT Team FROM main_db.experiment1 WHERE Name = @0", playerInfo.Name);
            if (!playerTeam.Read())
            {
                args.Player.SendErrorMessage($"{playerInfo.Name} doesn't exist or isn't in a team.");
                return;
            }

            if (playerTeam.Get<string>("Team") != team)
            {
                args.Player.SendErrorMessage($"You can only banish players from your own team.");
                return;
            }

            db.Query("DELETE FROM main_db.experiment1 WHERE Name = @0", playerInfo.Name);
            args.Player.SendSuccessMessage($"{playerInfo.Name} has been banished from the team.");
            SetPlayerTeam(playerName, "none");
        }

        private void TeamListCommand(CommandArgs args)
        {
            if (!args.Player.IsLoggedIn)
            {
                args.Player.SendErrorMessage("You must be logged in to use this command.");
                return;
            }

            var db = TShock.DB;
            var result = db.QueryReader("SELECT Team, Role FROM main_db.experiment1 WHERE Name = @0", args.Player.Name);
            if (!result.Read())
            {
                args.Player.SendErrorMessage("You are not in a team.");
                return;
            }

            string team = result.Get<string>("Team");
            string role = result.Get<string>("Role");

            args.Player.SendMessage($"Members of Team {team}", Microsoft.Xna.Framework.Color.Yellow);

            var teamMembers = db.QueryReader("SELECT Name, Role FROM main_db.experiment1 WHERE Team = @0", team);
            while (teamMembers.Read())
            {
                string playerName = teamMembers.Get<string>("Name");
                string playerRole = teamMembers.Get<string>("Role");


                if (playerRole.ToLower() == "leader")
                {
                    args.Player.SendMessage($"{playerName} (Leader)", Microsoft.Xna.Framework.Color.Yellow);
                }
                else
                {
                    args.Player.SendMessage(playerName, Microsoft.Xna.Framework.Color.White);
                }
            }
        }
    }
}