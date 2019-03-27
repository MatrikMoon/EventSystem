﻿/*
 * Created by Moon on 9/15/2018
 * Holds simple static variables and constructs needed by both sides of the plugin
 */

using System;

namespace TeamSaberShared
{
    public static class SharedConstructs
    {
        public static string Name => "TeamSaberPlugin";
        public static string Version => "0.1.3";
        public static int VersionCode => 013;
        public static string Changelog =
            "0.0.1: First attempt at fork from DiscordCommunityPlugin\n" +
            "0.0.2: Sample update\n" +
            "0.0.3: Made Teams dynamically loaded from server\n" +
            "0.0.4: Difficulties are now decided by the server, fixed isolation, unified oculus / steam versions\n" +
            "0.0.5: Various UI improvements: Fixed scroll buttons, made plugin button non-interactible until song load, added difficulty text view\n" +
            "0.0.6: Event 2: HoT, again tried to fix unification\n" +
            "0.0.7: Fixed scroll button overlap! Thank almighty andruzzzhka for this miracle\n" +
            "0.0.8: Database changes, update for KotH!\n" +
            "0.0.9: Database changes for OneSaber!\n" +
            "0.1.0: Updated for Beat Saber 0.13.0\n" +
            "0.1.1: TeamSaber speed event!\n" +
            "0.1.2: Speed restart exploit hotfix\n" +
            "0.1.3: First beta for unified event plugin\n";

        public enum Rarity
        {
            None = -1, //Only use as "does not exist"
            C = 0,
            B = 1,
            A = 2,
            S = 3,
            SS = 4,
            SSS = 5,
            All = 6 //Not to be stored. Only use as "no filter necessary"
        }

        public enum LevelDifficulty
        {
            Auto = -1,
            Easy = 0,
            Normal = 1,
            Hard = 2,
            Expert = 3,
            ExpertPlus = 4
        }

        [Flags]
        public enum GameOptions
        {
            None = 0,

            //Negative modifiers
            NoFail = 1,
            NoArrows = 2,
            NoBombs = 4,
            NoObstacles = 8,
            NoWalls = 16,
            SlowSong = 32,

            //Positive Modifiers
            InstaFail = 64,
            FailOnClash = 128,
            BatteryEnergy = 256,
            FastNotes = 512,
            FastSong = 1024,
            DisappearingArrows = 2048,

            //New options
            GhostNotes = 4096
        }

        [Flags]
        public enum PlayerOptions
        {
            None = 0,
            Mirror = 1,
            StaticLights = 2,
            NoHud = 4,
            AdvancedHud = 8,
            ReduceDebris = 16
        }

        [Flags]
        public enum ServerFeatures
        {
            None = 0,
            Teams = 1,
            Tokens = 2,
            AutoPromotion = 4
        }
    }
}