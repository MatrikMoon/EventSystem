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

                await ReplyAsync($"User `{player.GetDiscordName()}` successfully linked to `{player.GetPlayerId()}`");
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
            if (!Item.Exists(songId))
            {
                await ReplyAsync("Downloading song...");
                string songPath = BeatSaver.BeatSaverDownloader.DownloadSong(songId);
                if (songPath != null)
                {
                    string songName = new BeatSaver.Song(songId).SongName;
                    new Item(songId, Category.Map);
                    await ReplyAsync($"{songName} downloaded and added to song list!");
                }
                else await ReplyAsync("Could not download song.");
            }
            else await ReplyAsync("The song is already in the database");
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
