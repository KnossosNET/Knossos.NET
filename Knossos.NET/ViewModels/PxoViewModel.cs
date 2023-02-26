using Avalonia.Controls;
using CommunityToolkit.Mvvm.ComponentModel;
using Knossos.NET.Models;
using Knossos.NET.Views;
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
        [ObservableProperty]
        private List<PxoGamesActive> activeGames = new List<PxoGamesActive>();

        public PxoViewModel()
        {

        }

        private void OpenPXOWeb()
        {
            Knossos.OpenBrowserURL(@"https://pxo.nottheeye.com/");
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
