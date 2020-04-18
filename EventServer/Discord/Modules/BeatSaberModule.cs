using Discord;
using Discord.Commands;
using EventServer.Database;
using EventShared;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using static EventServer.Database.SqlUtils;
using static EventShared.SharedConstructs;

/**
 * Created by Moon on ??/??/2018
 * A Discord.NET module for Beat Saber Events related commands
 */

namespace EventServer.Discord.Modules
{
    public class BeatSaberModule : ModuleBase<SocketCommandContext>
    {
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

        private bool IsAdmin()
        {
            ulong moderatorRoleId = Context.Guild.Roles.FirstOrDefault(x => CommunityBot.AdminRoles.Contains(x.Name.ToLower())).Id;
            bool isAdmin =
                ((IGuildUser)Context.User).GetPermissions((IGuildChannel)Context.Channel).Has(ChannelPermission.ManageChannels) ||
                ((IGuildUser)Context.User).RoleIds.Any(x => x == moderatorRoleId);
            return isAdmin;
        }

        [Command("serverOptions")]
        [RequireContext(ContextType.Guild)]
        public async Task ServerOptionsAsync([Remainder] string paramString = null)
        {
            if (IsAdmin())
            {
                if (null != paramString)
                {
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
        public async Task RegisterAsync(string userId, IGuildUser user = null, [Remainder]string extras = null)
        {
            if (IsAdmin())
            {
                user = user ?? (IGuildUser)Context.User;
            }
            else user = (IGuildUser)Context.User;

            //Sanitize input
            if (userId.StartsWith("https://scoresaber.com/u/"))
            {
                userId = userId.Substring("https://scoresaber.com/u/".Length);
            }

            if (userId.Contains("&"))
            {
                userId = userId.Substring(0, userId.IndexOf("&"));
            }

            userId = Regex.Replace(userId, "[^0-9]", "");
            extras = ParseArgs(extras, "extras");
            extras = Regex.Replace(extras ?? "none", "[^a-zA-Z0-9 :-]", "");

#if ASIAVR
            //Special roles for tournamonth
            bool isTier1 = (Context.User as IGuildUser).RoleIds.Contains((ulong)572765180140716062);
            bool isTier2 = (Context.User as IGuildUser).RoleIds.Contains((ulong)572765611709693954);

            if (!isTier1 && !isTier2)
            {
                await ReplyAsync("Please select a tier by reacting to the message in the tournamonth channel");
                return;
            }
#endif

            if (!Player.Exists(userId) || !Player.IsRegistered(userId))
            {
                int rank = 0;
                var embed = Context.Message.Embeds.FirstOrDefault();

                if (embed != null)
                {
                    string username = Regex.Replace(user.Username, "[\'\";]", "");

                    Player player = new Player(userId);
                    player.DiscordName = username;
                    player.DiscordExtension = user.Discriminator;
                    player.DiscordMention = user.Mention;
                    player.Extras = extras;

                    //Scrape out the rank data
                    var description = embed.Description;
                    var searchBy = "Player Ranking: #";
                    description = description.Substring(description.IndexOf(searchBy) + searchBy.Length);
                    description = description.Substring(0, description.IndexOf("\n"));

                    rank = Convert.ToInt32(Regex.Replace(description, "[^0-9]", ""));
                    player.Rank = rank;

#if ASIAVR
                    //Assign them to the proper tournamonth team
                    if (isTier1) CommunityBot.ChangeTeam(player, new Team("tier1"), role: Context.Guild.GetRole(572765180140716062));
                    else if (isTier2) CommunityBot.ChangeTeam(player, new Team("tier2"), role: Context.Guild.GetRole(572765611709693954));
#endif

#if TEAMSABER
                    //Add "registered" role
                    await ((SocketGuildUser)Context.User).AddRoleAsync(Context.Guild.Roles.FirstOrDefault(x => x.Name.ToLower() == "Season Two Registrant".ToLower()));
#endif
                    string reply = $"User `{player.DiscordName}` successfully linked to `{player.UserId}`";
                    if (rank > 0) reply += $" with rank `{rank}`";
                    await ReplyAsync(reply);
                }
                else await ReplyAsync("Waiting for embedded content...");
            }
            else if (new Player(userId).DiscordMention!= user.Mention)
            {
                await ReplyAsync($"That steam account is already linked to `{new Player(userId).DiscordName}`, message an admin if you *really* need to relink it.");
            }
        }

        [Command("addSong")]
        public async Task AddSongAsync(string songId, [Remainder] string paramString = null)
        {
            if (IsAdmin())
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

                string characteristicArg = ParseArgs(paramString, "characteristic");
                characteristicArg = characteristicArg ?? "Standard";

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
                if (songId.StartsWith("https://beatsaver.com/") || songId.StartsWith("https://bsaber.com/"))
                {
                    //Strip off the trailing slash if there is one
                    if (songId.EndsWith("/")) songId = songId.Substring(0, songId.Length - 1);

                    //Strip off the beginning of the url to leave the id
                    songId = songId.Substring(songId.LastIndexOf("/") + 1);
                }

                if (songId.Contains("&"))
                {
                    songId = songId.Substring(0, songId.IndexOf("&"));
                }

                //Get the hash for the song
                var hash = BeatSaver.BeatSaverDownloader.GetHashFromID(songId);

                if (OstHelper.IsOst(hash))
                {
                    if (!Song.Exists(hash, parsedDifficulty, characteristicArg, true))
                    {
                        Song song = new Song(hash, parsedDifficulty, characteristicArg);
                        song.GameOptions = (int)gameOptions;
                        song.PlayerOptions = (int)playerOptions;
                        await ReplyAsync($"Added: {OstHelper.GetOstSongNameFromLevelId(hash)} ({parsedDifficulty}) ({characteristicArg})" +
                                $"{(gameOptions != GameOptions.None ? $" with game options: ({gameOptions.ToString()})" : "")}" +
                                $"{(playerOptions != PlayerOptions.None ? $" with player options: ({playerOptions.ToString()})" : "!")}");
                    }
                    else await ReplyAsync("Song is already active in the database");
                }
                else
                {
                    string songPath = BeatSaver.BeatSaverDownloader.DownloadSong(hash);
                    if (songPath != null)
                    {
                        BeatSaver.Song song = new BeatSaver.Song(hash);
                        string songName = song.SongName;

                        if (parsedDifficulty != LevelDifficulty.Auto && !song.GetLevelDifficulties(characteristicArg).Contains(parsedDifficulty))
                        {
                            LevelDifficulty nextBestDifficulty = song.GetClosestDifficultyPreferLower(parsedDifficulty);

                            if (Song.Exists(hash, nextBestDifficulty, characteristicArg))
                            {
                                await ReplyAsync($"{songName} doesn't have {parsedDifficulty}, and {nextBestDifficulty} is already in the database.\n" +
                                    $"Song not added.");
                            }

                            else
                            {
                                var databaseSong = new Song(hash, nextBestDifficulty, characteristicArg);
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
                            var databaseSong = new Song(hash, parsedDifficulty, characteristicArg);
                            databaseSong.GameOptions = (int)gameOptions;
                            databaseSong.PlayerOptions = (int)playerOptions;
                            await ReplyAsync($"{songName} ({parsedDifficulty}) ({characteristicArg}) downloaded and added to song list" +
                                $"{(gameOptions != GameOptions.None ? $" with game options: ({gameOptions.ToString()})" : "")}" +
                                $"{(playerOptions != PlayerOptions.None ? $" with player options: ({playerOptions.ToString()})" : "!")}");
                        }
                    }
                    else await ReplyAsync("Could not download song.");
                }
            }
        }

        [Command("removeSong")]
        public async Task RemoveSongAsync(string songId, [Remainder] string paramString = null)
        {
            if (IsAdmin())
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
                        "Usage: removeSong [songId] [difficulty]");

                        return;
                    }
                }

                string characteristicArg = ParseArgs(paramString, "characteristic");
                characteristicArg = characteristicArg ?? "Standard";

                //Sanitize input
                if (songId.StartsWith("https://beatsaver.com/") || songId.StartsWith("https://bsaber.com/"))
                {
                    //Strip off the trailing slash if there is one
                    if (songId.EndsWith("/")) songId = songId.Substring(0, songId.Length - 1);

                    //Strip off the beginning of the url to leave the id
                    songId = songId.Substring(songId.LastIndexOf("/") + 1);
                }

                if (songId.Contains("&"))
                {
                    songId = songId.Substring(0, songId.IndexOf("&"));
                }

                //Get the hash for the song
                var hash = BeatSaver.BeatSaverDownloader.GetHashFromID(songId);

                if (Song.Exists(hash, parsedDifficulty, characteristicArg, true))
                {
                    var song = new Song(hash, parsedDifficulty, characteristicArg);
                    song.Old = true;
                    await ReplyAsync($"Removed {song.SongName} ({song.Difficulty}) ({song.Characteristic}) from the song list");
                }
                else await ReplyAsync("Specified song does not exist with that difficulty and characteristic");
            }
        }

        [Command("endEvent")]
        public async Task EndEventAsync()
        {
            if (IsAdmin())
            {
                //Make server backup
                Logger.Warning($"BACKING UP DATABASE...");
                File.Copy("EventDatabase.db", $"EventDatabase_bak_{DateTime.Now.Day}_{DateTime.Now.Hour}_{DateTime.Now.Minute}_{DateTime.Now.Second}.db");
                Logger.Success("Database backed up succsessfully.");


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

        [Command("createTeam")]
        [RequireBotPermission(GuildPermission.ManageRoles)]
        public async Task CreateTeamAsync(string teamId, [Remainder] string args)
        {
            teamId = teamId.ToLower();

            if (IsAdmin())
            {
                var name = ParseArgs(args, "name") ?? "Default Team Name";
                var color = ParseArgs(args, "color") ?? "#ffffff";

                //Community group rank progression
                var requiredTokens = ParseArgs(args, "requiredTokens");
                var nextPromotion = ParseArgs(args, "nextPromotion");

                if (!Team.Exists(teamId))
                {
                    AddTeam(teamId, name, "", color, 0, "");

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

            if (Team.Exists(teamId))
            {
                //Check if the person issuing the command is the captain of the team.
                //Note: this was more relevant when the command required the issuer to input the team id instead of finding it for them,
                //though, it's still relevant since any person could *try* to modify the team still. They should just get a permission required message
                Team currentTeam = new Team(teamId);
                string captain = currentTeam.Captain;
                bool isCaptain = string.IsNullOrEmpty(captain) ? true : captain == Player.GetByDiscordMetion(Context.User.Mention).UserId; //In the case of teams, team captains count as admins

                if (IsAdmin() || isCaptain)
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

            user = user ?? (IGuildUser)Context.User; //Anyone who can assign teams can assign roles for others

            if (Team.Exists(teamId)) {
                //Check if the person issuing the command is the captain of the team.
                //Note: this was more relevant when the command required the issuer to input the team id instead of finding it for them,
                //though, it's still relevant since any person could *try* to modify the team still. They should just get a permission required message
                Team currentTeam = new Team(teamId);
                string currentCaptain = currentTeam.Captain;
                bool isCaptain = string.IsNullOrEmpty(currentCaptain) ? true : currentCaptain == Player.GetByDiscordMetion(Context.User.Mention).UserId; //In the case of teams, team captains count as admins
                var isAdmin = IsAdmin();

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
            if (!IsAdmin()) return;

            string finalMessage = "Leaderboard:\n\n";

            if (Config.ServerFlags.HasFlag(ServerFlags.Tokens))
            {
                foreach (var team in GetAllTeams())
                {
                    finalMessage += $"({team.TeamName})\n\n";

                    foreach (var song in GetAllScores(teamId: team.TeamId))
                    {
                        if (song.Scores.Count > 0) //Don't print if no one submitted scores
                        {
                            finalMessage += $"{song.Name} ({song.Scores.First().Difficulty}):\n";

                            int place = 1;
                            foreach (var score in song.Scores)
                            {
                                //Incredibly inefficient to open a song info file every time, but only the score structure is guaranteed to hold the real difficutly,
                                //seeing as auto difficulty is what would be represented in the songconstruct
                                string percentage = "???%";
                                if (!OstHelper.IsOst(song.SongHash))
                                {
                                    var maxScore = new BeatSaver.Song(song.SongHash).GetMaxScore(score.Characteristic, score.Difficulty);
                                    percentage = ((double)score.Score / maxScore).ToString("P", CultureInfo.InvariantCulture);
                                }

                                finalMessage += place + ": " + new Player(score.UserId).DiscordName + " - " + score.Score + $" ({percentage})" + (score.FullCombo ? " (Full Combo)" : "");
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
                    }
                    finalMessage += "\n";
                }
            }
            else
            {
                List<SongConstruct> songs = GetActiveSongs(true);

                songs.ForEach(x =>
                {
                    string hash = x.SongHash;

                    if (x.Scores.Count > 0) //Don't print if no one submitted scores
                    {
                        var song = new Song(hash, x.Difficulty, x.Characteristic);
                        finalMessage += song.SongName + ":\n";

                        int place = 1;
                        foreach (ScoreConstruct item in x.Scores)
                        {
                            //Incredibly inefficient to open a song info file every time, but only the score structure is guaranteed to hold the real difficutly,
                            //seeing as auto difficulty is what would be represented in the songconstruct
                            string percentage = "???%";
                            if (!OstHelper.IsOst(hash))
                            {
                                var maxScore = new BeatSaver.Song(hash).GetMaxScore(item.Characteristic, item.Difficulty);
                                percentage = ((double)item.Score / maxScore).ToString("P", CultureInfo.InvariantCulture);
                            }

                            finalMessage += place + ": " + new Player(item.UserId).DiscordName + " - " + item.Score + $" ({percentage})" + (item.FullCombo ? " (Full Combo)" : "");
                            finalMessage += "\n";
                            place++;
                        }
                        finalMessage += "\n";
                    }
                });
            }

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
            if (!IsAdmin()) return;

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
                "register [scoresaber link] <extras> <@User>\n" +
                "addSong [beatsaver url] <-difficulty> (<difficulty> can be either a number or whole-word, such as 4 or ExpertPlus)\n" +
                "endEvent\n" +
                "crunchRarities (looks at the ranks of all registered players and assigns rarities accordingly)\n" +
                "createTeam [teamId] <name> <color> (teamId should be in the form of teamX, where X is the next teamId number in line, name must be no-spaces, and color must start with #)\n" +
                "modifyTeam <teamId> <-name> <-color> (admins can override the teamId they're changing)\n" +
                "assignTeam [@User] <teamId> <captain> (admins can assign users to a team specified in teamId, captains auto-assign to their own team. If the captain parameter is \"captain\", that user becomes the captain of the team)\n" +
                "leaderboards (Shows leaderboards... Duh. Moderators only)\n" +
                "listTeams (Shows a list of teams. Moderators only)\n" +
                "help (This message!)```";
            await ReplyAsync(ret);
        }
    }
}
