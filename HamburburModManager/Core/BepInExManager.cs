using System;
using System.IO;
using System.Net.Http;
using System.IO.Compression;
using System.Threading.Tasks;

namespace HamburburModManager.Core;

public static class BepInExManager
{
    private static readonly HttpClient Http = new();

    private const string LatestVersion = "5.4.23.5";
    private const string DownloadUrl   = $"https://github.com/BepInEx/BepInEx/releases/download/v{LatestVersion}/BepInEx_win_x64_{LatestVersion}.zip";
    
    public static bool HasBepInEx(string gamePath)
    {
        if (string.IsNullOrEmpty(gamePath))
            return false;

        string bepInExFolder = Path.Combine(gamePath, "BepInEx");
        return Directory.Exists(bepInExFolder) &&
               File.Exists(Path.Combine(bepInExFolder, "core", "BepInEx.Preloader.dll"));
    }
    
    public static async Task InstallBepInEx(string gamePath)
    {
        if (string.IsNullOrEmpty(gamePath))
            throw new ArgumentNullException(nameof(gamePath));

        string zipPath = Path.Combine(Path.GetTempPath(), $"BepInEx_{LatestVersion}.zip");

        Console.WriteLine("Downloading BepInEx...");

        try
        {
            byte[] data = await Http.GetByteArrayAsync(DownloadUrl);
            await File.WriteAllBytesAsync(zipPath, data);

            Console.WriteLine("Extracting BepInEx...");

            await ZipFile.ExtractToDirectoryAsync(zipPath, gamePath, true);
            Console.WriteLine("BepInEx installed successfully!");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Failed to install BepInEx: {ex.Message}");
        }
        finally
        {
            if (File.Exists(zipPath))
                File.Delete(zipPath);
        }
    }
}