using Discord;
using Discord.Commands;
using DiscordCommunityServer.Database;
using DiscordCommunityServer.Discord.Services;
using DiscordCommunityShared;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using static DiscordCommunityShared.SharedConstructs;

namespace DiscordCommunityServer.Discord.Modules
{
    // Modules must be public and inherit from an IModuleBase
    public class PublicModule : ModuleBase<SocketCommandContext>
    {
        // Dependency Injection will fill this value in for us
        public PictureService PictureService { get; set; }

        private static int[] _saberRankValues = { 0, 1, 2, 3, 4, 5 };
        private static string[] _saberRoles = { "white", "bronze", "silver", "gold", "blue", "purple" };

        [Command("register")]
        public async Task RegisterAsync([Remainder] string steamId)
        {
            if (!Player.Exists(steamId) || !Player.IsRegistered(steamId))
            {
                Player player = new Player(steamId);
                player.SetDiscordName(Context.User.Username);
                player.SetDiscordExtension(Context.User.Discriminator);
                player.SetDiscordMention(Context.User.Mention);

                ulong[] discordRoleIds = 
                    Context.Guild.Roles.ToList()
                    .Where(x => _saberRoles.Contains(x.Name))
                    .Select(x => x.Id)
                    .ToArray();

                ulong saberRole = ((IGuildUser)Context.User).RoleIds.ToList().Where(x => discordRoleIds.Contains(x)).FirstOrDefault();
                if (saberRole >= 0)
                {
                    player.SetRank(_saberRankValues[discordRoleIds.ToList().IndexOf(saberRole)]);
                }
                string reply = $"User `{player.GetDiscordName()}` successfully linked to `{player.GetSteamId()}`";
                if (saberRole >= 0) reply += $" with role `{_saberRoles[discordRoleIds.ToList().IndexOf(saberRole)]}`";
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
                if (OstHashToSongName.IsOst(songId))
                {
                    await ReplyAsync($"Added: {new Song(songId, mode).GetSongName()}");
                }
                else
                {
                    await ReplyAsync("Downloading song...");
                    string songPath = Misc.BeatSaverDownloader.DownloadSong(songId);
                    if (songPath != null)
                    {
                        string songName = Path.GetFileName(Directory.GetDirectories(songPath).First());
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
            SimpleSql.MarkAllOld();
            await ReplyAsync("All songs and scores are marked as Old. You may now add new songs.");
        }

        [Command("role")]
        [RequireBotPermission(GuildPermission.ManageRoles)]
        public async Task RoleAsync(string role, IGuildUser user = null)
        {
            role = role.ToLower();
            bool isAdmin = (((IGuildUser)Context.User).GetPermissions((IGuildChannel)Context.Channel).Has(ChannelPermission.ManageChannel));
            if (isAdmin) user = user ?? (IGuildUser)Context.User; //Admins can assign roles for others
            else user = (IGuildUser)Context.User;

            await user.RemoveRolesAsync(Context.Guild.Roles.Where(x => _saberRoles.Contains(x.Name.ToLower())));

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
                    player.SetRank(_saberRankValues[_saberRoles.ToList().IndexOf(role)]);
                    await user.AddRoleAsync(Context.Guild.Roles.FirstOrDefault(x => x.Name.ToLower() == role));
                }
            }
            else await ReplyAsync("You are not authorized to assign that role");
        }

        //A certifiable mess.
        [Command("leaderboards")]
        [RequireUserPermission(ChannelPermission.ManageChannel)]
        public async Task LeaderboardsAsync()
        {
            string finalMessage = "Weekly Leaderboard:\n\n";
            List<Dictionary<string, string>> songIds = SimpleSql.ExecuteQuery("SELECT songId, mode FROM songTable WHERE NOT old = 1", "songId", "mode");

            Rank[] ranksToList = { Rank.Purple, Rank.Blue, Rank.Gold,
                                            Rank.Silver, Rank.Bronze };

            foreach (Rank r in ranksToList)
            {
                Dictionary<string, IDictionary<string, long>> scores = new Dictionary<string, IDictionary<string, long>>();
                songIds.ForEach(x => {
                    string songId = x["songId"];
                    int mode = Convert.ToInt32(x["mode"]);
                    scores.Add(songId + mode, SimpleSql.GetScoresForSong(songId, mode, (long)r));
                });

                finalMessage += $"{r}\n\n";
                songIds.ForEach(x =>
                {
                    string songId = x["songId"];
                    int mode = Convert.ToInt32(x["mode"]);

                    if (scores[songId + mode].Count > 0) //Don't print if no one submitted scores
                    {
                        finalMessage += new Song(songId, mode).GetSongName() + (mode != 0 ? " " + ((GameplayMode)mode).ToString() + ":\n" : ":\n");

                        foreach (KeyValuePair<string, long> item in scores[songId + mode])
                        {
                            finalMessage += new Player(item.Key).GetDiscordName() + " - " + item.Value + "\n";
                        }
                        finalMessage += "\n";
                    }
                });
            }
            await ReplyAsync(finalMessage);
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
