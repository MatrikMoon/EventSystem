using Discord;
using Discord.Commands;
using DiscordCommunityServer.Database;
using DiscordCommunityServer.Discord.Services;
using DiscordCommunityShared;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using static DiscordCommunityServer.Database.SimpleSql;
using static DiscordCommunityShared.SharedConstructs;

namespace DiscordCommunityServer.Discord.Modules
{
    // Modules must be public and inherit from an IModuleBase
    public class PublicModule : ModuleBase<SocketCommandContext>
    {
        // Dependency Injection will fill this value in for us
        public PictureService PictureService { get; set; }

        private static int[] _saberRankValues = { 0, 1, 2, 3, 4, 5 };
        private static string[] _saberRoles = { "white", "bronze", "silver", "gold", "blue", "master" };
        Rank[] _ranksToList = { Rank.Master, Rank.Blue, Rank.Gold,
                                            Rank.Silver, Rank.Bronze };

        [Command("register")]
        public async Task RegisterAsync([Remainder] string steamId)
        {
            //Sanitize input
            steamId = Regex.Replace(steamId, "[^0-9]", "");

            if (!Player.Exists(steamId) || !Player.IsRegistered(steamId))
            {
                Player player = new Player(steamId);
                player.SetDiscordName(Context.User.Username);
                player.SetDiscordExtension(Context.User.Discriminator);
                player.SetDiscordMention(Context.User.Mention);

                var guildRoles = Context.Guild.Roles.ToList();

                ulong[] discordRoleIds = guildRoles
                    .Where(x => _saberRoles.Contains(x.Name.ToLower()))
                    .Select(x => x.Id)
                    .ToArray();

                ulong saberRole = ((IGuildUser)Context.User).RoleIds.ToList().Where(x => discordRoleIds.Contains(x)).FirstOrDefault();
                string userRoleText = guildRoles.Where(x => x.Id == saberRole).Select(x => x.Name.ToLower()).FirstOrDefault();
                if (saberRole > 0)
                {
                    player.SetRank(_saberRankValues[_saberRoles.ToList().IndexOf(userRoleText)]);
                }

                string reply = $"User `{player.GetDiscordName()}` successfully linked to `{player.GetSteamId()}`";
                if (saberRole > 0) reply += $" with role `{userRoleText}`";
                else reply += ". Please set your rank to Bronze or Silver using the `@Weekly Event Bot role [bronze/silver]` command, or have an admin set it higher.";
                await ReplyAsync(reply);
            }
            else
            {
                await ReplyAsync($"That steam account is already linked to `{new Player(steamId).GetDiscordName()}`, message an admin if you *really* need to relink it.");
            }
        }

        [Command("addSong")]
        [RequireUserPermission(ChannelPermission.ManageChannel)]
        public async Task AddSongAsync(string songId, string gameplayMode = null)
        {
            int mode = gameplayMode == null ? (int)GameplayMode.SoloStandard : Convert.ToInt32(gameplayMode);
            if (!Song.Exists(songId, mode))
            {
                if (OstHelper.IsOst(songId))
                {
                    await ReplyAsync($"Added: {new Song(songId, mode).GetSongName()}");
                }
                else
                {
                    await ReplyAsync("Downloading song...");
                    string songPath = BeatSaver.BeatSaverDownloader.DownloadSong(songId);
                    if (songPath != null)
                    {
                        string songName = new BeatSaver.Song(songId).SongName;
                        new Song(songId, mode);
                        await ReplyAsync($"{songName} downloaded and added to song list!");
                    }
                    else await ReplyAsync("Could not download song.");
                }
            }
            else await ReplyAsync("The song is already in the database");
        }

        [Command("endEvent")]
        [RequireUserPermission(ChannelPermission.ManageChannel)]
        public async Task EndEventAsync()
        {
            MarkAllOld();
            await ReplyAsync("All songs and scores are marked as Old. You may now add new songs.");
        }

        [Command("role")]
        [RequireBotPermission(GuildPermission.ManageRoles)]
        public async Task RoleAsync(string role, IGuildUser user = null)
        {
            role = role.ToLower();
            ulong weeklyEventManagerRoleId = Context.Guild.Roles.FirstOrDefault(x => x.Name.ToLower() == "weekly event manager").Id;
            bool isAdmin =
                ((IGuildUser)Context.User).GetPermissions((IGuildChannel)Context.Channel).Has(ChannelPermission.ManageChannel) ||
                ((IGuildUser)Context.User).RoleIds.Any(x => x == weeklyEventManagerRoleId);
            if (isAdmin) user = user ?? (IGuildUser)Context.User; //Admins can assign roles for others
            else user = (IGuildUser)Context.User;

            string[] rolesToRestrict = { "gold", "blue", "purple" };

            if ((isAdmin || !rolesToRestrict.Contains(role)) && _saberRoles.Contains(role))
            {
                Player player = Player.GetByDiscord(user.Mention);
                if (player == null)
                {
                    await ReplyAsync("That user has not registered with the bot yet");
                }
                else
                {
                    Rank newRank = (Rank)_saberRankValues[_saberRoles.ToList().IndexOf(role)];
                    player.SetRank((int)newRank);
                    await user.RemoveRolesAsync(Context.Guild.Roles.Where(x => _saberRoles.Contains(x.Name.ToLower())));
                    await user.AddRoleAsync(Context.Guild.Roles.FirstOrDefault(x => x.Name.ToLower() == role));

                    //Sort out existing scores
                    string steamId = player.GetSteamId();
                    IDictionary<SongConstruct, ScoreConstruct> playerScores = GetScoresForPlayer(steamId);
                    string replaySongs = string.Empty;

                    if (playerScores.Count >= 0)
                    {
                        playerScores.ToList().ForEach(x =>
                        {
                            BeatSaver.Song songData = new BeatSaver.Song(x.Key.SongId);
                            if (songData.GetDifficultyForRank(newRank) != x.Value.Difficulty)
                            {
                                replaySongs += songData.SongName + "\n";
                                ExecuteCommand($"UPDATE scoreTable SET old = 1 WHERE songId=\'{x.Key.SongId}\' AND mode=\'{x.Key.Mode}\' AND steamId=\'{steamId}\'");
                            }
                            else
                            {
                                ExecuteCommand($"UPDATE scoreTable SET rank = {(int)newRank} WHERE songId=\'{x.Key.SongId}\' AND mode=\'{x.Key.Mode}\' AND steamId=\'{steamId}\'");
                            }
                        });
                    }

                    if (replaySongs != string.Empty)
                    {
                        string reply = $"You have been promoted to {newRank}!\n" +
                            "You'll need to re-play the following songs:\n" +
                            replaySongs;
                        await ReplyAsync(reply);
                    }

                    else
                    {
                        await ReplyAsync($"You have been promoted to {newRank}!");
                    }
                }
            }
            else await ReplyAsync("You are not authorized to assign that role");
        }

        //A certifiable mess.
        [Command("leaderboards")]
        public async Task LeaderboardsAsync()
        {
            ulong weeklyEventManagerRoleId = Context.Guild.Roles.FirstOrDefault(x => x.Name.ToLower() == "weekly event manager").Id;
            bool isAdmin =
                ((IGuildUser)Context.User).GetPermissions((IGuildChannel)Context.Channel).Has(ChannelPermission.ManageChannel) ||
                ((IGuildUser)Context.User).RoleIds.Any(x => x == weeklyEventManagerRoleId);
            if (!isAdmin) return;

            string finalMessage = "Weekly Leaderboard:\n\n";
            List<SongConstruct> songs = GetActiveSongs();

            foreach (Rank r in _ranksToList)
            {
                Dictionary<SongConstruct, IDictionary<string, ScoreConstruct>> scores = new Dictionary<SongConstruct, IDictionary<string, ScoreConstruct>>();
                songs.ForEach(x => {
                    string songId = x.SongId;
                    int mode = Convert.ToInt32(x.Mode);
                    scores.Add(x, GetScoresForSong(x, (long)r));
                });

                finalMessage += $"{r}\n\n";
                songs.ForEach(x =>
                {
                    string songId = x.SongId;
                    int mode = Convert.ToInt32(x.Mode);

                    if (scores[x].Count > 0) //Don't print if no one submitted scores
                    {
                        finalMessage += new Song(songId, mode).GetSongName() + (mode != 0 ? " " + ((GameplayMode)mode).ToString() + ":\n" : ":\n");

                        foreach (KeyValuePair<string, ScoreConstruct> item in scores[x])
                        {
                            finalMessage += new Player(item.Key).GetDiscordName() + " - " + item.Value.Score + (item.Value.FullCombo ? " (Full Combo)" : "") + "\n";
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
            ulong weeklyEventManagerRoleId = Context.Guild.Roles.FirstOrDefault(x => x.Name.ToLower() == "weekly event manager").Id;
            bool isAdmin =
                ((IGuildUser)Context.User).GetPermissions((IGuildChannel)Context.Channel).Has(ChannelPermission.ManageChannel) ||
                ((IGuildUser)Context.User).RoleIds.Any(x => x == weeklyEventManagerRoleId);
            if (!isAdmin) return;

            string finalReply = "TOKENS:\n\n";

            finalReply += "Blue:\n";
            foreach (string steamId in Player.GetPlayersInRank((int)Rank.Blue))
            {
                Player player = new Player(steamId);
                int projectedTokens = player.GetProjectedTokens();
                if (projectedTokens > 0) finalReply += $"{player.GetDiscordName()} = {projectedTokens}\n";
            }
            finalReply += "\n";

            finalReply += "Gold:\n";
            foreach (string steamId in Player.GetPlayersInRank((int)Rank.Gold))
            {
                Player player = new Player(steamId);
                int projectedTokens = player.GetProjectedTokens();
                if (projectedTokens > 0) finalReply += $"{player.GetDiscordName()} = {projectedTokens}\n";
            }

            await ReplyAsync(finalReply);
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
    }
}
