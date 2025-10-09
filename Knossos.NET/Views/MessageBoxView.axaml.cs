using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using System;
using System.Threading.Tasks;
using static Knossos.NET.Views.MessageBox;

namespace Knossos.NET.Views
{
    /// <summary>
    /// Single view version of the Knossos Messagebox system
    /// Used to display a message on a overlay rather than a window
    /// </summary>
    public partial class MessageBoxView : UserControl
    {
        public string Title { get => _title.Text!; set => _title.Text = value; }
        public string Text { get => _body.Text!; set => _body.Text = value; }

        private readonly TextBlock _title;
        private readonly TextBlock _body;
        private readonly StackPanel _buttonsHostPanel;

        public MessageBoxView()
        {
            AvaloniaXamlLoader.Load(this);
            _title = this.FindControl<TextBlock>("TitleText")!;
            _body = this.FindControl<TextBlock>("BodyText")!;
            _buttonsHostPanel = this.FindControl<StackPanel>("ButtonsHostPanel")!;
        }

        /// <summary>
        /// Displays a message on a overlay over the main view
        /// Usefull for single view OS like Android or WASM
        /// Normally you dont want to call this directly unless you really want a message
        /// on the overlay. Use MessageBox.Show() instead.
        /// </summary>
        /// <param name="text"></param>
        /// <param name="title"></param>
        /// <param name="buttons"></param>
        /// <returns></returns>
        public static Task<MessageBoxResult> ShowAsync(string text, string title, MessageBoxButtons buttons)
        {
            var tcs = new TaskCompletionSource<MessageBoxResult>();
            var view = new MessageBoxView { Title = title, Text = text };

            void AddButton(string caption, MessageBoxResult r, bool isDefault = false, string? classes = null, double? width = null)
            {
                var b = new Button { Content = caption, MinWidth = width ?? 100 };
                if (!string.IsNullOrEmpty(classes)) b.Classes.Add(classes!);
                b.Click += (_, __) => { tcs.TrySetResult(r); DialogHost.Hide(view); };
                view._buttonsHostPanel.Children.Add(b);
                if (isDefault) view.AttachedToVisualTree += (_, __) => b.Focus();
            };

            MessageBox.ButtonCreation(AddButton, buttons);

            DialogHost.Show(view, onDismiss: () => tcs.TrySetResult(MessageBoxResult.Cancel));
            return tcs.Task;
        }
    }
}