using System.Diagnostics;
using System.Numerics;
using ClickableTransparentOverlay;
using HamburburModManager.Core;
using HamburburModManager.Models;
using HamburburModManager.Services;
using ImGuiNET;

namespace HamburburModManager;

public class Renderer : Overlay
{
    private readonly  Vector4       MainColour   = new(0.1694782f, 0.1504984f, 0.3584906f, 1f);
    private readonly List<ModInfo> officialMods = [];

    private readonly  Vector4                   SecondaryColour = new(0.03906193f, 0.0252314f, 0.1981132f, 1f);
    private readonly Dictionary<ModInfo, bool> selectedMods    = new();
    private readonly List<ModInfo>             unofficialMods  = [];
    private          string                    errorMessage    = "";
    private          bool                      errorPopupOpen;
    private          string                    gamePath        = "Not Found";
    private          string                    manualPathInput = "";
    private          bool                      pathPopupOpen;
    private          int                       selectedSidebar;
    private          bool                      showErrorPopup;
    private          bool                      showPathPopup;
    
    private bool modsLoaded = false;

    protected override void Render()
    {
        try
        {
            ImGui.SetNextWindowSize(new Vector2(800, 500), ImGuiCond.FirstUseEver);

            ImGui.PushStyleColor(ImGuiCol.TitleBgActive, SecondaryColour);
            ImGui.PushStyleColor(ImGuiCol.Button,        MainColour);
            ImGui.PushStyleColor(ImGuiCol.FrameBg,       new Vector4(0.1f, 0.1f, 0.1f, 1f));

            ImGui.Begin("Hamburbur Mod Manager");

            ImGui.SameLine(ImGui.GetWindowWidth() - 25);
            if (ImGui.Button("X", new Vector2(20, 20)))
                Environment.Exit(0);

            DrawSidebar();
            ImGui.SameLine();
            ImGui.BeginChild("MainContent");

            switch (selectedSidebar)
            {
                case 0: DrawHome(); break;
                case 1: DrawMods(); break;
                case 2: DrawConfigEditor(); break;
            }

            ImGui.PopStyleColor(3);

            ImGui.EndChild();
            ImGui.End();

            HandlePopups();
        }
        catch (Exception ex)
        {
            showErrorPopup = true;
            errorMessage   = ex.Message;
        }
    }

    private void DrawSidebar()
    {
        ImGui.BeginChild("Sidebar", new Vector2(200, 0), ImGuiChildFlags.None);

        if (ImGui.Selectable("Home",          selectedSidebar == 0)) selectedSidebar = 0;
        if (ImGui.Selectable("Mods",          selectedSidebar == 1)) selectedSidebar = 1;
        if (ImGui.Selectable("Config Editor", selectedSidebar == 2)) selectedSidebar = 2;

        ImGui.Separator();

        if (selectedMods.Count > 0 && ImGui.Button("Install/Update Selected"))
            InstallSelectedMods();

        if (ImGui.Button("Game Folder") && Directory.Exists(gamePath))
            Process.Start("explorer.exe", gamePath);

        if (ImGui.Button("Plugins Folder"))
        {
            string plugins = Path.Combine(gamePath, "BepInEx", "plugins");
            if (Directory.Exists(plugins))
                Process.Start("explorer.exe", plugins);
        }

        if (ImGui.Button("Discord"))
            Process.Start(new ProcessStartInfo
            {
                    FileName        = "https://discord.gg/hamburbur",
                    UseShellExecute = true,
            });

        ImGui.EndChild();
    }

    private void DrawHome()
    {
        ImGui.Text("Gorilla Tag Mod Manager");
        ImGui.Separator();
        ImGui.Text($"Game Path: {gamePath}");

        if (gamePath == "Not Found")
        {
            gamePath = GameLocator.FindGame();
        }

        if (string.IsNullOrEmpty(gamePath))
            return;

        if (!BepInExManager.HasBepInEx(gamePath))
            _ = BepInExManager.InstallBepInEx(gamePath);

        LoadMods().Wait();
    }

    private void DrawMods()
    {
        if (!ImGui.BeginTabBar("ModsTabs")) return;

        if (ImGui.BeginTabItem("Official"))
        {
            DrawModList(officialMods);
            ImGui.EndTabItem();
        }

        if (ImGui.BeginTabItem("Unofficial"))
        {
            DrawModList(unofficialMods);
            ImGui.EndTabItem();
        }

        ImGui.EndTabBar();
    }

    private void DrawModList(List<ModInfo> mods)
    {
        string                                  pluginsDir = Path.Combine(gamePath, "BepInEx", "plugins");
        IEnumerable<IGrouping<string, ModInfo>> grouped    = mods.GroupBy(m => m.Category);

        foreach (IGrouping<string, ModInfo> categoryGroup in grouped)
        {
            if (!ImGui.CollapsingHeader(categoryGroup.Key)) continue;

            foreach (ModInfo mod in categoryGroup)
            {
                ImGui.BeginChild(mod.Name, new Vector2(0, 60), ImGuiChildFlags.None);
                ImGui.Text($"{mod.Name} by {mod.Creator}");

                if (!selectedMods.ContainsKey(mod))
                    selectedMods[mod] = false;

                bool selected = selectedMods[mod];
                ImGui.Checkbox("Select", ref selected);
                selectedMods[mod] = selected;

                bool fileExists = Directory.EnumerateFiles(pluginsDir, mod.FileName, SearchOption.AllDirectories).Any();
                if (fileExists)
                {
                    ImGui.SameLine();
                    if (ImGui.Button("Delete"))
                        foreach (string file in Directory.EnumerateFiles(pluginsDir, mod.FileName,
                                         SearchOption.AllDirectories))
                            try
                            {
                                File.Delete(file);
                            }
                            catch (Exception ex)
                            {
                                errorMessage   = $"Failed to delete {file}: {ex.Message}";
                                showErrorPopup = true;
                            }
                }

                ImGui.EndChild();
            }
        }
    }

    private void InstallSelectedMods()
    {
        string pluginsDir = Path.Combine(gamePath, "BepInEx", "plugins");
        foreach (KeyValuePair<ModInfo, bool> kv in selectedMods.Where(kv => kv.Value))
            InstallOrUpdateMod(kv.Key, pluginsDir);

        List<ModInfo> keys                          = selectedMods.Keys.ToList();
        foreach (ModInfo k in keys) selectedMods[k] = false;
    }

    private void InstallOrUpdateMod(ModInfo mod, string pluginsDir)
    {
        using HttpClient client   = new();
        string           tempFile = Path.Combine(Path.GetTempPath(), mod.FileName);
        using (Stream stream = client.GetStreamAsync(mod.DownloadUrl).Result)
        {
            using (FileStream fs = File.Create(tempFile))
            {
                stream.CopyTo(fs);
            }
        }

        bool replaced = false;
        foreach (string file in Directory.EnumerateFiles(pluginsDir, mod.FileName, SearchOption.AllDirectories))
        {
            File.Copy(tempFile, file, true);
            replaced = true;
        }

        if (!replaced)
        {
            string modFolder = Path.Combine(pluginsDir, mod.Name);
            if (!Directory.Exists(modFolder))
                Directory.CreateDirectory(modFolder);

            File.Copy(tempFile, Path.Combine(modFolder, mod.FileName), true);
        }

        File.Delete(tempFile);
    }

    private static void DrawConfigEditor()
    {
        ImGui.Text("Config Editor coming soon...");
    }

    private void HandlePopups()
    {
        if (showPathPopup)
        {
            pathPopupOpen = true;
            ImGui.OpenPopup("Select Game Folder");
            showPathPopup = false;
        }

        if (ImGui.BeginPopupModal("Select Game Folder", ref pathPopupOpen, ImGuiWindowFlags.AlwaysAutoResize))
        {
            ImGui.Text("Could not find Gorilla Tag automatically.");
            ImGui.InputText("Path", ref manualPathInput, 260);

            if (ImGui.Button("Confirm"))
                if (GameLocator.IsValidGamePath(manualPathInput))
                {
                    gamePath = manualPathInput;
                    GameLocator.SavePath(manualPathInput);
                    pathPopupOpen = false;
                }

            ImGui.SameLine();
            if (ImGui.Button("Cancel")) pathPopupOpen = false;
            ImGui.EndPopup();
        }

        if (showErrorPopup)
        {
            errorPopupOpen = true;
            ImGui.OpenPopup("Error");
            showErrorPopup = false;
        }

        if (!ImGui.BeginPopupModal("Error", ref errorPopupOpen, ImGuiWindowFlags.AlwaysAutoResize))
            return;

        ImGui.Text($"An error occurred: {errorMessage}");
        if (ImGui.Button("Close")) errorPopupOpen = false;
        ImGui.EndPopup();
    }

    private async Task LoadMods()
    {
        const string baseUrl = "https://raw.githubusercontent.com/ZlothY29IQ/GorillaInfo/refs/heads/main/";
        
        const string OfficialUrl = baseUrl + "official.json";

        const string UnofficialUrl = baseUrl + "unofficial.json";

        officialMods.Clear();
        unofficialMods.Clear();
        selectedMods.Clear();

        officialMods.AddRange(await GitHubService.LoadMods(OfficialUrl));
        unofficialMods.AddRange(await GitHubService.LoadMods(UnofficialUrl));
    }
}