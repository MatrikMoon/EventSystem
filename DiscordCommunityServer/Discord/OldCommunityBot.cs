/*
using Discord;
using Discord.WebSocket;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DiscordCommunityServer.Discord
{
    class OldCommunityBot
    {
        public static DiscordSocketClient _client;

        public static void Start()
        {
            //Set up Discord bot
            _client = new DiscordSocketClient();
            _client.Log += LogAsync;
            _client.Ready += ReadyAsync;
            _client.MessageReceived += MessageReceivedAsync;
            _client.LoginAsync(TokenType.Bot, "MzI4Njg0NzcwNTAwMjgwMzIw.DndXug.9sDa8lIpaI8Xl74HPJF8JYZO4Js");
            _client.StartAsync();

        }

        public static void SendToScoreChannel(string message)
        {
            if (_client.ConnectionState == ConnectionState.Connected)
            {
                ((ISocketMessageChannel)_client.GetChannel(488445468141944842))?.SendMessageAsync(message);
            }
        }

        private static Task LogAsync(LogMessage log)
        {
            Console.WriteLine(log.ToString());
            return Task.CompletedTask;
        }

        // The Ready event indicates that the client has opened a
        // connection and it is now safe to access the cache.
        private static Task ReadyAsync()
        {
            Console.WriteLine($"{_client.CurrentUser} is connected!");

            return Task.CompletedTask;
        }

        // This is not the recommmended way to write a bot - consider
        // reading over the Commands Framework sample.
        private static async Task MessageReceivedAsync(SocketMessage message)
        {
            // The bot should never respond to itself.
            if (message.Author.Id == _client.CurrentUser.Id)
                return;

            if (message.Channel.Name == "bot-commands")
            {
                if (message.Content.StartsWith("!register ") && message.Content.Split().Length == 2)
                {
                    string steamId = message.Content.Split()[1];
                    if (!Database.Player.Exists(steamId))
                    {
                        Database.Player player = new Database.Player(steamId);
                        player.SetDiscordName(message.Author.Username);
                        player.SetDiscordExtension(message.Author.Discriminator);
                        player.SetDiscordMention(message.Author.Mention);
                        await message.Channel.SendMessageAsync($"User `{player.GetDiscordName()}` successfully linked to `{player.GetSteamId()}`");
                    }
                    else
                    {
                        await message.Channel.SendMessageAsync($"That steam account is already linked to `{new Database.Player(steamId).GetDiscordName()}`, message an admin if you *really* need to relink it.");
                    }
                }

                else if (message.Content.StartsWith("!addSong ") && message.Content.Split().Length == 2)
                {
                    string songId = message.Content.Split()[1];
                    if (!Database.Song.Exists(songId))
                    {
                        await message.Channel.SendMessageAsync("Downloading song...");
                        string songPath = Misc.BeatSaverDownloader.DownloadSong(songId);
                        if (songPath != null)
                        {
                            string songName = Path.GetFileName(Directory.GetDirectories(songPath).First());
                            new Database.Song(songId);
                            await message.Channel.SendMessageAsync($"{songName} downloaded and added to song list!");
                        }
                        else await message.Channel.SendMessageAsync("Could not download song.");
                    }
                    else await message.Channel.SendMessageAsync("The song is already in the database.");
                }
            }
        }
    }
}
*/