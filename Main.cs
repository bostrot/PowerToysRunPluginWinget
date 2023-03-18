// Copyright (c) Microsoft Corporation
// The Microsoft Corporation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
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

                Process process = new Process();

                process.StartInfo.FileName = "winget";
                process.StartInfo.Arguments = $"search \"{searchTerm}\"";
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

                // If there is no error, iterate through the output and add each line as a result
                string[] lines = output.Split(new[] { Environment.NewLine }, StringSplitOptions.None);

                var id = 0;

                int nameChars = 0;
                int idChars = 0;
                int versionChars = 0;
                int matchChars = 0;

                // Regex for words in header
                string v = lines[0];
                var matches = Regex.Matches(v, @"\S+");

                if (matches != null)
                {
                    // Get chars between Name, ID, Version, Matches, Source length including spaces
                    if (matches.Count == 5)
                    {
                        nameChars = matches[2].Index - matches[1].Index;
                        idChars = matches[3].Index - 1 - matches[2].Index;
                        versionChars = matches[4].Index - matches[3].Index;
                    }
                    else if (matches.Count == 6)
                    {
                        nameChars = matches[2].Index - matches[1].Index;
                        idChars = matches[3].Index - 1 - matches[2].Index;
                        versionChars = matches[4].Index - matches[3].Index;
                        matchChars = matches[5].Index - 1 - matches[4].Index;
                    }
                }

                foreach (string line0 in lines)
                {
                    // Skip header
                    if (id < 2)
                    {
                        id++;
                        continue;
                    }

                    // Filter non-text, non-number, non-space and non (-_.,) characters
                    var line = AllowedCharacters().Replace(line0, string.Empty);

                    if (line != string.Empty)
                    {
                        string name = string.Empty;
                        string idStr = string.Empty;
                        string version = string.Empty;
                        string match = string.Empty;
                        string source = string.Empty;
                        try
                        {
                            // Divide line into 5 parts by split
                            name = line.Substring(0, nameChars).Trim();
                            idStr = line.Substring(nameChars, idChars).Trim();
                            version = line.Substring(nameChars + idChars, versionChars).Trim();
                            if (matches.Count == 6)
                            {
                                match = line.Substring(versionChars + nameChars + idChars, matchChars).Trim();
                                source = line.Substring(matchChars + versionChars + nameChars + idChars).Trim();
                            }
                            else
                            {
                                match = string.Empty;
                            }
                        }
                        catch (Exception e)
                        {
                            name = e.ToString();
                        }

                        // Check if result is empty
                        if (name == string.Empty)
                        {
                            continue;
                        }

                        string title = $"{name} ({idStr})";
                        string subTitle = $"{Properties.Resources.plugin_result_name} {name} [{version}] ({source}) {match}";

                        if (source == string.Empty)
                        {
                            subTitle = $"{Properties.Resources.plugin_result_name} {name} [{version}]";
                        }

                        results.Add(new Result
                        {
                            Title = title,
                            SubTitle = subTitle,
                            QueryTextDisplay = name,
                            IcoPath = _iconPath,
                            ProgramArguments = idStr,
                            Action = action =>
                            {
                                Helper.OpenInShell("winget", "install " + idStr + " --wait", "/");

                                return true;
                            },
                        });
                        id++;
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
                        Helper.OpenInShell("winget", "install " + idStr + " -i --force --wait", "/");
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
                        Helper.OpenInShell("winget", "upgrade " + idStr + " --wait", "/");
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
                        Helper.OpenInShell("winget", "uninstall " + idStr + " --wait", "/");
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
                _iconPath = "Images/WebSearch.light.png";
            }
            else
            {
                _iconPath = "Images/WebSearch.dark.png";
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
