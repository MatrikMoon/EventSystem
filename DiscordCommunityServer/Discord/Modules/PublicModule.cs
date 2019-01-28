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
using System.Globalization;

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
                int rank = 0;
                var embed = Context.Message.Embeds.FirstOrDefault();

                if (embed != null)
                {
                    Player player = new Player(steamId);
                    player.SetDiscordName(Context.User.Username);
                    player.SetDiscordExtension(Context.User.Discriminator);
                    player.SetDiscordMention(Context.User.Mention);
                    player.SetTimezone(timezone);

                    //Scrape out the rank data
                    var description = embed.Description;
                    var searchBy = "Player Ranking: #";
                    description = description.Substring(description.IndexOf(searchBy) + searchBy.Length);
                    description = description.Substring(0, description.IndexOf("\n"));

                    rank = Convert.ToInt32(Regex.Replace(description, "[^0-9]", ""));
                    player.SetRank(rank);

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
                else await ReplyAsync("Waiting for embedded content...");
            }
            else if (new Player(steamId).GetDiscordMention() != Context.User.Mention)
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
            ulong moderatorRoleId = Context.Guild.Roles.FirstOrDefault(x => x.Name.ToLower() == "moderator").Id;
            bool isAdmin =
                ((IGuildUser)Context.User).GetPermissions((IGuildChannel)Context.Channel).Has(ChannelPermission.ManageChannel) ||
                ((IGuildUser)Context.User).RoleIds.Any(x => x == moderatorRoleId);
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
                    AddTeam(teamId, name, "", color);
                    await ReplyAsync($"Team created with id `{teamId}`, name `{name}`, and color `{color}`");
                }
                else await ReplyAsync("That team already exists, sorry.");
            }
            else await ReplyAsync("You are not authorized to create teams");
        }

        [Command("modifyTeam")]
        [RequireBotPermission(GuildPermission.ManageRoles)]
        public async Task ModifyTeamAsync(string teamId, string name = "Default Team Name", string color = "#ffffff")
        {
            teamId = teamId.ToLower();
            ulong moderatorRoleId = Context.Guild.Roles.FirstOrDefault(x => x.Name.ToLower() == "moderator").Id;
            bool isAdmin =
                ((IGuildUser)Context.User).GetPermissions((IGuildChannel)Context.Channel).Has(ChannelPermission.ManageChannel) ||
                ((IGuildUser)Context.User).RoleIds.Any(x => x == moderatorRoleId);

            if (Team.Exists(teamId))
            {
                bool isCaptain = new Team(teamId).GetCaptain() == Player.GetByDiscord(Context.User.Mention).GetSteamId(); //In the case of teams, team captains count as admins

                if (isAdmin || isCaptain)
                {
                    AddTeam(teamId, name, "", color);
                    await ReplyAsync($"Team now has id `{teamId}`, name `{name}`, and color `{color}`");
                }
                else await ReplyAsync("You are not authorized to modify this team");
            }
            else await ReplyAsync("That team does not exist, sorry.");
        }

        [Command("assignTeam")]
        [RequireBotPermission(GuildPermission.ManageRoles)]
        public async Task AssignTeamAsync(string teamId, string captain = null, IGuildUser user = null)
        {
            bool setToCaptain = "captain" == captain;
            teamId = teamId.ToLower();
            ulong moderatorRoleId = Context.Guild.Roles.FirstOrDefault(x => x.Name.ToLower() == "moderator").Id;
            bool isAdmin =
                ((IGuildUser)Context.User).GetPermissions((IGuildChannel)Context.Channel).Has(ChannelPermission.ManageChannel) ||
                ((IGuildUser)Context.User).RoleIds.Any(x => x == moderatorRoleId);

            user = user ?? (IGuildUser)Context.User; //Anyone who can assign teams can assign roles for others

            if (Team.Exists(teamId)) {
                bool isCaptain = new Team(teamId).GetCaptain() == Player.GetByDiscord(Context.User.Mention).GetSteamId(); //In the case of teams, team captains count as admins

                if (isAdmin || isCaptain)
                {
                    Player player = Player.GetByDiscord(user.Mention);
                    if (player == null) await ReplyAsync("That user has not registered with the bot yet");
                    else if ((isCaptain && !isAdmin) && player.GetTeam() != "-1") await ReplyAsync("This person is already on a team");
                    CommunityBot.ChangeTeam(player, new Team(teamId), setToCaptain);
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
                    var song = new Song(songId);
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
