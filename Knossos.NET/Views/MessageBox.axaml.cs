using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using Avalonia;
using Avalonia.Layout;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Knossos.NET.Views
{
    /// <summary>
    /// Knossos Messagebox. Se muestra como tarjeta sobre el overlay (DialogHost),
    /// en la capa MessageBox para que quede por encima de cualquier ventana abierta.
    /// No es thread-safe: llamar desde el hilo de UI.
    /// </summary>
    public partial class MessageBox : UserControl
    {
        private readonly Border _card;

        public enum MessageBoxButtons
        {
            OK,
            OKCancel,
            YesNo,
            YesNoCancel,
            Continue,
            ContinueCancel,
            Details,
            DetailsOKCancel,
            DetailsContinueCancel,
            DontWarnAgainOK,
            ContinueCancelSkipVersion,
            InstallBuildModes,
        }

        public enum MessageBoxResult
        {
            OK,
            Cancel,
            Yes,
            No,
            Continue,
            Details,
            DontWarnAgain,
            SkipVersion,
            InstallNormal,
            InstallDeveloper
        }

        private readonly TextBlock _title;
        private readonly TextBlock _body;
        private readonly WrapPanel _buttonsHostPanel;
        private readonly ComboBox _librarySelector;

        public string Title { get => _title.Text!; set => _title.Text = value; }
        public string Text { get => _body.Text!; set => _body.Text = value; }

        public MessageBox()
        {
            AvaloniaXamlLoader.Load(this);
            _title = this.FindControl<TextBlock>("TitleText")!;
            _body = this.FindControl<TextBlock>("BodyText")!;
            _buttonsHostPanel = this.FindControl<WrapPanel>("ButtonsHostPanel")!;
            _librarySelector = this.FindControl<ComboBox>("LibrarySelector")!;
            _card = this.FindControl<Border>("CardBorder")!;
        }

        public void UseWindowChrome()
        {
            _card.Margin = new Thickness(0);
            _card.CornerRadius = new CornerRadius(0);
            _card.BorderThickness = new Thickness(0);
            _card.BoxShadow = default;
            _card.HorizontalAlignment = HorizontalAlignment.Stretch;
            _card.VerticalAlignment = VerticalAlignment.Stretch;
            _title.IsVisible = false;
        }

        public static async Task<MessageBoxResult> Show(Window? parent, string text, string title, MessageBoxButtons buttons)
        {
            var answer = await ShowCore(parent, text, title, buttons, null);
            return answer.Result;
        }

        public static Task<(MessageBoxResult Result, string? LibraryPath)> ShowWithLibrary(
            Window? parent, string text, string title, MessageBoxButtons buttons) =>
            ShowCore(parent, text, title, buttons, Knossos.GetLibraryPaths());

        private static Task<(MessageBoxResult Result, string? LibraryPath)> ShowCore(
            Window? parent, string text, string title, MessageBoxButtons buttons, IReadOnlyList<string>? libraries)
        {
            var tcs = new TaskCompletionSource<(MessageBoxResult Result, string? LibraryPath)>(
                TaskCreationOptions.RunContinuationsAsynchronously);
            var view = new MessageBox { Title = title, Text = text };
            if (libraries is { Count: > 0 })
            {
                view._librarySelector.ItemsSource = libraries;
                view._librarySelector.SelectedIndex = 0;
                view.FindControl<StackPanel>("LibraryPicker")!.IsVisible = libraries.Count > 1;
            }

            bool overlay = Knossos.globalSettings.singleViewMode || KnUtils.IsAndroid || KnUtils.IsBrowser;

            Window? window = overlay ? null : new Window
            {
                Title = title,
                Content = view,
                SizeToContent = SizeToContent.WidthAndHeight,
                MinWidth = 320,
                MaxWidth = 520,
                CanResize = false,
                ShowInTaskbar = false,
                WindowStartupLocation = WindowStartupLocation.CenterOwner,
            };

            Action dismiss = overlay
                ? () => DialogHost.Hide(view)
                : () => window!.Close();

            void AddButton(string caption, MessageBoxResult r, bool isDefault = false, string? classes = null, double? width = null)
            {
                var b = new Button { Content = caption, MinWidth = width ?? 100, Margin = new Thickness(4) };
                if (!string.IsNullOrEmpty(classes)) b.Classes.Add(classes!);
                b.Click += (_, __) =>
                {
                    tcs.TrySetResult((r, view._librarySelector.SelectedItem as string));
                    dismiss();
                };
                view._buttonsHostPanel.Children.Add(b);
                if (isDefault) view.AttachedToVisualTree += (_, __) => b.Focus();
            }

            ButtonCreation(AddButton, buttons);

            if (overlay)
            {
                if (DialogHost.Show(view, DialogHost.Layer.MessageBox,
                    onDismiss: () => { if (tcs.TrySetResult((MessageBoxResult.Cancel, null))) DialogHost.Hide(view); }) == null)
                    tcs.TrySetResult((MessageBoxResult.Cancel, null));
            }
            else
            {
                view.UseWindowChrome();
                window!.Closed += (_, __) => tcs.TrySetResult((MessageBoxResult.Cancel, null));

                if (parent != null && parent.IsVisible)
                    window.ShowDialog(parent);
                else
                    window.Show();
            }

            return tcs.Task;
        }

        /// <summary>
        /// Do not use externally
        /// </summary>
        internal static void ButtonCreation(Action<string, MessageBoxResult, bool, string?, double?> addButtonMethod, MessageBoxButtons buttons)
        {
            if (buttons == MessageBoxButtons.OK || buttons == MessageBoxButtons.OKCancel || buttons == MessageBoxButtons.DetailsOKCancel)
                addButtonMethod("OK", MessageBoxResult.OK, true, "Accept", null);

            if (buttons == MessageBoxButtons.YesNo || buttons == MessageBoxButtons.YesNoCancel)
            {
                addButtonMethod("Yes", MessageBoxResult.Yes, false, "Accept", null);
                addButtonMethod("No", MessageBoxResult.No, true, "Cancel", null);
            }

            if (buttons == MessageBoxButtons.Continue || buttons == MessageBoxButtons.ContinueCancel || buttons == MessageBoxButtons.DetailsContinueCancel || buttons == MessageBoxButtons.ContinueCancelSkipVersion)
                addButtonMethod("Continue", MessageBoxResult.Continue, false, "Accept", null);

            if (buttons == MessageBoxButtons.OKCancel || buttons == MessageBoxButtons.YesNoCancel || buttons == MessageBoxButtons.ContinueCancel || buttons == MessageBoxButtons.DetailsOKCancel || buttons == MessageBoxButtons.DetailsContinueCancel || buttons == MessageBoxButtons.ContinueCancelSkipVersion)
                addButtonMethod("Cancel", MessageBoxResult.Cancel, true, "Cancel", null);

            if (buttons == MessageBoxButtons.Details || buttons == MessageBoxButtons.DetailsOKCancel || buttons == MessageBoxButtons.DetailsContinueCancel)
                addButtonMethod("Details", MessageBoxResult.Details, false, "Option", null);

            if (buttons == MessageBoxButtons.DontWarnAgainOK)
            {
                addButtonMethod("OK", MessageBoxResult.OK, true, "Accept", null);
                addButtonMethod("Don't warn again", MessageBoxResult.DontWarnAgain, false, "Option", 150);
            }

            if (buttons == MessageBoxButtons.ContinueCancelSkipVersion)
                addButtonMethod("Skip this version", MessageBoxResult.SkipVersion, false, "Option", 150);

            if (buttons == MessageBoxButtons.InstallBuildModes)
            {
                addButtonMethod("Normal mode", MessageBoxResult.InstallNormal, true, "Accept", 120);
                addButtonMethod("Developer mode", MessageBoxResult.InstallDeveloper, false, "Option", 150);
                addButtonMethod("Cancel", MessageBoxResult.Cancel, false, "Cancel", null);
            }
        }
    }
}
