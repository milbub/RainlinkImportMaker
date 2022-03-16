using System;
using System.Collections.Generic;
using System.Linq;
using SQLite;

namespace RainlinkParser
{
    public static class LinksLoader
    {
        private const string Path = "Links.db";

        private static readonly SQLiteConnection _dbLinks;

        static LinksLoader()
        {
            _dbLinks = new SQLiteConnection(Path);
            _ = _dbLinks.CreateTable<MwLink>();
        }

        public static List<MwUnit> LoadSelectedLinks(Dictionary<int, byte> MwsToLoad)
        {
            List<MwUnit> LoadedUnits = new List<MwUnit>();
            
            int[] keys = MwsToLoad.Keys.ToArray();
            TableQuery<MwLink> query = _dbLinks.Table<MwLink>().Where(v => keys.Contains(v.Id));

            foreach (MwLink link in query)
            {
                switch (MwsToLoad[link.Id])
                {
                    case 1:
                        MwUnit unitA = new MwUnit(link.IpA, link.IpB, link);
                        LoadedUnits.Add(unitA);
                        break;
                    case 2:
                        MwUnit unitB = new MwUnit(link.IpB, link.IpA, link);
                        LoadedUnits.Add(unitB);
                        break;
                    case 3:
                        MwUnit unitAA = new MwUnit(link.IpA, link.IpB, link);
                        LoadedUnits.Add(unitAA);
                        MwUnit unitBB = new MwUnit(link.IpB, link.IpA, link);
                        LoadedUnits.Add(unitBB);
                        break;
                    default:
                        break;
                }
            }

            return LoadedUnits;
        }
    }
}
