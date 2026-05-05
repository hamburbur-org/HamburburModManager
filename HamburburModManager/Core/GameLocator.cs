using System.Runtime.InteropServices;
using Microsoft.Win32;

namespace HamburburModManager.Core;

public static class GameLocator
{
    private const string RegistryKeyPath   = @"Software\GorillaTagModManager";
    private const string RegistryValueName = "GamePath";

    public static string FindGame()
    {
        string saved = GetSavedPath();

        if (IsValidGamePath(saved))
            return saved;

        bool isLinux = RuntimeInformation.IsOSPlatform(OSPlatform.Linux);

        List<string> commonPaths = [];

        if (isLinux)
        {
            string home = Environment.GetFolderPath(Environment.SpecialFolder.Personal);

            commonPaths.AddRange([
                                         Path.Combine(home, ".steam/steam/steamapps/common/Gorilla Tag"),
                                         Path.Combine(home, ".local/share/Steam/steamapps/common/Gorilla Tag"),
                                         Path.Combine(home, "Steam/steamapps/common/Gorilla Tag"),
                                         Path.Combine(home,
                                                 ".var/app/com.valvesoftware.Steam/.steam/steam/steamapps/common/Gorilla Tag"),
                                 ]);
        }
        else
        {
            commonPaths.AddRange([
                                         @"C:\Program Files (x86)\Steam\steamapps\common\Gorilla Tag",
                                         @"C:\Program Files\Steam\steamapps\common\Gorilla Tag",
                                         @"D:\SteamLibrary\steamapps\common\Gorilla Tag",
                                         @"E:\SteamLibrary\steamapps\common\Gorilla Tag",
                                 ]);
        }

        foreach (string path in commonPaths.Where(IsValidGamePath))
        {
            SavePath(path);

            return path;
        }

        string steamPath = GetSteamPath();

        if (string.IsNullOrEmpty(steamPath))
            return null;

        {
            string[] libs = GetSteamLibraries(steamPath);

            foreach (string lib in libs)
            {
                string path = Path.Combine(lib, "steamapps", "common", "Gorilla Tag");

                if (!IsValidGamePath(path))
                    continue;

                SavePath(path);

                return path;
            }
        }

        return null;
    }

    public static bool IsValidGamePath(string path)
    {
        if (string.IsNullOrEmpty(path))
            return false;

        return Directory.Exists(path) &&
               File.Exists(Path.Combine(path, "Gorilla Tag.exe"));
    }

    public static void SavePath(string path)
    {
        RegistryKey key = Registry.CurrentUser.CreateSubKey(RegistryKeyPath);
        key.SetValue(RegistryValueName, path);
    }

    private static string GetSavedPath()
    {
        RegistryKey key = Registry.CurrentUser.OpenSubKey(RegistryKeyPath);

        return key?.GetValue(RegistryValueName)?.ToString();
    }

    private static string GetSteamPath()
    {
        RegistryKey key = Registry.CurrentUser.OpenSubKey(@"Software\Valve\Steam");

        return key?.GetValue("SteamPath")?.ToString();
    }

    private static string[] GetSteamLibraries(string steamPath)
    {
        List<string> libraries = new() { steamPath, };

        string vdf = Path.Combine(steamPath, "steamapps", "libraryfolders.vdf");

        if (!File.Exists(vdf))
            return libraries.ToArray();

        string[] lines = File.ReadAllLines(vdf);

        libraries.AddRange(from line in lines
                           where line.Contains("path")
                           select line.Split('"')
                           into parts
                           where parts.Length >= 4
                           select parts[3].Replace("\\\\", "\\")
                           into path
                           where Directory.Exists(path)
                           select path);

        return libraries.Distinct().ToArray();
    }
}