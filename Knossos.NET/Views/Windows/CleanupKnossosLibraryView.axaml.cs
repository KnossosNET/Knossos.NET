using System;
using Avalonia.Controls;
using Knossos.NET.ViewModels;

namespace Knossos.NET.Views
{
    public partial class CleanupKnossosLibraryView : Window
    {
        public CleanupKnossosLibraryView()
        {
            InitializeComponent();
        }

        protected override void OnOpened(EventArgs e)
        {
            base.OnOpened(e);
            ((CleanupKnossosLibraryViewModel)DataContext!).LoadRemovableMods();
        }
    }
}
