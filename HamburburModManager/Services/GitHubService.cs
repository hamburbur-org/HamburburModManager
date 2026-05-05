using System.Text.Json;
using HamburburModManager.Models;

namespace HamburburModManager.Services;

public static class GitHubService
{
    private static readonly HttpClient Client = new();

    public static async Task<List<ModInfo>> LoadMods(string url)
    {
        try
        {
            string json = await Client.GetStringAsync(url);

            ModListWrapper? wrapper = JsonSerializer.Deserialize<ModListWrapper>(json, new JsonSerializerOptions
            {
                    PropertyNameCaseInsensitive = true,
            });

            return wrapper?.Mods ?? new List<ModInfo>();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Failed to load mods from {url}: {ex.Message}");

            return [];
        }
    }
}