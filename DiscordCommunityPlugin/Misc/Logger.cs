﻿using System;

namespace DiscordCommunityPlugin
{
    class Logger
    {
        private const string prefix = "[DiscordCommunityPlugin]: ";

        public static void Error(string message)
        {
            ConsoleColor originalColor = Console.ForegroundColor;
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine(prefix + message);
            Console.ForegroundColor = originalColor;
        }

        public static void Warning(string message)
        {
            ConsoleColor originalColor = Console.ForegroundColor;
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine(prefix + message);
            Console.ForegroundColor = originalColor;
        }

        public static void Info(string message)
        {
            ConsoleColor originalColor = Console.ForegroundColor;
            Console.ForegroundColor = ConsoleColor.White;
            Console.WriteLine(prefix + message);
            Console.ForegroundColor = originalColor;
        }
    }
}