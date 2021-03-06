﻿/*
 * Created by Moon on 9/15/2018
 * Holds simple static variables and constructs needed by both sides of the plugin
 */

using System;
using System.Collections.Generic;

namespace EventShared
{
    public static class SharedConstructs
    {
        public const string Name = "EventPlugin";
        public const string Version = "0.4.6";
        public const int VersionCode = 046;
        public static string Changelog =
            "0.0.1: First attempt at fork from EventPlugin\n" +
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
            "0.1.3: First beta for unified event plugin\n" +
            "0.1.4: Sabotage update!\n" +
            "0.1.5: Updated to BS 0.13.1\n" +
            "0.1.6: Floor is Lava! Updated to BS 0.13.2\n" +
            "0.1.7: Removed EventShared dependency from Plugin\n" +
            "0.1.8: Added NoFail toggle for Lava event\n" +
            "0.2.0: Changed to custom serialization, removed protobuf dependency\n" +
            "0.2.1: Comply with songloader 'requirements'\n" +
            "0.2.2: Removed rarity from database/api\n" +
            "0.2.3: Updated for Beat Saber 1.0.0\n" +
            "0.3.0: Updated for Beat Saber 1.1.0 (OTSs only)\n" +
            "0.3.1: Updated for Beat Saber 1.2.0\n" +
            "0.3.2: Added new songs since OST Vol 2\n" +
            "0.3.3: Updated for Beat Saber 1.3.0\n" +
            "0.3.4: Updated for Beat Saber 1.6.0\n" +
            "0.3.5: Updated for Beat Saber 1.6.2\n" +
            "0.3.6: Disable scoresaber submission\n" +
            "0.3.7: Updated for Beat Saber 1.9.0\n" +
            "0.3.8: Added new song packs\n" +
            "0.4.0: Updated characteristic handling\n" +
            "0.4.5: Major bug fixes, configurations for other tournaments\n" +
            "0.4.6: Added log which records score and signature for a failure fallback\n";

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
        public enum ServerFlags
        {
            None = 0,
            Teams = 1,
            Tokens = 2,
        }
    }
}
