using Discord;
using Discord.Commands;
using TeamSaberServer.Database;
using TeamSaberServer.Discord.Services;
using TeamSaberShared;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using static TeamSaberServer.Database.SimpleSql;
using static TeamSaberShared.SharedConstructs;
using Discord.WebSocket;
using System.Globalization;

namespace TeamSaberServer.Discord.Modules
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

            string[] argArray = argString.Split(' ');

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

        [Command("register")]
        [RequireContext(ContextType.Guild)]
        public async Task RegisterAsync(string steamId, string timezone, IGuildUser user = null)
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
            timezone = Regex.Replace(timezone, "[^a-zA-Z0-9]", "");

            if (!Player.Exists(steamId) || !Player.IsRegistered(steamId))
            {
                int rank = 0;
                var embed = Context.Message.Embeds.FirstOrDefault();

                if (embed != null)
                {
                    string username = Regex.Replace(user.Username, "[\'\";]", "");

                    Player player = new Player(steamId);
                    player.SetDiscordName(username);
                    player.SetDiscordExtension(user.Discriminator);
                    player.SetDiscordMention(user.Mention);
                    player.SetTimezone(timezone);

                    //Scrape out the rank data
                    var description = embed.Description;
                    var searchBy = "Player Ranking: #";
                    description = description.Substring(description.IndexOf(searchBy) + searchBy.Length);
                    description = description.Substring(0, description.IndexOf("\n"));

                    rank = Convert.ToInt32(Regex.Replace(description, "[^0-9]", ""));
                    player.SetRank(rank);

                    //Add "registered" role
                    await ((SocketGuildUser)Context.User).AddRoleAsync(Context.Guild.Roles.FirstOrDefault(x => x.Name.ToLower() == "Season Two Registrant".ToLower()));

                    string reply = $"User `{player.GetDiscordName()}` successfully linked to `{player.GetSteamId()}` with timezone `{timezone}`";
                    if (rank > 0) reply += $" and rank `{rank}`";
                    await ReplyAsync(reply);
                }
                else await ReplyAsync("Waiting for embedded content...");
            }
            else if (new Player(steamId).GetDiscordMention() != user.Mention)
            {
                await ReplyAsync($"That steam account is already linked to `{new Player(steamId).GetDiscordName()}`, message an admin if you *really* need to relink it.");
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

                if (!Song.Exists(songId, parsedDifficulty))
                {
                    if (OstHelper.IsOst(songId))
                    {
                        Song song = new Song(songId, parsedDifficulty);
                        song.SetGameOptions(gameOptions);
                        song.SetPlayerOptions(playerOptions);
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
                                    databaseSong.SetGameOptions(gameOptions);
                                    databaseSong.SetPlayerOptions(playerOptions);
                                    await ReplyAsync($"{songName} doesn't have {parsedDifficulty}, using {nextBestDifficulty} instead.\n" +
                                        $"Added to the song list" +
                                        $"{(gameOptions != GameOptions.None ? $" with game options: ({gameOptions.ToString()})" : "")}" +
                                        $"{(playerOptions != PlayerOptions.None ? $" with player options: ({playerOptions.ToString()})" : "!")}");
                                }
                            }
                            else
                            {
                                var databaseSong = new Song(songId, parsedDifficulty);
                                databaseSong.SetGameOptions(gameOptions);
                                databaseSong.SetPlayerOptions(playerOptions);
                                await ReplyAsync($"{songName} ({parsedDifficulty}) downloaded and added to song list" +
                                    $"{(gameOptions != GameOptions.None ? $" with game options: ({gameOptions.ToString()})" : "")}" +
                                    $"{(playerOptions != PlayerOptions.None ? $" with player options: ({playerOptions.ToString()})" : "!")}");
                            }
                        }
                        else await ReplyAsync("Could not download song.");
                    }
                }
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
                MarkAllOld();
                await ReplyAsync("All songs and scores are marked as Old. You may now add new songs.");
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
        public async Task CrunchRaritiesAsync()
        {
            ulong moderatorRoleId = Context.Guild.Roles.FirstOrDefault(x => x.Name.ToLower() == "moderator").Id;
            bool isAdmin =
                ((IGuildUser)Context.User).GetPermissions((IGuildChannel)Context.Channel).Has(ChannelPermission.ManageChannel) ||
                ((IGuildUser)Context.User).RoleIds.Any(x => x == moderatorRoleId);

            if (isAdmin)
            {
                var players = GetAllPlayers()
                    .Where(x => x.GetRarity() == -1 && Team.GetByDiscordMentionOfCaptain(x.GetDiscordMention()) == null)
                    .OrderBy(x => x.GetRank())
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
                    x.SetRarity((int)currentRarity);

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
                    reply += $"{x.GetDiscordName()} ({x.GetTimezone()}) - {(Rarity)x.GetRarity()}\n";

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
        public async Task CreateTeamAsync(string teamId, string name = "Default Team Name", string color = "#ffffff")
        {
            teamId = teamId.ToLower();
            ulong moderatorRoleId = Context.Guild.Roles.FirstOrDefault(x => x.Name.ToLower() == "moderator").Id;
            bool isAdmin =
                ((IGuildUser)Context.User).GetPermissions((IGuildChannel)Context.Channel).Has(ChannelPermission.ManageChannel) ||
                ((IGuildUser)Context.User).RoleIds.Any(x => x == moderatorRoleId);

            if (isAdmin)
            {
                if (!Team.Exists(teamId))
                {
                    AddTeam(teamId, name, "", color, 0);

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
        public async Task ModifyTeamAsync(string name, string color, string teamId = null)
        {
            if (teamId == null)
            {
                teamId = Team.GetByDiscordMentionOfCaptain(Context.User.Mention)?.GetTeamId();
                if (teamId == null)
                {
                    await ReplyAsync("You must provide the TeamId at the end of the command, since you are not the captian of any team");
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
                string captain = currentTeam.GetCaptain();
                bool isCaptain = string.IsNullOrEmpty(captain) ? true : captain == Player.GetByDiscordMetion(Context.User.Mention).GetSteamId(); //In the case of teams, team captains count as admins

                if (isAdmin || isCaptain)
                {
                    //If the provided color can be parsed into a uint, we can use the provided color as the color for the dicsord role
                    Color discordColor = Color.Blue;
                    if (uint.TryParse(color.Substring(1), NumberStyles.HexNumber, CultureInfo.InvariantCulture, out var discordColorUint)) discordColor = new Color(discordColorUint);

                    if (currentRoles.Any(x => Regex.Replace(x.Name.ToLower(), "[^a-z0-9 ]", "") == Regex.Replace(name.ToLower(), "[^a-z0-9 ]", "")))
                    {
                        await ReplyAsync("A role with that name already exists. Please use a different name");
                        return;
                    }

                    currentRoles.FirstOrDefault(x => Regex.Replace(x.Name.ToLower(), "[^a-z0-9 ]", "") == currentTeam.GetTeamName().ToLower())?
                        .ModifyAsync(x =>
                            {
                                x.Name = name;
                                x.Color = discordColor;
                            }
                        );

                    currentTeam.SetColor(color);
                    currentTeam.SetTeamName(name);

                    await ReplyAsync($"Team with id `{teamId}` now has name `{name}` and color `{color}`");
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
                teamId = Team.GetByDiscordMentionOfCaptain(Context.User.Mention)?.GetTeamId();
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
                string currentCaptain = currentTeam.GetCaptain();
                bool isCaptain = string.IsNullOrEmpty(currentCaptain) ? true : currentCaptain == Player.GetByDiscordMetion(Context.User.Mention).GetSteamId(); //In the case of teams, team captains count as admins

                if (isAdmin || isCaptain)
                {
                    Player player = Player.GetByDiscordMetion(user.Mention);
                    if (player == null) await ReplyAsync("That user has not registered with the bot yet");
                    else if ((isCaptain && !isAdmin) && player.GetTeam() != "-1") await ReplyAsync("This person is already on a team");
                    else CommunityBot.ChangeTeam(player, currentTeam, setToCaptain);
                }
                else await ReplyAsync("You are not authorized to assign that role");
            }
            else await ReplyAsync("Team does not exist");
        }

        //A certifiable mess.
        [Command("leaderboards")]
        public async Task LeaderboardsAsync()
        {
            ulong moderatorRoleId = Context.Guild.Roles.FirstOrDefault(x => x.Name.ToLower() == "moderator").Id;
            bool isAdmin =
                ((IGuildUser)Context.User).GetPermissions((IGuildChannel)Context.Channel).Has(ChannelPermission.ManageChannel) ||
                ((IGuildUser)Context.User).RoleIds.Any(x => x == moderatorRoleId);
            if (!isAdmin) return;

            string finalMessage = "Leaderboard:\n\n";
            List<SongConstruct> songs = GetActiveSongs();

            Dictionary<SongConstruct, IDictionary<string, ScoreConstruct>> scores = new Dictionary<SongConstruct, IDictionary<string, ScoreConstruct>>();
            songs.ForEach(x => {
                string songId = x.SongId;
                scores.Add(x, GetScoresForSong(x));
            });

            songs.ForEach(x =>
            {
                string songId = x.SongId;

                if (scores[x].Count > 0) //Don't print if no one submitted scores
                {
                    var song = new Song(songId, x.Difficulty);
                    finalMessage += song.GetSongName() + ":\n";

                    foreach (KeyValuePair<string, ScoreConstruct> item in scores[x])
                    {
                        //TODO: This means the info file is read *EVERY LOOP*. Super inefficient.
                        //It's left this way because there seems to be no way to determine the difficulty in a higher
                        //scope. This is something to look into later.
                        var percentage = ((double)item.Value.Score / (double)new BeatSaver.Song(songId).GetMaxScore(item.Value.Difficulty)).ToString("P", CultureInfo.InvariantCulture);
                        finalMessage += new Player(item.Key).GetDiscordName() + " - " + item.Value.Score + $" ({percentage})" + (item.Value.FullCombo ? " (Full Combo)" : "") + "\n";
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
                finalMessage += $"{x.GetTeamId()}: {x.GetTeamName()} | {new Player(x.GetCaptain()).GetDiscordName()}\n";
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
                "addSong [beatsaver url] <difficulty> (<difficulty> can be either a number or whole-word, such as 4 or ExpertPlus)\n" +
                "endEvent\n" +
                "rarity [rarity] <@User> (assigns a rarity to a user. Likely to remain unused)\n" +
                "crunchRarities (looks at the ranks of all registered players and assigns rarities accordingly)\n" +
                "createTeam [teamId] <name> <color> (teamId should be in the form of teamX, where X is the next teamId number in line, name must be no-spaces, and color must start with #)\n" +
                "modifyTeam [name] [color] <teamId> (admins can override the teamId they're changing)\n" +
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
