using Avalonia.Controls;
using Avalonia.Controls.Presenters;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.VisualTree;
using System;
using System.Linq;

namespace Knossos.NET.Views
{
    /// <summary>
    /// Handles displaying a view over an overlay on the MainView
    /// to display windows or messages
    /// </summary>
    public static class DialogHost
    {
        public static ContentPresenter? Show(Control dialog, Action? onDismiss = null)
        {
            var overlayHost = MainView.instance?.GetVisualDescendants().OfType<ContentPresenter>().FirstOrDefault(x => x.Name == "DialogOverlay");
            if (overlayHost == null)
            {
                Log.Add(Log.LogSeverity.Error, "DialogHost.Show()", "Unable to find the dialog overlay.");
                return null;
            }
            EnsureHost(overlayHost);
            overlayHost.IsHitTestVisible = true;
            overlayHost.Content = Wrap(dialog, onDismiss);
            return overlayHost;
        }


        public static void Hide(Control dialog, ContentPresenter? overlayHost = null)
        {
            if(overlayHost == null)
                overlayHost = MainView.instance?.GetVisualDescendants().OfType<ContentPresenter>().FirstOrDefault(x => x.Name == "DialogOverlay");
            if (overlayHost == null)
            {
                Log.Add(Log.LogSeverity.Error, "DialogHost.Show()", "Unable to find the dialog overlay.");
                return;
            }
            overlayHost.Content = null;
            overlayHost.IsHitTestVisible = false;
        }


        private static Control Wrap(Control content, Action? onDismiss)
        {
            var root = new Border
            {
                Background = new SolidColorBrush(Color.FromArgb(0x80, 0x00, 0x00, 0x00)),
                Child = content,
                HorizontalAlignment = HorizontalAlignment.Stretch,
                VerticalAlignment = VerticalAlignment.Stretch
            };
            //root.PointerPressed += (_, __) => onDismiss?.Invoke(); // dismiss outside card
            return root;
        }


        private static void EnsureHost(ContentPresenter overlayHost)
        {
            // Make sure overlayHost stretches over the window
            overlayHost.HorizontalAlignment = HorizontalAlignment.Stretch;
            overlayHost.VerticalAlignment = VerticalAlignment.Stretch;
        }
    }
}