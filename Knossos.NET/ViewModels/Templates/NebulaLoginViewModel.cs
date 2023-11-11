using Avalonia.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using Knossos.NET.Models;
using Knossos.NET.Views;

namespace Knossos.NET.ViewModels
{
    public partial class NebulaLoginViewModel : ViewModelBase
    {
        [ObservableProperty]
        internal string title = "Nebula Login";

        [ObservableProperty]
        internal bool userLoggedIn = false;

        [ObservableProperty]
        internal bool registerNewUser = false;

        [ObservableProperty]
        internal string? userName = string.Empty;

        [ObservableProperty]
        internal string? userPass = string.Empty;

        [ObservableProperty]
        internal string userEmail = string.Empty;

        [ObservableProperty]
        internal string editableIDs = string.Empty;

        public async void UpdateUI()
        {
            UserLoggedIn = Nebula.userIsLoggedIn;
            RegisterNewUser = false;
            UserName = Nebula.userName;
            UserPass = Nebula.userPass;
            if(UserLoggedIn) 
            { 
                var ids = await Nebula.GetEditableModIDs().ConfigureAwait(false);
                if (ids != null)
                {
                    Dispatcher.UIThread.Invoke(() =>
                    {
                        EditableIDs = string.Join(", ", ids);
                    });
                }
            }
        }

        internal async void LogIn()
        {
            if (string.IsNullOrEmpty(UserName) || string.IsNullOrEmpty(UserPass) || string.IsNullOrWhiteSpace(UserName) || string.IsNullOrWhiteSpace(UserPass))
            {
                await MessageBox.Show(MainWindow.instance!, "Username and password are required fields.", "Login error", MessageBox.MessageBoxButtons.OK);
                return;
            }
            var result = await Nebula.Login(UserName, UserPass);
            if (!result)
            {
                await MessageBox.Show(MainWindow.instance!, "Login failed, the user or password may be incorrect.", "Login error", MessageBox.MessageBoxButtons.OK);
            }
            else
            {
                UpdateUI();
                var ids = await Nebula.GetEditableModIDs();
                if(ids != null)
                {
                    EditableIDs = string.Join(", ", ids);
                }
            }
        }

        internal void LogOff()
        {
            Nebula.LogOff();
            UpdateUI();
        }

        internal void SwitchToRegister()
        {
            RegisterNewUser = true;
            UserEmail = string.Empty;
            UserPass = string.Empty;
            UserName = string.Empty;
        }

        internal void SwitchToLogin()
        {
            UpdateUI();
        }

        internal async void Register()
        {
            if(string.IsNullOrEmpty(UserName) || string.IsNullOrEmpty(UserPass) || string.IsNullOrEmpty(UserEmail) ||
               string.IsNullOrWhiteSpace(UserName) || string.IsNullOrWhiteSpace(UserPass) || string.IsNullOrWhiteSpace(UserEmail))
            {
                await MessageBox.Show(MainWindow.instance!, "Username, password and email are all required fields.", "Register error", MessageBox.MessageBoxButtons.OK);
                return;
            }
            if(UserPass.Length < 5 || UserName.Length < 5)
            {
                await MessageBox.Show(MainWindow.instance!, "The mininum length for password and username is 6.", "Register error", MessageBox.MessageBoxButtons.OK);
                return;
            }
            if(!KnUtils.IsValidEmail(UserEmail))
            {
                await MessageBox.Show(MainWindow.instance!, "Email: "+UserEmail+ " is not a valid email.", "Register error", MessageBox.MessageBoxButtons.OK);
                return;
            }
            var result = await Nebula.Register(UserName, UserPass, UserEmail);
            if (result == "ok")
            {
                await MessageBox.Show(MainWindow.instance!, "You need to activate your user before you can log in. An an account activation email has sent to your email address.", "Register successfull", MessageBox.MessageBoxButtons.OK);
                UpdateUI();
            }
            else
            {
                //The only way this can fail at this point is username
                await MessageBox.Show(MainWindow.instance!, "An error has ocurred while registering a new username. Reason: "+result, "Register error", MessageBox.MessageBoxButtons.OK);
            }
        }

        internal async void Reset() 
        {
            if (string.IsNullOrEmpty(UserName) || string.IsNullOrWhiteSpace(UserName))
            {
                await MessageBox.Show(MainWindow.instance!, "Username is a required field.", "Reset password", MessageBox.MessageBoxButtons.OK);
                return;
            }
            var result = await Nebula.Reset(UserName);
            if (result == "ok")
            {
                await MessageBox.Show(MainWindow.instance!, "A password reset link has been sent to your email.", "Reset password", MessageBox.MessageBoxButtons.OK);
                UpdateUI();
            }
            else
            {
                //The only way this can fail at this point is username
                await MessageBox.Show(MainWindow.instance!, "An error has ocurred while requesting password reset. Reason: " + result, "Reset password", MessageBox.MessageBoxButtons.OK);
            }
        }
    }
}
