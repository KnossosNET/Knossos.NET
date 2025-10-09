using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Presenters;
using Avalonia.Media;
using System;
using System.ComponentModel;
using System.Threading.Tasks;

namespace Knossos.NET.Views
{
    /// <summary>
    /// Window wrapper system
    /// For opening new windows both on desktop OS and single view systems like Android
    /// Instead of using Avalonia Window, use this, this will create a new window in runtime for desktop os
    /// and display the view on a overlay over the MainView in single view OS.
    /// </summary>
    public partial class KnossosWindow : UserControl
    {
        public KnossosWindow()
        {
        }

        //Add some properties from Window missing on Usercontrol 
        public static readonly StyledProperty<string?> TitleProperty = AvaloniaProperty.Register<KnossosWindow, string?>(nameof(Title));
        public string? Title
        {
            get => GetValue(TitleProperty);
            set => SetValue(TitleProperty, value);
        }

        public static readonly StyledProperty<bool> CanCloseProperty = AvaloniaProperty.Register<KnossosWindow, bool>(nameof(CanClose), defaultValue: true);
        public bool CanClose
        {
            get => GetValue(CanCloseProperty);
            set => SetValue(CanCloseProperty, value);
        }

        public static readonly StyledProperty<bool> CanResizeProperty = AvaloniaProperty.Register<KnossosWindow, bool>(nameof(CanResize), defaultValue: true);
        public bool CanResize
        {
            get => GetValue(CanResizeProperty);
            set => SetValue(CanResizeProperty, value);
        }

        public static readonly StyledProperty<WindowIcon?> IconProperty = AvaloniaProperty.Register<KnossosWindow, WindowIcon?>(nameof(Icon));
        public WindowIcon? Icon 
        { 
            get => GetValue(IconProperty); 
            set => SetValue(IconProperty, value); 
        }

        public static readonly StyledProperty<WindowStartupLocation> WindowStartupLocationProperty = AvaloniaProperty.Register<KnossosWindow, WindowStartupLocation>(nameof(WindowStartupLocation), WindowStartupLocation.CenterOwner);
        public WindowStartupLocation WindowStartupLocation
        {
            get => GetValue(WindowStartupLocationProperty);
            set => SetValue(WindowStartupLocationProperty, value);
        }

        public static readonly StyledProperty<SizeToContent> SizeToContentProperty = AvaloniaProperty.Register<KnossosWindow, SizeToContent>(nameof(SizeToContent), SizeToContent.Manual);
        public SizeToContent SizeToContent
        {
            get => GetValue(SizeToContentProperty);
            set => SetValue(SizeToContentProperty, value);
        }

        public static readonly StyledProperty<bool> TopmostProperty = AvaloniaProperty.Register<KnossosWindow, bool>(nameof(Topmost), false);
        public bool Topmost
        {
            get => GetValue(TopmostProperty);
            set => SetValue(TopmostProperty, value);
        }

        // Dialog result
        public object? DialogResult { get; set; }

        // Window events
        public event EventHandler? Opened;
        public event EventHandler<CancelEventArgs>? Closing;
        public event EventHandler? Closed;

        // Window-like API
        public void Show() => _ = ShowInternalAsync(KnUtils.GetTopLevel(), isDialog: false);
        public void Show(Window owner) => _ = ShowInternalAsync(owner, isDialog: false);
        public Task ShowDialog() => ShowInternalAsync(KnUtils.GetTopLevel(), isDialog: true);
        public Task ShowDialog<T>(Window? owner) => ShowInternalAsync(owner, isDialog: true);
        protected virtual Task<bool> OnClosingAsync() => Task.FromResult(true);

        private Window? _hostWindow;                 // desktop host
        private ContentPresenter? _overlayHost;      // android host
        private TaskCompletionSource<object?>? _tcs; // ShowDialog()

        //Close the window and run OnClosing
        public async void Close()
        {
            var ce = new CancelEventArgs();
            if (!CanClose) ce.Cancel = true;
            Closing?.Invoke(this, ce);
            if (ce.Cancel) return;

            var ok = await OnClosingAsync().ConfigureAwait(true);
            if (!ok) return;

            //desktop
            if (_hostWindow != null)
            {
                _hostWindow.Close();
                return;
            }

            //android
            if (_overlayHost != null)
            {
                DialogHost.Hide(this, _overlayHost);
                _overlayHost.IsHitTestVisible = false;
                _overlayHost = null;
                _tcs?.TrySetResult(DialogResult);
                _tcs = null;
                Closed?.Invoke(this, EventArgs.Empty);
            }
        }

        // Add Window-like title and close button to the supplied view
        private Control BuildOverlayChrome(Control body)
        {
            var card = new Border
            {
                Background = new Avalonia.Media.SolidColorBrush(Avalonia.Media.Colors.Black),
                CornerRadius = new CornerRadius(10),
                Padding = new Thickness(0),
                Margin = new Thickness(20),
                HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Stretch,
                VerticalAlignment = Avalonia.Layout.VerticalAlignment.Stretch
            };

            var layout = new Grid
            {
                RowDefinitions = new RowDefinitions("60, *")
            };

            var titleBar = new Grid
            {
                Background = new Avalonia.Media.SolidColorBrush(Avalonia.Media.Color.FromRgb(240, 240, 240))
            };
            titleBar.ColumnDefinitions = new ColumnDefinitions("*, Auto");

            // title
            var titleText = new TextBlock
            {
                Margin = new Thickness(8),
                FontWeight = Avalonia.Media.FontWeight.SemiBold,
                Foreground = SolidColorBrush.Parse("Black"),
                Text = Title
            };

            // close button
            var closeBtn = new Button
            {
                Content = "✕",
                Width = 60,
                Height = 30,
                Background = SolidColorBrush.Parse("Red"),
                Margin = new Thickness(0, -18, 6, 6)
            };
            closeBtn.Click += (_, __) => Close();

            titleBar.Children.Add(titleText);
            Grid.SetColumn(closeBtn, 1);
            titleBar.Children.Add(closeBtn);

            // title bar
            Grid.SetRow(titleBar, 0);
            layout.Children.Add(titleBar);

            // view
            Grid.SetRow(body, 1);
            body.HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Stretch;
            body.VerticalAlignment = Avalonia.Layout.VerticalAlignment.Stretch;
            layout.Children.Add(body);

            card.Child = layout;
            return card;
        }

        // Open the window, creates a window in runtime and attaches the view to it
        // or display it on the mainview via dialoghost
        private async Task ShowInternalAsync(TopLevel? owner, bool isDialog)
        {
            if (KnUtils.IsAndroid || KnUtils.IsBrowser)
            {
                _tcs = new TaskCompletionSource<object?>(TaskCreationOptions.RunContinuationsAsynchronously);
                var chrome = BuildOverlayChrome(this);
                _overlayHost = DialogHost.Show(chrome, onDismiss: () => Close());
                Opened?.Invoke(this, EventArgs.Empty);

                if (isDialog)
                    await _tcs.Task.ConfigureAwait(false);

                return;
            }

            // Desktop, create a window
            var w = new Window
            {
                Content = this,
                Title = Title ?? string.Empty,
                SizeToContent = SizeToContent,
                Topmost = Topmost,
            };

            if (!double.IsNaN(Width) && Width > 0) w.Width = Width;
            if (!double.IsNaN(Height) && Height > 0) w.Height = Height;
            if (MinWidth > 0) w.MinWidth = MinWidth;
            if (MinHeight > 0) w.MinHeight = MinHeight;
            w.WindowStartupLocation = WindowStartupLocation;

            w.Closing += async (_, e) =>
            {
                var ce = new CancelEventArgs();
                if (!CanClose) ce.Cancel = true;
                Closing?.Invoke(this, ce);
                if (ce.Cancel) { e.Cancel = true; return; }

                var ok = await OnClosingAsync().ConfigureAwait(true);
                if (!ok) { e.Cancel = true; return; }
            };

            w.Opened += (_, __) => Opened?.Invoke(this, EventArgs.Empty);
            w.Closed += (_, __) => { Closed?.Invoke(this, EventArgs.Empty); _hostWindow = null; };

            _hostWindow = w;

            if (isDialog && owner is Window ownerWin)
                await w.ShowDialog(ownerWin);
            else if (isDialog && MainWindow.instance != null)
                await w.ShowDialog(MainWindow.instance);
            else
                w.Show();
        }
    }
}
