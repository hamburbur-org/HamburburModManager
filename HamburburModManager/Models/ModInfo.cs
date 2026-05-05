namespace HamburburModManager.Models;

public class ModInfo
{
    public string Name        { get; set; }
    public string Creator     { get; set; }
    public string Category    { get; set; }
    public string DownloadUrl { get; set; }
    public string FileName    { get; set; }
}

public class ModListWrapper
{
    public List<ModInfo> Mods { get; init; } = [];
}