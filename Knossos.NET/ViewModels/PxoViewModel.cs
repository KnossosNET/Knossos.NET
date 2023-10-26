using Avalonia.Controls;
using CommunityToolkit.Mvvm.ComponentModel;
using Knossos.NET.Models;
using Knossos.NET.Views;
using Knossos.NET.Views.Windows;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace Knossos.NET.ViewModels
{
    public class PxoGamesActive
    {
        public string? Game { get; set; }
        public string? Tag { get; set; }
        public List<PxoServer> Servers { get; set; } = new List<PxoServer>();
    }

    public class PxoServer
    {
        public string Added { get; set; } = string.Empty;
        public string LastUpdate { get; set; } = string.Empty;
        public List<string> Flags { get; set; } = new List<string>();
        public string Probe { get; set; } = string.Empty;
        public string Location { get; set; } = string.Empty;
        public string IP { get; set; } = string.Empty;
        public int Port { get; set; }
        public PxoGame Game { get; set; } = new PxoGame();
    }

    public class PxoGame
    {
        public string Name { get; set; } = string.Empty;
        public int NumPlayers { get; set; }
        public int MaxPlayers { get; set; }
        public string Mode { get; set; } = string.Empty;
        public string Type { get; set; } = string.Empty;
        public string State { get; set; } = string.Empty;
        public string Mission { get; set; } = string.Empty;
        public string Channel { get; set; } = string.Empty;
        public int Version { get; set; }
        public List<string> Flags { get; set; } = new List<string>();
    }

    public partial class PxoViewModel : ViewModelBase
    {
        public static PxoViewModel? Instance;
        private bool initialLoadDone = false;

        [ObservableProperty]
        internal List<PxoGamesActive> activeGames = new List<PxoGamesActive>();

        [ObservableProperty]
        internal string login = string.Empty;
        [ObservableProperty]
        internal string password = string.Empty;

        public PxoViewModel()
        {
            Instance = this;
        }

        internal void OpenPXOWeb()
        {
            KnUtils.OpenBrowserURL(@"https://pxo.nottheeye.com/");
        }

        public void InitialLoad()
        {
            Login = Knossos.globalSettings.pxoLogin;
            Password = Knossos.globalSettings.pxoPassword;
            if (!initialLoadDone)
            {
                RefreshData();
                initialLoadDone = true;
            }
        }

        internal async void SavePXOCredentials()
        {
            Login = Login.Replace(" ","");
            Password = Password.Replace(" ", "");
            if (Login == string.Empty || Password == string.Empty)
            {
                await MessageBox.Show(MainWindow.instance!, "The PXO Login and Password cant be empty.", "Validation error", MessageBox.MessageBoxButtons.OK);
                return;
            }

            if (!Login.All(char.IsDigit) || Login.Length > 8)
            {
                await MessageBox.Show(MainWindow.instance!, "The PXO Login can only contain digits and have a max length of 8.", "Validation error", MessageBox.MessageBoxButtons.OK);
                return;
            }

            if (Password.All(char.IsDigit) || !Password.All(char.IsLower) || Password.Length != 12)
            {
                await MessageBox.Show(MainWindow.instance!, "The PXO Password can only contain 12 lowercase letters.", "Validation error", MessageBox.MessageBoxButtons.OK);
                return;
            }

            Knossos.globalSettings.pxoLogin = Login;
            Knossos.globalSettings.pxoPassword = Password;
            Knossos.globalSettings.WriteFS2IniValues();
        }

        internal async void OpenServerCreator()
        {
            if (MainWindow.instance != null)
            {
                var dialog = new ServerCreatorView();
                dialog.DataContext = new ServerCreatorViewModel();

                await dialog.ShowDialog<ServerCreatorView?>(MainWindow.instance);
            }
        }

        public async void RefreshData()
        {
            
            using (HttpClient client = new HttpClient())
            {
                try
                {
                    HttpResponseMessage response = await client.GetAsync("https://pxo.nottheeye.com/api/v1/games/active");
                    var json = await response.Content.ReadAsStringAsync();
                    ActiveGames = JsonSerializer.Deserialize<List<PxoGamesActive>>(json)!;
                    foreach (var result in ActiveGames)
                    {
                        foreach(var server in result.Servers)
                        {
                            foreach(var flag in server.Flags)
                            {
                                if(flag.Contains("probe"))
                                {
                                    server.Probe = flag;
                                }
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    if(MainWindow.instance != null)
                    {
                        await MessageBox.Show(MainWindow.instance,ex.Message,"Error getting PXO Game list",MessageBox.MessageBoxButtons.OK);
                    }
                }
            }
        }
    }
}
