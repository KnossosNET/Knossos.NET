using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Controls.Presenters;
using Avalonia.Controls.Primitives;
using Avalonia.Markup.Xaml;
using Avalonia.Threading;
using Avalonia.VisualTree;
using Knossos.NET.ViewModels;
using System;
using System.Linq;
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
            YesNoCancel,
            Continue,
            ContinueCancel,
            Details,
            DetailsOKCancel,
            DetailsContinueCancel,
            DontWarnAgainOK,
            ContinueCancelSkipVersion,
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
            SkipVersion
        }

        public MessageBox()
        {
            AvaloniaXamlLoader.Load(this);//not thread safe!
        }

        //Messagebox is not thread safe!
        public static Task<MessageBoxResult> Show(Window? parent, string text, string title, MessageBoxButtons buttons)
        {
            if (KnUtils.IsAndroid || KnUtils.IsBrowser)
            {
                // Redirect to the non-window version
                return MessageBoxView.ShowAsync(text, title, buttons).ContinueWith(t => t.Result);
            }
            // Open a window to show the message
            return ShowWindow(parent, text, title, buttons);
        }

        private static Task<MessageBoxResult> ShowWindow(Window? parent, string text, string title, MessageBoxButtons buttons)
        {
            var msgbox = new MessageBox()
            {
                Title = title
            };

            msgbox.FindControl<TextBlock>("Text")!.Text = text;

            var buttonPanel = msgbox.FindControl<StackPanel>("Buttons")!;

            var result = MessageBoxResult.OK;

            void AddButton(string caption, MessageBoxResult r, bool def = false, string? classes = null, double? buttonWidth = null)
            {
                var button = new Button { Content = caption, Width = 100 };
                button.Click += (_, __) =>
                {
                    result = r;
                    msgbox.Close();
                };
                if (classes != null)
                {
                    button.Classes.Add(classes);
                }
                if (buttonWidth.HasValue)
                {
                    button.Width = buttonWidth.Value;
                }
                buttonPanel.Children.Add(button);
                if (def)
                {
                    result = r;
                }
            }

            ButtonCreation(AddButton, buttons);

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

        /// <summary>
        /// Creates buttons for both versions of the messagebox system, do not call directly
        /// </summary>
        internal static void ButtonCreation(Action<string, MessageBoxResult, bool, string?, double?> addButtonMethod, MessageBoxButtons buttons)
        {
            if (buttons == MessageBoxButtons.OK || buttons == MessageBoxButtons.OKCancel || buttons == MessageBoxButtons.DetailsOKCancel)
            {
                addButtonMethod("OK", MessageBoxResult.OK, true, "Accept", null);
            }

            if (buttons == MessageBoxButtons.YesNo || buttons == MessageBoxButtons.YesNoCancel)
            {
                addButtonMethod("Yes", MessageBoxResult.Yes, false, "Accept", null);
                addButtonMethod("No", MessageBoxResult.No, true, "Cancel", null);
            }

            if (buttons == MessageBoxButtons.Continue || buttons == MessageBoxButtons.ContinueCancel || buttons == MessageBoxButtons.DetailsContinueCancel || buttons == MessageBoxButtons.ContinueCancelSkipVersion)
            {
                addButtonMethod("Continue", MessageBoxResult.Continue, false, "Accept", null);
            }

            if (buttons == MessageBoxButtons.OKCancel || buttons == MessageBoxButtons.YesNoCancel || buttons == MessageBoxButtons.ContinueCancel || buttons == MessageBoxButtons.DetailsOKCancel || buttons == MessageBoxButtons.DetailsContinueCancel || buttons == MessageBoxButtons.ContinueCancelSkipVersion)
            {
                addButtonMethod("Cancel", MessageBoxResult.Cancel, true, "Cancel", null);
            }

            if (buttons == MessageBoxButtons.Details || buttons == MessageBoxButtons.DetailsOKCancel || buttons == MessageBoxButtons.DetailsContinueCancel)
            {
                addButtonMethod("Details", MessageBoxResult.Details, false, "Option", null);
            }

            if (buttons == MessageBoxButtons.DontWarnAgainOK)
            {
                addButtonMethod("OK", MessageBoxResult.OK, true, "Accept", null);
                addButtonMethod("Don't warn again", MessageBoxResult.DontWarnAgain, false, "Option", 150);
            }

            if (buttons == MessageBoxButtons.ContinueCancelSkipVersion)
            {
                addButtonMethod("Skip this version", MessageBoxResult.SkipVersion, false, "Option", 150);
            }
        }
    }
}