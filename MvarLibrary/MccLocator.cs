using System;
using System.Collections.Generic;
using System.IO;
using Microsoft.Win32;
using System.Text.RegularExpressions;

namespace MVARStudio.MvarLibrary
{
    public static class MccLocator
    {
        private static string _cachedPath = null;

        public static string FindMccPath()
        {
            if (_cachedPath != null) return _cachedPath;

            // 1. Try Registry
            string regPath = GetPathFromRegistry();
            if (!string.IsNullOrEmpty(regPath) && Directory.Exists(Path.Combine(regPath, "haloreach")))
            {
                _cachedPath = regPath;
                return regPath;
            }

            // 2. Try Steam Library Folders
            string steamPath = GetSteamInstallPath();
            if (!string.IsNullOrEmpty(steamPath))
            {
                var libraries = GetSteamLibraries(steamPath);
                foreach (var lib in libraries)
                {
                    string mccPath = Path.Combine(lib, "steamapps", "common", "Halo The Master Chief Collection");
                    if (Directory.Exists(Path.Combine(mccPath, "haloreach")))
                        return mccPath;
                }
            }

            // 3. Fallback to common locations
            string[] commonPaths = {
                @"C:\Program Files (x86)\Steam\steamapps\common\Halo The Master Chief Collection",
                @"D:\SteamLibrary\steamapps\common\Halo The Master Chief Collection",
                @"E:\SteamLibrary\steamapps\common\Halo The Master Chief Collection",
                @"F:\SteamLibrary\steamapps\common\Halo The Master Chief Collection"
            };

            foreach (var path in commonPaths)
            {
                if (Directory.Exists(Path.Combine(path, "haloreach")))
                    return path;
            }

            return null;
        }

        private static string GetPathFromRegistry()
        {
            try
            {
                using (var key = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall\Steam App 976730"))
                {
                    return key?.GetValue("InstallLocation") as string;
                }
            }
            catch { return null; }
        }

        private static string GetSteamInstallPath()
        {
            try
            {
                using (var key = Registry.CurrentUser.OpenSubKey(@"Software\Valve\Steam"))
                {
                    return key?.GetValue("SteamPath") as string;
                }
            }
            catch { return null; }
        }

        private static List<string> GetSteamLibraries(string steamPath)
        {
            var libs = new List<string> { steamPath };
            string vdfPath = Path.Combine(steamPath, "steamapps", "libraryfolders.vdf");
            if (File.Exists(vdfPath))
            {
                try
                {
                    string content = File.ReadAllText(vdfPath);
                    // Extremely basic regex to find "path" "..."
                    var matches = Regex.Matches(content, @"""path""\s+""([^""]+)""");
                    foreach (Match m in matches)
                    {
                        string path = m.Groups[1].Value.Replace(@"\\", @"\");
                        if (!libs.Contains(path)) libs.Add(path);
                    }
                }
                catch { }
            }
            return libs;
        }

        public static string GetMapFilePath(string mccPath, string internalMapName)
        {
            if (string.IsNullOrEmpty(mccPath)) return null;
            string mapPath = Path.Combine(mccPath, "haloreach", "maps", internalMapName + ".map");
            return File.Exists(mapPath) ? mapPath : null;
        }
    }
}
