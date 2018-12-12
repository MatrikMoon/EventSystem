using DiscordCommunityShared;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static DiscordCommunityShared.SharedConstructs;

/*
 * Created by Moon on 9/11/2018
 * TODO: Use Properties (get/set) instead of getters and setters
 */

namespace DiscordCommunityServer.Database
{
    public class Item
    {
        public string ItemId { get; }
        public Category Category { get; }

        public Item(string itemId, Category category)
        {
            ItemId = itemId;
            Category = category;
            if (!Exists())
            {
                //Add a placeholder, trigger song download from BeatSaver if it doesn't exist
                SimpleSql.AddItem("", "", "", itemId, category);
                if (category == Category.Map) BeatSaver.BeatSaverDownloader.UpdateSongInfoThreaded(this);
            }
        }

        public string GetItemName()
        {
            return SimpleSql.ExecuteQuery($"SELECT name FROM itemTable WHERE itemId = \'{ItemId}\'", "name").First();
        }

        public bool SetItemName(string name)
        {
            return SimpleSql.ExecuteCommand($"UPDATE itemTable SET name = \'{name}\' WHERE itemId = \'{ItemId}\'") > 1;
        }

        public string GetItemAuthor()
        {
            return SimpleSql.ExecuteQuery($"SELECT author FROM itemTable WHERE itemId = \'{ItemId}\'", "author").First();
        }

        public bool SetItemAuthor(string author)
        {
            return SimpleSql.ExecuteCommand($"UPDATE itemTable SET author = \'{author}\' WHERE itemId = \'{ItemId}\'") > 1;
        }

        public string GetItemSubname()
        {
            return SimpleSql.ExecuteQuery($"SELECT subName FROM itemTable WHERE itemId = \'{ItemId}\'", "subName").First();
        }

        public bool SetItemSubname(string subName)
        {
            return SimpleSql.ExecuteCommand($"UPDATE itemTable SET subName = \'{subName}\' WHERE itemId = \'{ItemId}\'") > 1;
        }

        public bool IsOld()
        {
            return SimpleSql.ExecuteQuery($"SELECT old FROM itemTable WHERE itemId = \'{ItemId}\'", "old").First() == "1";
        }

        public bool Exists()
        {
            return Exists(ItemId);
        }

        public static bool Exists(string itemId)
        {
            return SimpleSql.ExecuteQuery($"SELECT * FROM itemTable WHERE itemId = \'{itemId}\'", "itemId").Any();
        }
    }
}