using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using Avalonia.Threading;
using System.Threading.Tasks;

namespace Knossos.NET.Views
{
    public partial class MessageBox : Window
    {
        public enum MessageBoxButtons
        {
            OK,
            OKCancel,
            YesNo,
            YesNoCancel
        }

        public enum MessageBoxResult
        {
            OK,
            Cancel,
            Yes,
            No
        }

        public MessageBox()
        {
            AvaloniaXamlLoader.Load(this);//not thread safe!
        }

        //Messagebox is not thread safe!
        public static Task<MessageBoxResult> Show(Window parent, string text, string title, MessageBoxButtons buttons)
        {
            var msgbox = new MessageBox()
            {
                Title = title
            };

            msgbox.FindControl<TextBlock>("Text").Text = text;

            var buttonPanel = msgbox.FindControl<StackPanel>("Buttons");

            var result = MessageBoxResult.OK;

            void AddButton(string caption, MessageBoxResult r, bool def = false)
            {                
                var button = new Button { Content = caption };
                button.Click += (_, __) => {
                    result = r;
                    msgbox.Close();
                };
                buttonPanel.Children.Add(button);
                if (def)
                {
                    result = r;
                }
            }

            if (buttons == MessageBoxButtons.OK || buttons == MessageBoxButtons.OKCancel)
            {
                AddButton("OK", MessageBoxResult.OK, true);
            }

            if (buttons == MessageBoxButtons.YesNo || buttons == MessageBoxButtons.YesNoCancel)
            {
                AddButton("Yes", MessageBoxResult.Yes);
                AddButton("No", MessageBoxResult.No, true);
            }

            if (buttons == MessageBoxButtons.OKCancel || buttons == MessageBoxButtons.YesNoCancel)
            {
                AddButton("Cancel", MessageBoxResult.Cancel, true);
            }

            var tcs = new TaskCompletionSource<MessageBoxResult>();
            msgbox.Closed += delegate { tcs.TrySetResult(result); };

            if (parent != null && parent.IsVisible)
            {
                msgbox.ShowDialog(parent);
            }
            else
            {
                msgbox.Show();
            }

            return tcs.Task;
        }
    }
}
