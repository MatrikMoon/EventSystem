﻿using Discord;
using Discord.Commands;
using EventServer.Discord.Services;
using System.IO;
using System.Threading.Tasks;

/**
 * Created by Moon on 5/18/2019
 * A Discord.NET module for image commands
 */

namespace EventServer.Discord.Modules
{
    public class PictureModule : ModuleBase<SocketCommandContext>
    {
        public PictureService PictureService { get; set; }

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

            await ReplyAsync("", false, builder.Build());
        }

        [Command("nekolewdgif")]
        [RequireNsfw]
        public async Task NekoLewdGifAsync()
        {
            // Get a stream containing an image of a cat
            var gifLink = await PictureService.GetNekoLewdGifAsync();

            var builder = new EmbedBuilder();
            builder.WithImageUrl(gifLink);

            await ReplyAsync("", false, builder.Build());
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

            await ReplyAsync("", false, builder.Build());
        }
    }
}
