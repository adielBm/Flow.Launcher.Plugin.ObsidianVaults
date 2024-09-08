using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using System.Diagnostics;
using Flow.Launcher.Plugin;
using System.Windows.Input;

namespace Flow.Launcher.Plugin.ObsidianVaults
{
    public class Main : IAsyncPlugin, IContextMenu
    {
        private const string VaultsFileName = "obsidian.json";
        private string vaultsFilePath;

        public Task InitAsync(PluginInitContext context)
        {
            string appDataPath = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            vaultsFilePath = Path.Combine(appDataPath, "obsidian", VaultsFileName);
            return Task.CompletedTask;
        }

        public Task<List<Result>> QueryAsync(Query query, CancellationToken token)
        {
            var results = new List<Result>();

            if (!File.Exists(vaultsFilePath))
            {
                return Task.FromResult(new List<Result>
                {
                    CreateResult("Obsidian not found", "Please make sure Obsidian is installed.", "Images/icon.png")
                });
            }

            var vaults = GetVaults() ?? new List<Vault>();

            results.AddRange(vaults
                .Where(vault => vault.Name.Contains(query.Search, StringComparison.OrdinalIgnoreCase))
                .Select(vault => CreateVaultResult(vault))
            );

            return Task.FromResult(results);
        }

        public List<Result> LoadContextMenus(Result selectedResult)
        {
            if (selectedResult.ContextData is Vault vault)
            {
                return new List<Result>
                {
                    CreateContextMenu("Open Folder", "Images/folder.png", () => OpenFolder(vault.Path)),
                    CreateContextMenu("Open in Obsidian", "Images/icon.png", () => OpenObsidian(vault.Name))
                };
            }

            return new List<Result>();
        }

        private Result CreateVaultResult(Vault vault)
        {
            return CreateResult(vault.Name, vault.Path, "Images/icon.png", vault, _ =>
            {
                if (_.SpecialKeyState.ToModifierKeys() == ModifierKeys.Control)
                {
                    OpenFolder(vault.Path);
                }
                else
                {
                    OpenObsidian(vault.Name);
                }
                return true;
            });
        }

        private static Result CreateResult(string title, string subTitle, string iconPath, object contextData = null, Func<ActionContext, bool> action = null)
        {
            return new Result
            {
                Title = title,
                SubTitle = subTitle,
                IcoPath = iconPath,
                ContextData = contextData,
                Action = action
            };
        }

        private static Result CreateContextMenu(string title, string iconPath, Func<bool> action)
        {
            return new Result
            {
                Title = title,
                IcoPath = iconPath,
                Action = _ => action()
            };
        }

        private bool OpenFolder(string path)
        {
            Process.Start("explorer.exe", path);
            return true;
        }

        private bool OpenObsidian(string vaultName)
        {
            var obsidianUrl = $"obsidian://open?vault={Uri.EscapeDataString(vaultName)}";
            Process.Start(new ProcessStartInfo(obsidianUrl) { UseShellExecute = true });
            return true;
        }

        private List<Vault> GetVaults()
        {
            try
            {
                var jsonData = File.ReadAllText(vaultsFilePath);

                using var document = JsonDocument.Parse(jsonData);
                return document.RootElement.TryGetProperty("vaults", out var vaultsElement)
                    ? vaultsElement.EnumerateObject()
                        .Select(vault => vault.Value.TryGetProperty("path", out var pathElement) && !string.IsNullOrEmpty(pathElement.GetString())
                            ? new Vault { Name = Path.GetFileName(pathElement.GetString()), Path = pathElement.GetString() }
                            : null)
                        .Where(vault => vault != null)
                        .ToList()
                    : new List<Vault>();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error reading vaults: {ex.Message}");
                return null;
            }
        }

        private class Vault
        {
            public string Name { get; set; }
            public string Path { get; set; }
        }

        public void Dispose() { }
    }
}
