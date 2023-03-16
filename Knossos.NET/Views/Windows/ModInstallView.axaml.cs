using Avalonia.Controls;
using System;
using System.ComponentModel;

namespace Knossos.NET.Views
{
    public partial class ModInstallView : Window
    {
        public ModInstallView()
        {
            InitializeComponent();
            this.Closing += ForceCollectTrash;
        }

        private void ForceCollectTrash(object? sender, CancelEventArgs e)
        {
            GC.Collect();
        }
    }
}
