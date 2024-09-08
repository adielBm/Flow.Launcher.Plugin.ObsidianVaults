using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using System.Threading;
using System.Windows.Controls;
using Flow.Launcher.Plugin;
using System.Diagnostics;

namespace Flow.Launcher.Plugin.ObsidianVaults
{
    public class Main : IAsyncPlugin, IContextMenu /*, ISettingProvider */
    {
        private PluginInitContext context;
        // private Settings settings;

        private const string VaultsFileName = "obsidian.json";
        private string vaultsFilePath;

        public Task InitAsync(PluginInitContext context)
        {
            string appDataPath = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            vaultsFilePath = Path.Combine(appDataPath, "obsidian", VaultsFileName);

            this.context = context;
            // settings = context.API.LoadSettingJsonStorage<Settings>();
            return Task.CompletedTask;
        }

        public Task<List<Result>> QueryAsync(Query query, CancellationToken token)
        {
            var results = new List<Result>();
            var searchTerm = query.Search;

            if (!File.Exists(vaultsFilePath))
            {
                results.Add(new Result
                {
                    Title = "Obsidian not found",
                    SubTitle = "Please make sure Obsidian is installed.",
                    IcoPath = "Images/app.png"
                });
                return Task.FromResult(results);
            }

            var vaults = GetVaults();
            if (vaults == null || !vaults.Any())
            {
                return Task.FromResult(results);
            }

            foreach (var vault in vaults)
            {
                if (vault.Name.Contains(query.Search, StringComparison.OrdinalIgnoreCase))
                {
                    results.Add(new Result
                    {
                        Title = vault.Name,
                        SubTitle = vault.Path,
                        IcoPath = "Images/folder.png",
                        Action = _ =>
                        {
                            // Correct the URL format by passing only the vault name
                            var vaultName = vault.Name; // Assuming vault.Name corresponds to the vault's name
                            var obsidianUrl = $"obsidian://open?vault={Uri.EscapeDataString(vaultName)}";
                            System.Diagnostics.Process.Start(new ProcessStartInfo(obsidianUrl) { UseShellExecute = true });
                            return true;
                        }
                    });
                }
            }

            return Task.FromResult(results);
        }

        // Function to read vaults from the obsidian.json file using System.Text.Json
        private List<Vault> GetVaults()
        {
            try
            {
                var jsonData = File.ReadAllText(vaultsFilePath);
                var vaults = new List<Vault>();

                // Parse the JSON data
                using (JsonDocument document = JsonDocument.Parse(jsonData))
                {
                    JsonElement root = document.RootElement;
                    if (root.TryGetProperty("vaults", out JsonElement vaultsElement))
                    {
                        foreach (JsonProperty vault in vaultsElement.EnumerateObject())
                        {
                            if (vault.Value.TryGetProperty("path", out JsonElement pathElement))
                            {
                                var path = pathElement.GetString();
                                if (!string.IsNullOrEmpty(path))
                                {
                                    vaults.Add(new Vault { Name = Path.GetFileName(path), Path = path });
                                }
                            }
                        }
                    }
                }

                return vaults;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error reading vaults: {ex.Message}");
                return null;
            }
        }

        // Vault class to hold vault information
        private class Vault
        {
            public string Name { get; set; }
            public string Path { get; set; }
        }

        public void Dispose()
        {
        }

        // public Control CreateSettingPanel() => new SettingsControl(settings);

        public List<Result> LoadContextMenus(Result selectedResult)
        {
            return new List<Result>();
        }
    }
}
