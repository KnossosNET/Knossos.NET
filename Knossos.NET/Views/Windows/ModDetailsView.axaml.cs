using Avalonia.Controls;
using Avalonia.Layout;
using System;
using System.ComponentModel;

namespace Knossos.NET.Views
{
    public partial class ModDetailsView : Window
    {
        private Carousel _carousel;
        private Button _left;
        private Button _right;

        public ModDetailsView()
        {
            InitializeComponent();
            this.Closing += ModDetailsView_StopTTS;

            _carousel = this.Get<Carousel>("carousel");
            _left = this.Get<Button>("left");
            _right = this.Get<Button>("right");
            _left.Click += (s, e) => _carousel.Previous();
            _right.Click += (s, e) => _carousel.Next();
        }

        private void ModDetailsView_StopTTS(object? sender, CancelEventArgs e)
        {
            Knossos.Tts(string.Empty);
        }
    }
}
