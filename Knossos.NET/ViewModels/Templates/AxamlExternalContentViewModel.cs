using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using Knossos.NET.Converters;
using System;

namespace Knossos.NET.ViewModels
{
    public partial class AxamlExternalContentViewModel : ViewModelBase
    {
        private string externalPath;
        public string name = string.Empty;
        private UserControl? view;

        public AxamlExternalContentViewModel(string externalPath, string name) 
        {
            this.externalPath = externalPath;
            this.name = name;
        }

        /// <summary>
        /// Return the generated view element
        /// </summary>
        /// <returns>Usercontrol or null</returns>
        public UserControl? GetView()
        {
            if(view == null)
            {
                var conv = new TextFileToStringConverter();
                var result = conv.Convert(externalPath, typeof(string), null, null);
                if (result != null)
                {
                    view = (UserControl)AvaloniaRuntimeXamlLoader.Parse((string)result);
                }
                else
                {
                    Log.Add(Log.LogSeverity.Error, "AxamlExternalContentViewModel.LoadData()", "Unable to load remote axaml file " + externalPath);
                }
            }

            return view;
        }

        //Hooks so the CustomView to use internal functions

        /// <summary>
        /// Open Link in user Web Browser
        /// </summary>
        /// <param name="param"></param>
        internal void OpenLink (object? param)
        {
            try
            {
                if (param != null)
                {
                    KnUtils.OpenBrowserURL((string)param);
                }
            }
            catch (Exception ex)
            {
                Log.Add(Log.LogSeverity.Error, "AxamlExternalContentViewModel.OpenLink()", ex);
            }
        }
    }
}
