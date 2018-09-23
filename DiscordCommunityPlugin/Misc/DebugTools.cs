﻿using DiscordCommunityShared;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using Logger = DiscordCommunityShared.Logger;
using Object = UnityEngine.Object;

/*
 * Created by Moon on 9/23/2018
 * This class is intended to contain tools useful for reverse enginnering
 * Beat Saber. This was created due to the need to display a GameObject Hierarchy
 * to find the views which are necessary to destroy for the Leaderboards
 */

namespace DiscordCommunityPlugin.Misc
{
    class DebugTools
    {
        public static void PrintObjectHierarchy()
        {
            Logger.Success("BEGINNING TREE");
            foreach (GameObject obj in Object.FindObjectsOfType(typeof(GameObject)))
            {
                if (obj.transform.parent == null)
                {
                    Logger.Warning($"NEW TREE ROOT: {obj.name}");
                    Traverse(obj);
                }
            }
        }

        public static void Traverse(GameObject obj, string history = null)
        {
            Logger.Info($"BRANCH: {history}/{obj.name}");
            foreach (Transform child in obj.transform)
            {
                Traverse(child.gameObject, history + $"/{obj.name}");
            }
        }
    }
}
