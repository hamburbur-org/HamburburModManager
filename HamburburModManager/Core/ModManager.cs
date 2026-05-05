using System;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;
using HamburburModManager.Models;

namespace HamburburModManager.Core;

public static class ModManager
{
    private static readonly HttpClient Http = new();

    /// <summary>
    ///     Installs or updates a mod in the plugins folder.
    /// </summary>
    /// <param name="mod">The mod info</param>
    /// <param name="pluginsFolder">Full path to BepInEx/plugins</param>
    public static async Task InstallOrUpdate(ModInfo mod, string pluginsFolder)
    {
        if (!Directory.Exists(pluginsFolder))
            Directory.CreateDirectory(pluginsFolder);

        string modFolder = Path.Combine(pluginsFolder, mod.Name);

        if (!Directory.Exists(modFolder))
            Directory.CreateDirectory(modFolder);

        string dllPath = Path.Combine(modFolder, mod.FileName);

        try
        {
            byte[] data = await Http.GetByteArrayAsync(mod.DownloadUrl);

            await File.WriteAllBytesAsync(dllPath, data);
            Console.WriteLine($"Installed/Updated {mod.Name} to {dllPath}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Failed to install {mod.Name}: {ex.Message}");
        }
    }
}