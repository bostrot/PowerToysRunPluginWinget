﻿// Copyright (c) Microsoft Corporation
// The Microsoft Corporation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.IO.Packaging;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
using System.Reflection;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Shapes;
using ManagedCommon;
using Microsoft.PowerToys.Settings.UI.Library;
using Wox.Infrastructure;
using Wox.Plugin;
using Wox.Plugin.Logger;
using BrowserInfo = Wox.Plugin.Common.DefaultBrowserInfo;

namespace Community.PowerToys.Run.Plugin.Winget
{
    public partial class Main : IPlugin, IPluginI18n, IContextMenu, ISettingProvider, IReloadable, IDisposable
    {
        // Should only be set in Init()
        private Action onPluginError;

        private static List<WingetPackage> packagesList;

        private const string NotGlobalIfUri = nameof(NotGlobalIfUri);

        /// <summary>If true, dont show global result on queries that are URIs</summary>
        private bool _notGlobalIfUri;

        private PluginInitContext _context;

        private string _iconPath;

        private bool _disposed;

        public string Name => Properties.Resources.plugin_name;

        public string Description => Properties.Resources.plugin_description;

        public IEnumerable<PluginAdditionalOption> AdditionalOptions => new List<PluginAdditionalOption>()
        {
            new PluginAdditionalOption()
            {
                Key = NotGlobalIfUri,
                DisplayLabel = Properties.Resources.plugin_global_if_uri,
                Value = true,
            },
        };

        private static string installed;

        // constructor
        public Main()
        {
            GetPackages();
            LoadInstalledList();
        }

        private static void LoadInstalledList()
        {
            Process process = new Process();

            process.StartInfo.FileName = "winget";
            process.StartInfo.Arguments = "list";
            process.StartInfo.UseShellExecute = false;
            process.StartInfo.CreateNoWindow = true;
            process.StartInfo.RedirectStandardOutput = true;
            process.Start();

            string output = process.StandardOutput.ReadToEnd();
            process.WaitForExit();

            // UTF16 to UTF8
            output = System.Text.Encoding.UTF8.GetString(
                System.Text.Encoding.Convert(
                    System.Text.Encoding.Unicode,
                    System.Text.Encoding.UTF8,
                    System.Text.Encoding.Unicode.GetBytes(output)));

            installed = output;
        }

        public class WingetPackage
        {
            public string Name { get; set; }

            public string Company { get; set; }

            public string Version { get; set; }
        }

        private static async void GetPackages()
        {
            // Download packages list
            var packages = new List<string>();
            HttpClient client = new HttpClient();
            HttpResponseMessage response = await client.GetAsync("https://bostrot.github.io/PowerToysRunPluginWinget/pkgs.json");
            response.EnsureSuccessStatusCode();
            string responseBody = await response.Content.ReadAsStringAsync();

            // Allow trailing comma
            var options = new JsonSerializerOptions
            {
                AllowTrailingCommas = true,
                PropertyNameCaseInsensitive = true,
            };

            // Parse response to JSON and ignore use lowercase first letter instead of uppercase
            packagesList = JsonSerializer.Deserialize<List<WingetPackage>>(responseBody, options);
        }

        public List<Result> Query(Query query)
        {
            if (query is null)
            {
                throw new ArgumentNullException(nameof(query));
            }

            var results = new List<Result>();

            // empty query
            if (string.IsNullOrEmpty(query.Search))
            {
                string arguments = "winget ";
                results.Add(new Result
                {
                    Title = Properties.Resources.plugin_description,
                    SubTitle = "via winget CLI",
                    QueryTextDisplay = string.Empty,
                    IcoPath = _iconPath,
                    ProgramArguments = arguments,
                    Action = action =>
                    {
                        return true;
                    },
                });
                return results;
            }
            else
            {
                string searchTerm = query.Search;
                foreach (WingetPackage package in packagesList)
                {
                    var idStr = $"{package.Company}.{package.Name}";
                    if (package.Name.ToLower().Contains(searchTerm.ToLower()) || package.Company.ToLower().Contains(searchTerm.ToLower()))
                    {
                        results.Add(new Result
                        {
                            Title = package.Name,
                            SubTitle = $"by {package.Company} version: {package.Version}",
                            QueryTextDisplay = idStr,
                            IcoPath = _iconPath,
                            ProgramArguments = idStr,
                            Action = action =>
                            {
                                Winget($"install {idStr} --wait");
                                return true;
                            },
                        });
                    }
                }
            }

            return results;
        }

        public void Init(PluginInitContext context)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
            _context.API.ThemeChanged += OnThemeChanged;
            UpdateIconPath(_context.API.GetCurrentTheme());
            BrowserInfo.UpdateIfTimePassed();

            onPluginError = () =>
            {
                string errorMsgString = string.Format(CultureInfo.CurrentCulture, Properties.Resources.plugin_search_failed, BrowserInfo.Name ?? BrowserInfo.MSEdgeName);

                Log.Error(errorMsgString, GetType());
                _context.API.ShowMsg(
                    $"Plugin: {Properties.Resources.plugin_name}",
                    errorMsgString);
            };
        }

        public static void Winget(string cmd)
        {
            // Call thread
            Thread thread = new Thread(() => WingetCmdThread(cmd));
            thread.Start();
        }

        public static void WingetCmdThread(string cmd)
        {
            Process process = new Process();

            process.StartInfo.FileName = "winget";
            process.StartInfo.Arguments = cmd;
            process.StartInfo.UseShellExecute = true;
            process.StartInfo.WindowStyle = ProcessWindowStyle.Normal;
            process.Start();

            // Wait for process to exit
            process.WaitForExit();
            LoadInstalledList();
        }

        private static List<ContextMenuResult> GetContextMenu(in Result result, in string assemblyName)
        {
            if (result?.Title == Properties.Resources.plugin_description)
            {
                return new List<ContextMenuResult>(0);
            }

            var idStr = result?.ProgramArguments;
            var name = result?.QueryTextDisplay.Replace("winget ", string.Empty);

            List<ContextMenuResult> list = new List<ContextMenuResult>(1)
            {
                new ContextMenuResult
                {
                    AcceleratorKey = Key.I,
                    AcceleratorModifiers = ModifierKeys.Control,
                    Action = _ =>
                    {
                        Winget("install " + idStr + " -i --force --wait");
                        return true;
                    },
                    FontFamily = "Segoe MDL2 Assets",
                    Glyph = "\xE70F", // Symbol: Edit
                    PluginName = assemblyName,
                    Title = "Forced interactive install (Ctrl+I)",
                },
            };

            if (installed.ToLower().Contains(name.ToLower()))
            {
                list.Add(new ContextMenuResult
                {
                    AcceleratorKey = Key.U,
                    AcceleratorModifiers = ModifierKeys.Control,
                    Action = _ =>
                    {
                        Winget("upgrade " + idStr + " --wait");
                        return true;
                    },
                    FontFamily = "Segoe MDL2 Assets",
                    Glyph = "\xE777", // Symbol: UpdateRestore
                    PluginName = assemblyName,
                    Title = "Upgrade (Ctrl+U)",
                });
                list.Add(new ContextMenuResult
                {
                    AcceleratorKey = Key.D,
                    AcceleratorModifiers = ModifierKeys.Control,
                    Action = _ =>
                    {
                        Winget("uninstall " + idStr + " --wait");
                        return true;
                    },
                    FontFamily = "Segoe MDL2 Assets",
                    Glyph = "\xE74D", // Symbol: Delete
                    PluginName = assemblyName,
                    Title = "Delete (Ctrl+D)",
                });
            }

            return list;
        }

        public List<ContextMenuResult> LoadContextMenus(Result selectedResult)
        {
            return GetContextMenu(selectedResult, "someassemblyname");
        }

        public string GetTranslatedPluginTitle()
        {
            return Properties.Resources.plugin_name;
        }

        public string GetTranslatedPluginDescription()
        {
            return Properties.Resources.plugin_description;
        }

        private void OnThemeChanged(Theme oldtheme, Theme newTheme)
        {
            UpdateIconPath(newTheme);
        }

        private void UpdateIconPath(Theme theme)
        {
            if (theme == Theme.Light || theme == Theme.HighContrastWhite)
            {
                _iconPath = "Images/Winget.light.png";
            }
            else
            {
                _iconPath = "Images/Winget.dark.png";
            }
        }

        public Control CreateSettingPanel()
        {
            throw new NotImplementedException();
        }

        public void UpdateSettings(PowerLauncherPluginSettings settings)
        {
            _notGlobalIfUri = settings?.AdditionalOptions?.FirstOrDefault(x => x.Key == NotGlobalIfUri)?.Value ?? false;
        }

        public void ReloadData()
        {
            if (_context is null)
            {
                return;
            }

            UpdateIconPath(_context.API.GetCurrentTheme());
            BrowserInfo.UpdateIfTimePassed();
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!_disposed && disposing)
            {
                if (_context != null && _context.API != null)
                {
                    _context.API.ThemeChanged -= OnThemeChanged;
                }

                _disposed = true;
            }
        }

        [GeneratedRegex("[^\\u0020-\\u007E]")]
        private static partial Regex AllowedCharacters();
    }
}
