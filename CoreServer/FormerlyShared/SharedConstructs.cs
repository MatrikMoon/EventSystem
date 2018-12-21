/*
 * Created by Moon on 9/15/2018
 * Holds simple static variables and constructs needed by both sides of the plugin
 */ 

namespace ChristmasShared
{
    public static class SharedConstructs
    {
        public static string Name => "ChristmasVotingPlugin";
        public static string Version => "0.0.1";
        public static int VersionCode => 001;

        public enum Category
        {
            None = -1, //Not to be stored. Only use as "does not exist"
            Map = 0,
            Saber = 1,
            Avatar = 2,
            Platform = 3
        }

        public class TableItem
        {
            public string Name { get; set; }
            public string Author { get; set; }
            public string SubName { get; set; }
            public Category Category { get; set; }
            public string ItemId { get; set; }

            //Necessary overrides for using Contains()
            public static bool operator ==(TableItem a, TableItem b)
            {
                if (b == null) return false;
                return a.GetHashCode() == b.GetHashCode();
            }

            public static bool operator !=(TableItem a, TableItem b)
            {
                if (b == null) return false;
                return a.GetHashCode() != b.GetHashCode();
            }

            public override bool Equals(object obj)
            {
                if (obj == null) return false;
                if (!(obj is TableItem)) return false;
                return GetHashCode() == obj.GetHashCode();
            }

            public override int GetHashCode()
            {
                int hash = 13;
                hash = (hash * 7) + ItemId.GetHashCode();
                return hash;
            }
        }
    }
}
