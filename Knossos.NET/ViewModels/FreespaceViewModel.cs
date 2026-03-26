using CommunityToolkit.Mvvm.ComponentModel;
using Knossos.NET.Views;
using ObservableCollections;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Knossos.NET.ViewModels
{
    public partial class FreespaceViewModel : ViewModelBase
    {
        [ObservableProperty]
        public ObservableList<FreespaceModCardViewModel> cardList = new ObservableList<FreespaceModCardViewModel>();

        public static FreespaceViewModel? Instance { get; private set; }

        public FreespaceViewModel()
        {
            CardList = new ObservableList<FreespaceModCardViewModel>() {
                    new FreespaceModCardViewModel("fs2_org_demo", "The original Freespace 2 Demo with no modifications", "avares://Knossos.NET/Assets/fs2_res/fs2_demo_banner.png", false),
                    new FreespaceModCardViewModel("FS2", "Freespace 2 Retail without any graphics upgrades", "avares://Knossos.NET/Assets/fs2_res/kn_banner.png", true),
                    new FreespaceModCardViewModel("MVPS", "Freespace 2 with the newerest graphics upgrades", "avares://Knossos.NET/Assets/fs2_res/mvps_banner.png", true),
                    new FreespaceModCardViewModel("fsport", "FSPort is a conversion of Freespace 1 story to Freespace 2. No graphics upgrades.", "avares://Knossos.NET/Assets/fs2_res/fsport_banner.png", true),
                    new FreespaceModCardViewModel("fsport-mediavps", "FSPort (Freespace 1 Story) with all the graphic upgrades", "avares://Knossos.NET/Assets/fs2_res/fsport_mvps_banner.png", true),
                    new FreespaceModCardViewModel("str", "A retell of the Freespace 1 expansion 'Silent Threat', includes all the graphic upgrades", "avares://Knossos.NET/Assets/fs2_res/str_banner.png", true),
                    new FreespaceModCardViewModel("fs2coopup", "This mod includes multiplayer coop missions to play the entire Freespace 2 story with friends.", "avares://Knossos.NET/Assets/fs2_res/fs2_coop_banner.jpg", true),
                    new FreespaceModCardViewModel("fs1coopup", "This mod includes multiplayer coop missions to play the entire Freespace 1 story with friends.", "avares://Knossos.NET/Assets/fs2_res/fs1_coop_banner.png", true),
                    new FreespaceModCardViewModel("strcoopup", "This mod includes multiplayer coop missions to play the entire Silent Threat Reborn story with friends.", "avares://Knossos.NET/Assets/fs2_res/str_coop_banner.jpg", true)
            };

            Instance = this;
        }

        /// <summary>
        /// Sets the loading status to all mods listed in the freespace tab
        /// </summary>
        /// <param name="status"></param>
        public void SetLoading(bool status)
        {
            foreach (var item in CardList)
            {
                item.SetLoadingOverlay(status);
            }
        }

        /// <summary>
        /// Set the status of the freespace 2 root found to all mods listed in the freespace tab
        /// </summary>
        /// <param name="status"></param>
        public void SetFS2RootFound(bool status)
        {
            foreach (var item in CardList)
            {
                item.SetRetailOverlay(!status);
            }
        }

        /// <summary>
        /// Set the status of the installing overlay to a mod in the freespace tab
        /// Allows to pass null as modID to change the status of all mods
        /// </summary>
        /// <param name="modID"></param>
        /// <param name="status"></param>
        public void SetInstalling(string? modID,bool status)
        {
            foreach (var item in CardList)
            {
                if(modID == null || modID == item.ModID)
                    item.SetInstallingOverlay(status);
            }
        }

        /// <summary>
        /// Set the status of the installed overlay to a mod in the freespace tab
        /// </summary>
        /// <param name="modID"></param>
        /// <param name="status"></param>
        public void SetInstalled(string modID, bool status)
        {
            foreach (var item in CardList)
            {
                if (modID == item.ModID)
                {
                    item.SetInstalled(status);
                    break;
                }
            }
        }
    }
}
