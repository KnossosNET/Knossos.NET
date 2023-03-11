using Avalonia.Controls;
using System;
using System.ComponentModel;

namespace Knossos.NET.Views
{
    public partial class ModDetailsView : Window
    {
        public ModDetailsView()
        {
            InitializeComponent();
            this.Closing += ModDetailsView_StopTTS;
        }

        private void ModDetailsView_StopTTS(object? sender, CancelEventArgs e)
        {
            Knossos.Tts(string.Empty);
        }
    }
}
