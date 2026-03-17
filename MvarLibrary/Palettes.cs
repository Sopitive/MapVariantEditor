using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml.Linq;
using System.Text;

namespace MVARStudio.MvarLibrary
{
    public class MapItem
    {
        public string Name { get; set; }
        public string ObjectPath { get; set; }
    }

    public class MapFolder
    {
        public string Name { get; set; }
        public List<MapItem> Items { get; set; } = new List<MapItem>();
    }

    public class MapPalette
    {
        public uint MapId { get; set; }
        public string Name { get; set; }
        public List<MapFolder> Folders { get; set; } = new List<MapFolder>();

        public List<string> GetAllItemNames()
        {
            var list = new List<string>();
            for (int f = 0; f < Folders.Count; f++)
            {
                var folder = Folders[f];
                for (int i = 0; i < folder.Items.Count; i++)
                {
                    list.Add($"{f:D3}:{i:D2} {folder.Name} > {folder.Items[i].Name}");
                }
            }
            return list;
        }

        public (int f, int i) ParseItemString(string s)
        {
            if (string.IsNullOrEmpty(s) || s.Length < 6 || s[3] != ':') return (-1, -1);
            if (int.TryParse(s.Substring(0, 3), out int f) && int.TryParse(s.Substring(4, 2), out int i))
                return (f, i);
            return (-1, -1);
        }

        public string GetFullItemName(int f, int i)
        {
            if (f >= 0 && f < Folders.Count)
            {
                var folder = Folders[f];
                if (i >= 0 && i < folder.Items.Count)
                    return $"{f:D3}:{i:D2} {folder.Name} > {folder.Items[i].Name}";
            }
            return "Unknown";
        }

        public static MapPalette LoadFromXml(uint mapId, string filePath)
        {
            if (!File.Exists(filePath)) return null;

            var palette = new MapPalette { MapId = mapId };
            var doc = XDocument.Load(filePath);
            var root = doc.Root;

            // Follow the logic from maps.js
            // The root contains elements which are palettes (Weapon, Vehicle, etc.)
            // We MUST sort by the index attribute to ensure global folder indices are correct.
            var palettesBlock = root.Elements("element")
                                   .OrderBy(e => int.Parse(e.Attribute("index")?.Value ?? "0"));

            foreach (var pElem in palettesBlock)
            {
                var entriesBlock = pElem.Elements("field").FirstOrDefault(f => f.Attribute("name")?.Value == "entries");
                if (entriesBlock == null) continue;

                var foldersElems = pElem.Elements("element")
                                       .OrderBy(e => int.Parse(e.Attribute("index")?.Value ?? "0"));
                
                foreach (var fElem in foldersElems)
                {
                    var folder = new MapFolder
                    {
                        Name = fElem.Attribute("name")?.Value ?? "Unknown Folder"
                    };

                    var variantsBlock = fElem.Elements("field").FirstOrDefault(f => f.Attribute("name")?.Value == "variants");
                    if (variantsBlock != null)
                    {
                        var itemElems = fElem.Elements("element")
                                             .OrderBy(e => int.Parse(e.Attribute("index")?.Value ?? "0"));
                        
                        foreach (var iElem in itemElems)
                        {
                            var dispNameField = iElem.Elements("field").FirstOrDefault(f => f.Attribute("name")?.Value == "display name");
                            var objectField = iElem.Elements("field").FirstOrDefault(f => f.Attribute("name")?.Value == "object");
                            
                            string iName = iElem.Attribute("name")?.Value;
                            string dName = dispNameField?.Attribute("value")?.Value;

                            string finalName = !string.IsNullOrEmpty(iName) ? iName : (!string.IsNullOrEmpty(dName) ? dName : folder.Name);

                            folder.Items.Add(new MapItem
                            {
                                Name = finalName,
                                ObjectPath = objectField?.Attribute("value")?.Value
                            });
                        }
                    }
                    palette.Folders.Add(folder);
                }
            }

            return palette;
        }

        public string ResolveItemName(int folderIndex, int itemIndex)
        {
            if (folderIndex >= 0 && folderIndex < Folders.Count)
            {
                var folder = Folders[folderIndex];
                if (itemIndex >= 0 && itemIndex < folder.Items.Count)
                {
                    return folder.Items[itemIndex].Name;
                }
                return $"{folder.Name} [I:{itemIndex}]";
            }
            return $"Category {folderIndex} Item {itemIndex}";
        }

    }

    public static class ReachMaps
    {
        public class MapInfo
        {
            public uint MapId { get; set; }
            public string Name { get; set; }
            public string InternalName { get; set; }
        }

        public static readonly List<MapInfo> Maps = new List<MapInfo>
        {
            new MapInfo { MapId = 3006, Name = "Forge World", InternalName = "forge_halo" },
            new MapInfo { MapId = 1080, Name = "Boneyard", InternalName = "70_boneyard" },
            new MapInfo { MapId = 1040, Name = "Zealot", InternalName = "45_aftship" },
            new MapInfo { MapId = 2004, Name = "Tempest", InternalName = "dlc_medium" },
            new MapInfo { MapId = 2001, Name = "Anchor 9", InternalName = "dlc_slayer" },
            new MapInfo { MapId = 1150, Name = "Reflection", InternalName = "52_ivory_tower" },
            new MapInfo { MapId = 1035, Name = "Boardwalk", InternalName = "50_panopticon" },
            new MapInfo { MapId = 1020, Name = "Countdown", InternalName = "45_launch_station" },
            new MapInfo { MapId = 1055, Name = "Powerhouse", InternalName = "30_settlement" },
            new MapInfo { MapId = 1200, Name = "Spire", InternalName = "35_island" },
            new MapInfo { MapId = 1000, Name = "Sword Base", InternalName = "20_sword_slayer" },
            new MapInfo { MapId = 2002, Name = "Breakpoint", InternalName = "dlc_invasion" },
            new MapInfo { MapId = 1500, Name = "Condemned", InternalName = "condemned" },
            new MapInfo { MapId = 1510, Name = "Highlands", InternalName = "trainingpreserve" },
            new MapInfo { MapId = 10020, Name = "Battle Canyon", InternalName = "cex_beaver_creek" },
            new MapInfo { MapId = 10010, Name = "Penance", InternalName = "cex_damnation" },
            new MapInfo { MapId = 10030, Name = "Ridgeline", InternalName = "cex_timerland" },
            new MapInfo { MapId = 10070, Name = "Solitary", InternalName = "cex_prisoner" },
            new MapInfo { MapId = 10060, Name = "High Noon", InternalName = "cex_hangemhigh" },
            new MapInfo { MapId = 10050, Name = "Breakneck", InternalName = "cex_headlong" },
            // Firefight
            new MapInfo { MapId = 7060, Name = "Beachhead", InternalName = "ff50_park" },
            new MapInfo { MapId = 7110, Name = "Corvette", InternalName = "ff45_corvette" },
            new MapInfo { MapId = 7020, Name = "Courtyard", InternalName = "ff20_courtyard" },
            new MapInfo { MapId = 7130, Name = "Glacier", InternalName = "ff60_icecave" },
            new MapInfo { MapId = 7080, Name = "Holdout", InternalName = "ff70_holdout" },
            new MapInfo { MapId = 7030, Name = "Outpost", InternalName = "ff60_ruins" },
            new MapInfo { MapId = 7000, Name = "Overlook", InternalName = "ff10_prototype" },
            new MapInfo { MapId = 7040, Name = "Waterfront", InternalName = "ff30_waterfront" },
            new MapInfo { MapId = 7500, Name = "Unearthed", InternalName = "ff_unearthed" },
            new MapInfo { MapId = 10080, Name = "Installation 04", InternalName = "cex_ff_halo" },
        };

        public static MapInfo GetMap(uint mapId) => Maps.FirstOrDefault(m => m.MapId == mapId);
    }
}
