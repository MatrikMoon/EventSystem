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

namespace TeamSaberServer.Discord.Modules
{
    // Modules must be public and inherit from an IModuleBase
    public class PublicModule : ModuleBase<SocketCommandContext>
    {
        // Dependency Injection will fill this value in for us
        public PictureService PictureService { get; set; }

        [Command("register")]
        public async Task RegisterAsync(string steamId, string timezone)
        {
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
                Player player = new Player(steamId);
                player.SetDiscordName(Context.User.Username);
                player.SetDiscordExtension(Context.User.Discriminator);
                player.SetDiscordMention(Context.User.Mention);
                player.SetTimezone(timezone);

                int rank = 0;

                var embed = Context.Message.Embeds.FirstOrDefault();

                if (embed != null)
                {
                    //Scrape out the rank data
                    var description = embed.Description;
                    var searchBy = "Player Ranking: #";
                    description = description.Substring(description.IndexOf(searchBy) + searchBy.Length);
                    description = description.Substring(0, description.IndexOf("\n"));

                    rank = Convert.ToInt32(Regex.Replace(description, "[^0-9]", ""));
                    player.SetRank(rank);
                }
                
                var guildRoles = Context.Guild.Roles.ToList();

                ulong[] discordRoleIds = guildRoles
                    .Where(x => CommunityBot._rarityRoles.Contains(x.Name.ToLower()))
                    .Select(x => x.Id)
                    .ToArray();

                ulong saberRole = ((IGuildUser)Context.User).RoleIds.ToList().Where(x => discordRoleIds.Contains(x)).FirstOrDefault();
                string userRoleText = guildRoles.Where(x => x.Id == saberRole).Select(x => x.Name.ToLower()).FirstOrDefault();
                if (saberRole > 0)
                {
                    player.SetRarity(CommunityBot._saberRankValues[CommunityBot._rarityRoles.ToList().IndexOf(userRoleText)]);
                }

                string reply = $"User `{player.GetDiscordName()}` successfully linked to `{player.GetSteamId()}` with timezone `{timezone}`";
                if (saberRole > 0) reply += $" and rarity `{userRoleText}`";
                if (rank > 0) reply += $" and rank `{rank}`";
                await ReplyAsync(reply);
            }
            else
            {
                await ReplyAsync($"That steam account is already linked to `{new Player(steamId).GetDiscordName()}`, message an admin if you *really* need to relink it.");
            }
        }

        [Command("addSong")]
        [RequireUserPermission(ChannelPermission.ManageChannel)]
        public async Task AddSongAsync(string songId)
        {
            if (!Song.Exists(songId))
            {
                if (OstHelper.IsOst(songId))
                {
                    await ReplyAsync($"Added: {new Song(songId).GetSongName()}");
                }
                else
                {
                    await ReplyAsync("Downloading song...");
                    string songPath = BeatSaver.BeatSaverDownloader.DownloadSong(songId);
                    if (songPath != null)
                    {
                        string songName = new BeatSaver.Song(songId).SongName;
                        new Song(songId);
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

        [Command("rarity")]
        [RequireBotPermission(GuildPermission.ManageRoles)]
        public async Task RarityAsync(string role, IGuildUser user = null)
        {
            role = role.ToLower();
            ulong weeklyEventManagerRoleId = Context.Guild.Roles.FirstOrDefault(x => x.Name.ToLower() == "weekly event manager").Id;
            bool isAdmin =
                ((IGuildUser)Context.User).GetPermissions((IGuildChannel)Context.Channel).Has(ChannelPermission.ManageChannel) ||
                ((IGuildUser)Context.User).RoleIds.Any(x => x == weeklyEventManagerRoleId);
            if (isAdmin) user = user ?? (IGuildUser)Context.User; //Admins can assign roles for others
            else user = (IGuildUser)Context.User;

            if ((isAdmin) && CommunityBot._rarityRoles.Contains(role))
            {
                Player player = Player.GetByDiscord(user.Mention);
                if (player == null) await ReplyAsync("That user has not registered with the bot yet");
                else if (Enum.TryParse(char.ToUpper(role[0]) + role.Substring(1), out Rarity rarity)) CommunityBot.ChangeRarity(player, rarity);
                else await ReplyAsync("Role parse failed");
            }
            else await ReplyAsync("You are not authorized to assign that role");
        }

        [Command("team")]
        [RequireBotPermission(GuildPermission.ManageRoles)]
        public async Task TeamAsync(string teamId, IGuildUser user = null)
        {
            teamId = teamId.ToLower();
            ulong weeklyEventManagerRoleId = Context.Guild.Roles.FirstOrDefault(x => x.Name.ToLower() == "weekly event manager").Id;
            bool isAdmin =
                ((IGuildUser)Context.User).GetPermissions((IGuildChannel)Context.Channel).Has(ChannelPermission.ManageChannel) ||
                ((IGuildUser)Context.User).RoleIds.Any(x => x == weeklyEventManagerRoleId);
            if (isAdmin) user = user ?? (IGuildUser)Context.User; //Admins can assign roles for others
            else user = (IGuildUser)Context.User;

            if ((isAdmin))
            {
                Player player = Player.GetByDiscord(user.Mention);
                if (player == null) await ReplyAsync("That user has not registered with the bot yet");
                else if (Team.Exists(teamId)) CommunityBot.ChangeTeam(player, new Team(teamId));
                else await ReplyAsync("Team parse failed");
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

            string finalMessage = "Leaderboard:\n\n";
            List<SongConstruct> songs = GetActiveSongs();

            foreach (Rarity r in CommunityBot._rarityToList)
            {
                Dictionary<SongConstruct, IDictionary<string, ScoreConstruct>> scores = new Dictionary<SongConstruct, IDictionary<string, ScoreConstruct>>();
                songs.ForEach(x => {
                    string songId = x.SongId;
                    scores.Add(x, GetScoresForSong(x, (long)r));
                });

                finalMessage += $"{r}\n\n";
                songs.ForEach(x =>
                {
                    string songId = x.SongId;

                    if (scores[x].Count > 0) //Don't print if no one submitted scores
                    {
                        finalMessage += new Song(songId).GetSongName() + ":\n";

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
