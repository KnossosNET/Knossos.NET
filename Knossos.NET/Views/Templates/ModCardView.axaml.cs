using Avalonia.Controls;

namespace Knossos.NET.Views
{
    public partial class ModCardView : UserControl
    {
        public ModCardView()
        {
            InitializeComponent();
        }

        private void StackPanel_PointerPressed(object? sender, Avalonia.Input.PointerPressedEventArgs e)
        {
            if (!KnUtils.IsAndroid)
                return;
            var overlayPanel = this.FindControl<StackPanel>("Overlay");
            if (overlayPanel != null)
            {
                if (overlayPanel.Opacity == 0)
                {
                    overlayPanel.Opacity = 0.8;
                }
                else
                {
                    overlayPanel.Opacity = 0;
                }
            }
        }
    }
}
