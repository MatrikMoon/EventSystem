using Discord;
using Discord.Commands;
using EventServer.Database;
using EventServer.Discord.Services;
using EventShared;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using static EventServer.Database.SimpleSql;
using static EventShared.SharedConstructs;
using Discord.WebSocket;
using System.Globalization;

namespace EventServer.Discord.Modules
{
    // Modules must be public and inherit from an IModuleBase
    public class PublicModule : ModuleBase<SocketCommandContext>
    {
        // Dependency Injection will fill this value in for us
        public PictureService PictureService { get; set; }

        //Pull parameters out of an argument list string
        //Note: argument specifiers are required to start with "-"
        private static string ParseArgs(string argString, string argToGet)
        {
            //Return nothing if the parameter arg string is empty
            if (string.IsNullOrWhiteSpace(argString) || string.IsNullOrWhiteSpace(argToGet)) return null;

            List<string> argsWithQuotedStrings = new List<string>();
            string[] argArray = argString.Split(' ');

            for (int x = 0; x < argArray.Length; x++)
            {
                if (argArray[x].StartsWith("\""))
                {
                    string assembledString = string.Empty; //argArray[x].Substring(1) + " ";
                    for (int y = x; y < argArray.Length; y++)
                    {
                        if (argArray[y].StartsWith("\"")) argArray[y] = argArray[y].Substring(1); //Strip quotes off the front of the currently tested word.
                                                                                                  //This is necessary since this part of the code also handles the string right after the open quote
                        if (argArray[y].EndsWith("\""))
                        {
                            assembledString += argArray[y].Substring(0, argArray[y].Length - 1);
                            x = y;
                            break;
                        }
                        else assembledString += argArray[y] + " ";
                    }
                    argsWithQuotedStrings.Add(assembledString);
                }
                else argsWithQuotedStrings.Add(argArray[x]);
            }

            argArray = argsWithQuotedStrings.ToArray();

            for (int i = 0; i < argArray.Length; i++)
            {
                if (argArray[i].ToLower() == $"-{argToGet}".ToLower())
                {
                    if (((i + 1) < (argArray.Length)) && !argArray[i + 1].StartsWith("-"))
                    {
                        return argArray[i + 1];
                    }
                    else return "true";
                }
            }

            return null;
        }

        [Command("serverOptions")]
        [RequireContext(ContextType.Guild)]
        public async Task ServerOptionsAsync([Remainder] string paramString = null)
        {
            ulong moderatorRoleId = Context.Guild.Roles.FirstOrDefault(x => x.Name.ToLower() == "moderator").Id;
            bool isAdmin =
                ((IGuildUser)Context.User).GetPermissions((IGuildChannel)Context.Channel).Has(ChannelPermission.ManageChannel) ||
                ((IGuildUser)Context.User).RoleIds.Any(x => x == moderatorRoleId);

            if (isAdmin)
            {
                if (null != paramString)
                {
                    //Load up the GameOptions and PlayerOptions
                    ServerFlags features = Config.ServerFlags;
                    foreach (ServerFlags o in Enum.GetValues(typeof(ServerFlags)))
                    {
                        var option = ParseArgs(paramString, o.ToString());
                        if (option == "true") features = (features | o);
                        else if (option == "false") features &= ~o;
                    }
                    Config.ServerFlags = features;
                    Config.SaveConfig();

                    await ReplyAsync($"Applied settings: {features.ToString()}");
                }
                else
                {
                    await ReplyAsync($"Current server options: {Config.ServerFlags.ToString()}");
                }
            }
        }

        [Command("register")]
        [RequireContext(ContextType.Guild)]
        public async Task RegisterAsync(string steamId, string timezone = null, IGuildUser user = null)
        {
            ulong moderatorRoleId = Context.Guild.Roles.FirstOrDefault(x => x.Name.ToLower() == "moderator").Id;
            bool isAdmin =
                ((IGuildUser)Context.User).GetPermissions((IGuildChannel)Context.Channel).Has(ChannelPermission.ManageChannel) ||
                ((IGuildUser)Context.User).RoleIds.Any(x => x == moderatorRoleId);

            if (isAdmin)
            {
                user = user ?? (IGuildUser)Context.User;
            }
            else user = (IGuildUser)Context.User;

            //Sanitize input
            if (steamId.StartsWith("https://scoresaber.com/u/"))
            {
                steamId = steamId.Substring("https://scoresaber.com/u/".Length);
            }

            if (steamId.Contains("&"))
            {
                steamId = steamId.Substring(0, steamId.IndexOf("&"));
            }

            steamId = Regex.Replace(steamId, "[^0-9]", "");
            timezone = Regex.Replace(timezone ?? "none", "[^a-zA-Z0-9]", "");

            if (!Player.Exists(steamId) || !Player.IsRegistered(steamId))
            {
                int rank = 0;
                var embed = Context.Message.Embeds.FirstOrDefault();

                if (embed != null)
                {
                    string username = Regex.Replace(user.Username, "[\'\";]", "");

                    Player player = new Player(steamId);
                    player.DiscordName = username;
                    player.DiscordExtension = user.Discriminator;
                    player.DiscordMention = user.Mention;
                    player.Timezone = timezone;

                    //Scrape out the rank data
                    var description = embed.Description;
                    var searchBy = "Player Ranking: #";
                    description = description.Substring(description.IndexOf(searchBy) + searchBy.Length);
                    description = description.Substring(0, description.IndexOf("\n"));

                    rank = Convert.ToInt32(Regex.Replace(description, "[^0-9]", ""));
                    player.Rank = rank;

                    //Add "registered" role
#if TEAMSABER
                    await ((SocketGuildUser)Context.User).AddRoleAsync(Context.Guild.Roles.FirstOrDefault(x => x.Name.ToLower() == "Season Two Registrant".ToLower()));
#endif
                    string reply = $"User `{player.DiscordName}` successfully linked to `{player.SteamId}`";
                    if (rank > 0) reply += $" and rank `{rank}`";
                    await ReplyAsync(reply);
                }
                else await ReplyAsync("Waiting for embedded content...");
            }
            else if (new Player(steamId).DiscordMention!= user.Mention)
            {
                await ReplyAsync($"That steam account is already linked to `{new Player(steamId).DiscordName}`, message an admin if you *really* need to relink it.");
            }
        }

        [Command("addSong")]
        public async Task AddSongAsync(string songId, [Remainder] string paramString = null)
        {
            ulong moderatorRoleId = Context.Guild.Roles.FirstOrDefault(x => x.Name.ToLower() == "moderator").Id;
            bool isAdmin =
                ((IGuildUser)Context.User).GetPermissions((IGuildChannel)Context.Channel).Has(ChannelPermission.ManageChannel) ||
                ((IGuildUser)Context.User).RoleIds.Any(x => x == moderatorRoleId);

            if (isAdmin)
            {
                //Parse the difficulty input, either as an int or a string
                LevelDifficulty parsedDifficulty = LevelDifficulty.ExpertPlus;

                string difficultyArg = ParseArgs(paramString, "difficulty");
                if (difficultyArg != null)
                {
                    //If the enum conversion doesn't succeed, try it as an int
                    if (!Enum.TryParse(difficultyArg, true, out parsedDifficulty))
                    {
                        await ReplyAsync("Could not parse difficulty parameter.\n" +
                        "Usage: addSong [songId] [difficulty]");

                        return;
                    }
                }

                GameOptions gameOptions = GameOptions.None;
                PlayerOptions playerOptions = PlayerOptions.None;

                //Load up the GameOptions and PlayerOptions
                foreach (GameOptions o in Enum.GetValues(typeof(GameOptions)))
                {
                    if (ParseArgs(paramString, o.ToString()) == "true") gameOptions = (gameOptions | o);
                }

                foreach (PlayerOptions o in Enum.GetValues(typeof(PlayerOptions)))
                {
                    if (ParseArgs(paramString, o.ToString()) == "true") playerOptions = (playerOptions | o);
                }

                //Sanitize input
                if (songId.StartsWith("https://beatsaver.com/"))
                {
                    songId = songId.Substring(songId.LastIndexOf("/") + 1);
                }

                if (songId.Contains("&"))
                {
                    songId = songId.Substring(0, songId.IndexOf("&"));
                }

                if (!Song.Exists(songId, parsedDifficulty, true))
                {
                    if (OstHelper.IsOst(songId))
                    {
                        Song song = new Song(songId, parsedDifficulty);
                        song.GameOptions = (int)gameOptions;
                        song.PlayerOptions = (int)playerOptions;
                        await ReplyAsync($"Added: {OstHelper.GetOstSongNameFromLevelId(songId)} ({parsedDifficulty})" +
                                $"{(gameOptions != GameOptions.None ? $" with game options: ({gameOptions.ToString()})" : "")}" +
                                $"{(playerOptions != PlayerOptions.None ? $" with player options: ({playerOptions.ToString()})" : "!")}");
                    }
                    else
                    {
                        string songPath = BeatSaver.BeatSaverDownloader.DownloadSong(songId);
                        if (songPath != null)
                        {
                            BeatSaver.Song song = new BeatSaver.Song(songId);
                            string songName = song.SongName;

                            if (!song.Difficulties.Contains(parsedDifficulty))
                            {
                                LevelDifficulty nextBestDifficulty = song.GetClosestDifficultyPreferLower(parsedDifficulty);

                                if (Song.Exists(songId, nextBestDifficulty))
                                {
                                    await ReplyAsync($"{songName} doesn't have {parsedDifficulty}, and {nextBestDifficulty} is already in the database.\n" +
                                        $"Song not added.");
                                }

                                else
                                {
                                    var databaseSong = new Song(songId, nextBestDifficulty);
                                    databaseSong.GameOptions = (int)gameOptions;
                                    databaseSong.PlayerOptions = (int)playerOptions;
                                    await ReplyAsync($"{songName} doesn't have {parsedDifficulty}, using {nextBestDifficulty} instead.\n" +
                                        $"Added to the song list" +
                                        $"{(gameOptions != GameOptions.None ? $" with game options: ({gameOptions.ToString()})" : "")}" +
                                        $"{(playerOptions != PlayerOptions.None ? $" with player options: ({playerOptions.ToString()})" : "!")}");
                                }
                            }
                            else
                            {
                                var databaseSong = new Song(songId, parsedDifficulty);
                                databaseSong.GameOptions = (int)gameOptions;
                                databaseSong.PlayerOptions = (int)playerOptions;
                                await ReplyAsync($"{songName} ({parsedDifficulty}) downloaded and added to song list" +
                                    $"{(gameOptions != GameOptions.None ? $" with game options: ({gameOptions.ToString()})" : "")}" +
                                    $"{(playerOptions != PlayerOptions.None ? $" with player options: ({playerOptions.ToString()})" : "!")}");
                            }
                        }
                        else await ReplyAsync("Could not download song.");
                    }
                }
                else await ReplyAsync("Song is already active in the database");
            }
        }

        [Command("endEvent")]
        public async Task EndEventAsync()
        {
            ulong moderatorRoleId = Context.Guild.Roles.FirstOrDefault(x => x.Name.ToLower() == "moderator").Id;
            bool isAdmin =
                ((IGuildUser)Context.User).GetPermissions((IGuildChannel)Context.Channel).Has(ChannelPermission.ManageChannel) ||
                ((IGuildUser)Context.User).RoleIds.Any(x => x == moderatorRoleId);

            if (isAdmin)
            {
                //If the server has tokens enabled, dish out tokens to the players now
                if (Config.ServerFlags.HasFlag(ServerFlags.Tokens))
                {
                    var tokenDispersal = GetTokenDispersal();

                    foreach (var key in tokenDispersal.Keys)
                    {
                        key.Tokens += tokenDispersal[key];
                    }
                }

                //Mark all songs and scores as old
                MarkAllOld();
                await ReplyAsync($"All songs and scores are marked as Old.{(Config.ServerFlags.HasFlag(ServerFlags.Tokens) ? " Tokens dispersed." : "")} You may now add new songs.");
            }
        }

        [Command("rarity")]
        [RequireBotPermission(GuildPermission.ManageRoles)]
        public async Task RarityAsync(string role, IGuildUser user = null)
        {
            role = role.ToLower();
            ulong moderatorRoleId = Context.Guild.Roles.FirstOrDefault(x => x.Name.ToLower() == "moderator").Id;
            bool isAdmin =
                ((IGuildUser)Context.User).GetPermissions((IGuildChannel)Context.Channel).Has(ChannelPermission.ManageChannel) ||
                ((IGuildUser)Context.User).RoleIds.Any(x => x == moderatorRoleId);
            if (isAdmin) user = user ?? (IGuildUser)Context.User; //Admins can assign roles for others
            else user = (IGuildUser)Context.User;

            if ((isAdmin) && CommunityBot._rarityRoles.Contains(role))
            {
                Player player = Player.GetByDiscordMetion(user.Mention);
                if (player == null) await ReplyAsync("That user has not registered with the bot yet");
                else if (Enum.TryParse(char.ToUpper(role[0]) + role.Substring(1), out Rarity rarity)) CommunityBot.ChangeRarity(player, rarity);
                else await ReplyAsync("Role parse failed");
            }
            else await ReplyAsync("You are not authorized to assign that role");
        }

        [Command("crunchRarities")]
        [RequireBotPermission(GuildPermission.ManageRoles)]
        public async Task CrunchRaritiesAsync()
        {
            ulong moderatorRoleId = Context.Guild.Roles.FirstOrDefault(x => x.Name.ToLower() == "moderator").Id;
            bool isAdmin =
                ((IGuildUser)Context.User).GetPermissions((IGuildChannel)Context.Channel).Has(ChannelPermission.ManageChannel) ||
                ((IGuildUser)Context.User).RoleIds.Any(x => x == moderatorRoleId);

            if (isAdmin)
            {
                var players = GetAllPlayers()
                    .Where(x => x.Rarity== -1 && Team.GetByDiscordMentionOfCaptain(x.DiscordMention) == null)
                    .OrderBy(x => x.Rank)
                    .ToList(); //Database complexity hell. Oh well.

                int count = players.Count;
                int threshold = count / 6; //Divide players by number of rarities for an even split

                string reply = $"Registrant count: {players.Count}\n" +
                    $"Tiers: 6\n" +
                    $"Players per tier: {threshold}\n\n";

                int index = 0;
                Rarity currentRarity = Rarity.SSS;

                players.ForEach(x =>
                {
                    x.Rarity = (int)currentRarity;

                    //Increment current rarity based on how many players we've iterated over
                    if (++index >= threshold)
                    {
                        index = 0;
                        currentRarity--;
                    }
                });

                reply += "Rarity Assignments:\n\n";

                index = 0;
                currentRarity = Rarity.SS;

                players.ForEach(x =>
                {
                    reply += $"{x.DiscordName} ({x.Timezone}) - {(Rarity)x.Rarity}\n";

                    //Increment current rarity based on how many players we've iterated over
                    if (++index >= threshold)
                    {
                        reply += $"\n\n{currentRarity}:\n";
                        index = 0;
                        currentRarity--;
                    }
                });

                //Deal with long messages
                if (reply.Length > 2000)
                {
                    for (int i = 0; reply.Length > 2000; i++)
                    {
                        await ReplyAsync(reply.Substring(0, reply.Length > 2000 ? 2000 : reply.Length));
                        reply = reply.Substring(2000);
                    }
                }
                await ReplyAsync(reply);
            }
        }

        [Command("createTeam")]
        [RequireBotPermission(GuildPermission.ManageRoles)]
        public async Task CreateTeamAsync(string teamId, [Remainder] string args)
        {
            teamId = teamId.ToLower();
            ulong moderatorRoleId = Context.Guild.Roles.FirstOrDefault(x => x.Name.ToLower() == "moderator").Id;
            bool isAdmin =
                ((IGuildUser)Context.User).GetPermissions((IGuildChannel)Context.Channel).Has(ChannelPermission.ManageChannel) ||
                ((IGuildUser)Context.User).RoleIds.Any(x => x == moderatorRoleId);

            if (isAdmin)
            {
                var name = ParseArgs(args, "name") ?? "Default Team Name";
                var color = ParseArgs(args, "color") ?? "#ffffff";

                //Community group rank progression
                var requiredTokens = ParseArgs(args, "requiredTokens");
                var nextPromotion = ParseArgs(args, "nextPromotion");

                if (!Team.Exists(teamId))
                {
                    AddTeam(teamId, name, "", color, 0);

                    if (requiredTokens != null && nextPromotion != null)
                    {
                        var team = new Team(teamId);
                        team.RequiredTokens = Convert.ToInt32(requiredTokens);
                        team.NextPromotion = nextPromotion;
                    }

                    //If the provided color can be parsed into a uint, we can use the provided color as the color for the dicsord role
                    Color discordColor = Color.Blue;
                    if (uint.TryParse(color.Substring(1), NumberStyles.HexNumber, CultureInfo.InvariantCulture, out var discordColorUint)) discordColor = new Color(discordColorUint);

                    await Context.Guild.CreateRoleAsync(name, color: discordColor, isHoisted: true);
                    await ReplyAsync($"Team created with id `{teamId}`, name `{name}`, and color `{color}`");
                }
                else await ReplyAsync("That team already exists, sorry.");
            }
            else await ReplyAsync("You are not authorized to create teams");
        }

        [Command("modifyTeam")]
        [RequireBotPermission(GuildPermission.ManageRoles)]
        public async Task ModifyTeamAsync([Remainder]string args = null)
        {
            var color = ParseArgs(args, "color");
            var name = ParseArgs(args, "name");
            var teamId = ParseArgs(args, "teamId");

            if (teamId == null)
            {
                teamId = Team.GetByDiscordMentionOfCaptain(Context.User.Mention)?.TeamId;
                if (teamId == null)
                {
                    await ReplyAsync("You must provide the TeamId at the beginning of the command, since you are not the captian of any team");
                    return;
                }
            }

            var currentRoles = Context.Guild.Roles;

            teamId = teamId.ToLower();
            ulong moderatorRoleId = currentRoles.FirstOrDefault(x => x.Name.ToLower() == "moderator").Id;
            bool isAdmin =
                ((IGuildUser)Context.User).GetPermissions((IGuildChannel)Context.Channel).Has(ChannelPermission.ManageChannel) ||
                ((IGuildUser)Context.User).RoleIds.Any(x => x == moderatorRoleId);

            if (Team.Exists(teamId))
            {
                //Check if the person issuing the command is the captain of the team.
                //Note: this was more relevant when the command required the issuer to input the team id instead of finding it for them,
                //though, it's still relevant since any person could *try* to modify the team still. They should just get a permission required message
                Team currentTeam = new Team(teamId);
                string captain = currentTeam.Captain;
                bool isCaptain = string.IsNullOrEmpty(captain) ? true : captain == Player.GetByDiscordMetion(Context.User.Mention).SteamId; //In the case of teams, team captains count as admins

                if (isAdmin || isCaptain)
                {
                    //If the provided color can be parsed into a uint, we can use the provided color as the color for the dicsord role
                    Color? discordColor = null;
                    if (color != null && uint.TryParse(color.Substring(1), NumberStyles.HexNumber, CultureInfo.InvariantCulture, out var discordColorUint)) discordColor = new Color(discordColorUint);

                    if (name != null && currentRoles.Any(x => Regex.Replace(x.Name.ToLower(), "[^a-z0-9 ]", "") == Regex.Replace(name.ToLower(), "[^a-z0-9 ]", "")))
                    {
                        await ReplyAsync("A role with that name already exists. Please use a different name");
                        return;
                    }

                    currentRoles.FirstOrDefault(x => Regex.Replace(x.Name.ToLower(), "[^a-z0-9 ]", "") == currentTeam.TeamName.ToLower())?
                        .ModifyAsync(x =>
                            {
                                if (name != null) x.Name = name;
                                if (color != null) x.Color = (Color)discordColor;
                            }
                        );

                    if (color != null) currentTeam.Color = color;
                    if (name != null) currentTeam.TeamName = name;

                    await ReplyAsync($"Team with id `{teamId}` now has name `{name ?? currentTeam.TeamName}` and color `{color ?? currentTeam.Color}`");
                }
                else await ReplyAsync("You are not authorized to modify this team");
            }
            else await ReplyAsync("That team does not exist, sorry.");
        }

        [Command("assignTeam")]
        [RequireBotPermission(GuildPermission.ManageRoles)]
        public async Task AssignTeamAsync(IGuildUser user, string teamId = null, string captain = null)
        {
            if (teamId == null)
            {
                teamId = Team.GetByDiscordMentionOfCaptain(Context.User.Mention)?.TeamId;
                if (teamId == null)
                {
                    await ReplyAsync("You must provide the TeamId at the end of the command, since you are not the captian of any team");
                    return;
                }
            }

            bool setToCaptain = "captain" == captain;
            teamId = teamId.ToLower();
            ulong moderatorRoleId = Context.Guild.Roles.FirstOrDefault(x => x.Name.ToLower() == "moderator").Id;
            bool isAdmin =
                ((IGuildUser)Context.User).GetPermissions((IGuildChannel)Context.Channel).Has(ChannelPermission.ManageChannel) ||
                ((IGuildUser)Context.User).RoleIds.Any(x => x == moderatorRoleId);

            user = user ?? (IGuildUser)Context.User; //Anyone who can assign teams can assign roles for others

            if (Team.Exists(teamId)) {
                //Check if the person issuing the command is the captain of the team.
                //Note: this was more relevant when the command required the issuer to input the team id instead of finding it for them,
                //though, it's still relevant since any person could *try* to modify the team still. They should just get a permission required message
                Team currentTeam = new Team(teamId);
                string currentCaptain = currentTeam.Captain;
                bool isCaptain = string.IsNullOrEmpty(currentCaptain) ? true : currentCaptain == Player.GetByDiscordMetion(Context.User.Mention).SteamId; //In the case of teams, team captains count as admins

                if (isAdmin || isCaptain)
                {
                    Player player = Player.GetByDiscordMetion(user.Mention);
                    if (player == null) await ReplyAsync("That user has not registered with the bot yet");
                    else if (isCaptain && !isAdmin && player.Team!= "-1") await ReplyAsync("This person is already on a team");
                    else CommunityBot.ChangeTeam(player, currentTeam, setToCaptain);
                }
                else await ReplyAsync("You are not authorized to assign that role");
            }
            else await ReplyAsync("Team does not exist");
        }

        [Command("leaderboards")]
        public async Task LeaderboardsAsync()
        {
            ulong moderatorRoleId = Context.Guild.Roles.FirstOrDefault(x => x.Name.ToLower() == "moderator").Id;
            bool isAdmin =
                ((IGuildUser)Context.User).GetPermissions((IGuildChannel)Context.Channel).Has(ChannelPermission.ManageChannel) ||
                ((IGuildUser)Context.User).RoleIds.Any(x => x == moderatorRoleId);
            if (!isAdmin) return;

            string finalMessage = "Leaderboard:\n\n";
            List<SongConstruct> songs = GetActiveSongs(true);

            songs.ForEach(x =>
            {
                string songId = x.SongId;

                if (x.Scores.Count > 0) //Don't print if no one submitted scores
                {
                    var song = new Song(songId, x.Difficulty);
                    finalMessage += song.SongName + ":\n";

                    int place = 1;
                    foreach (ScoreConstruct item in x.Scores)
                    {
                        //Incredibly inefficient to open a song info file every time, but only the score structure is guaranteed to hold the real difficutly,
                        //seeing as auto difficulty is what would be represented in the songconstruct
                        var maxScore = new BeatSaver.Song(songId).GetMaxScore(item.Difficulty);

                        var percentage = ((double)item.Score / maxScore).ToString("P", CultureInfo.InvariantCulture);
                        finalMessage += place + ": " + new Player(item.PlayerId).DiscordName+ " - " + item.Score + $" ({percentage})" + (item.FullCombo ? " (Full Combo)" : "");
                        if (Config.ServerFlags.HasFlag(ServerFlags.Tokens))
                        {
                            if (place == 1) finalMessage += " (+3 Tokens)";
                            else if (place == 2) finalMessage += " (+2 Tokens)";
                            else if (place == 3) finalMessage += " (+1 Token)";
                        }
                        finalMessage += "\n";
                        place++;
                    }
                    finalMessage += "\n";
                }
            });

            //Deal with long messages
            if (finalMessage.Length > 2000)
            {
                for (int i = 0; finalMessage.Length > 2000; i++)
                {
                    await ReplyAsync(finalMessage.Substring(0, finalMessage.Length > 2000 ? 2000 : finalMessage.Length));
                    finalMessage = finalMessage.Substring(2000);
                }
            }
            await ReplyAsync(finalMessage);
        }

        [Command("lava")]
        public async Task LavaAsync()
        {
            string finalMessage = "-----Current team averages-----\n\n";

            GetAllScores().ForEach(s =>
            {
                finalMessage += s.Name + ":\n\n";
                Dictionary<string, double> finalAverages = new Dictionary<string, double>();

                var maxPossiblePoints = new BeatSaver.Song(s.SongId).GetMaxScore(s.Difficulty);
                GetAllTeams().ForEach(t =>
                {
                    finalAverages[t.TeamId] = 0;
                    var scores = s.Scores.OrderByDescending(x => x.Score).Where(sc => sc.TeamId == t.TeamId).Take(4).ToList();

                    if (scores.Count < 4) finalMessage += $"Note: {t.TeamName} ({t.TeamId}) is missing {4 - scores.Count} scores.\n";

                    while (scores.Count < 4) scores.Add(new ScoreConstruct() { Score = 0 }); //Fill out up to four scores
                    scores.ForEach(sc => finalAverages[t.TeamId] += sc.Score);

                    finalAverages[t.TeamId] = finalAverages[t.TeamId] / (maxPossiblePoints * 4);
                });

                finalMessage += "\n";

                finalAverages.Where(x => new Team(x.Key).Score > 0).OrderByDescending(x => x.Value).ThenByDescending(x => new Team(x.Key).Score).ToList().ForEach(x => finalMessage += $"{new Team(x.Key).TeamName} - Average accuracy: {x.Value.ToString("P", CultureInfo.InvariantCulture)}\n");

                finalMessage += "\n\n";
            });

            //Deal with long messages
            if (finalMessage.Length > 2000)
            {
                for (int i = 0; finalMessage.Length > 2000; i++)
                {
                    await ReplyAsync(finalMessage.Substring(0, finalMessage.Length > 2000 ? 2000 : finalMessage.Length));
                    finalMessage = finalMessage.Substring(2000);
                }
            }
            await ReplyAsync(finalMessage);
        }

        [Command("tokens")]
        public async Task TokensAsync()
        {
            var tokenDispersal = GetTokenDispersal();

            string finalMessage = "Tokens:\n";

            foreach (var key in tokenDispersal.Keys)
            {
                finalMessage += $"{key.DiscordName}: {tokenDispersal[key]}\n";
            }

            await ReplyAsync(finalMessage);
        }

        [Command("listTeams")]
        public async Task ListTeamsAsync()
        {
            ulong moderatorRoleId = Context.Guild.Roles.FirstOrDefault(x => x.Name.ToLower() == "moderator").Id;
            bool isAdmin =
                ((IGuildUser)Context.User).GetPermissions((IGuildChannel)Context.Channel).Has(ChannelPermission.ManageChannel) ||
                ((IGuildUser)Context.User).RoleIds.Any(x => x == moderatorRoleId);
            if (!isAdmin) return;

            string finalMessage = "Teams:\n\n";
            List<Team> teams = GetAllTeams();

            teams.ForEach(x =>
            {
                finalMessage += $"{x.TeamId}: {x.TeamName} | {new Player(x.Captain).DiscordName}\n";
            });

            await ReplyAsync(finalMessage);
        }

        [Command("help")]
        public async Task HelpAsync()
        {
            string ret = "```Key:\n\n" +
                "[] -> parameter\n" +
                "<> -> optional parameter / admin-only parameter\n" +
                "() -> Extra notes about command```\n\n" +
                "```Commands:\n\n" +
                "register [scoresaber link] [timezone] <@User>\n" +
                "addSong [beatsaver url] <-difficulty> (<difficulty> can be either a number or whole-word, such as 4 or ExpertPlus)\n" +
                "endEvent\n" +
                "rarity [rarity] <@User> (assigns a rarity to a user. Likely to remain unused)\n" +
                "crunchRarities (looks at the ranks of all registered players and assigns rarities accordingly)\n" +
                "createTeam [teamId] <name> <color> (teamId should be in the form of teamX, where X is the next teamId number in line, name must be no-spaces, and color must start with #)\n" +
                "modifyTeam <teamId> <-name> <-color> (admins can override the teamId they're changing)\n" +
                "assignTeam [@User] <teamId> <captain> (admins can assign users to a team specified in teamId, captains auto-assign to their own team. If the captain parameter is \"captain\", that user becomes the captain of the team)\n" +
                "leaderboards (Shows leaderboards... Duh. Moderators only)\n" +
                "listTeams (Shows a list of teams. Moderators only)\n" +
                "help (This message!)```";
            await ReplyAsync(ret);
        }

        [Command("cat")]
        public async Task CatAsync()
        {
            // Get a stream containing an image of a cat
            var stream = await PictureService.GetCatPictureAsync();
            // Streams must be seeked to their beginning before being uploaded!
            stream.Seek(0, SeekOrigin.Begin);
            await Context.Channel.SendFileAsync(stream, "cat.png");
        }

        [Command("neko")]
        public async Task NekoAsync()
        {
            // Get a stream containing an image of a cat
            var stream = await PictureService.GetNekoStreamAsync(PictureService.NekoType.Neko);
            // Streams must be seeked to their beginning before being uploaded!
            stream.Seek(0, SeekOrigin.Begin);
            await Context.Channel.SendFileAsync(stream, "neko.png");
        }

        [Command("nekolewd")]
        [RequireNsfw]
        public async Task NekoLewdAsync()
        {
            // Get a stream containing an image of a cat
            var stream = await PictureService.GetNekoStreamAsync(PictureService.NekoType.NekoLewd);
            // Streams must be seeked to their beginning before being uploaded!
            stream.Seek(0, SeekOrigin.Begin);
            await Context.Channel.SendFileAsync(stream, "nekolewd.png");
        }

        [Command("nekogif")]
        public async Task NekoGifAsync()
        {
            // Get a stream containing an image of a cat
            var gifLink = await PictureService.GetNekoGifAsync();

            var builder = new EmbedBuilder();
            builder.WithImageUrl(gifLink);

            await ReplyAsync("", false, builder);
        }

        [Command("nekolewdgif")]
        [RequireNsfw]
        public async Task NekoLewdGifAsync()
        {
            // Get a stream containing an image of a cat
            var gifLink = await PictureService.GetNekoLewdGifAsync();

            var builder = new EmbedBuilder();
            builder.WithImageUrl(gifLink);

            await ReplyAsync("", false, builder);
        }

        [Command("lewd")]
        [RequireNsfw]
        public async Task LewdAsync()
        {
            // Get a stream containing an image of a cat
            var stream = await PictureService.GetNekoStreamAsync(PictureService.NekoType.Hentai);
            // Streams must be seeked to their beginning before being uploaded!
            stream.Seek(0, SeekOrigin.Begin);
            await Context.Channel.SendFileAsync(stream, "lewd.png");
        }

        [Command("lewdgif")]
        [RequireNsfw]
        public async Task LewdGifAsync()
        {
            // Get a stream containing an image of a cat
            var gifLink = await PictureService.GetLewdGifAsync();

            var builder = new EmbedBuilder();
            builder.WithImageUrl(gifLink);

            await ReplyAsync("", false, builder);
        }

        [Command("lewdsmall")]
        [RequireNsfw]
        public async Task LewdSmallAsync()
        {
            // Get a stream containing an image of a cat
            var stream = await PictureService.GetNekoStreamAsync(PictureService.NekoType.HentaiSmall);
            // Streams must be seeked to their beginning before being uploaded!
            stream.Seek(0, SeekOrigin.Begin);
            await Context.Channel.SendFileAsync(stream, "lewdsmall.png");
        }
    }
}
