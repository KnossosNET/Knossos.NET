using Avalonia.Controls;
using Avalonia.Controls.Templates;
using Knossos.NET.ViewModels;
using System;

namespace Knossos.NET
{
    public class ViewLocator : IDataTemplate
    {
        public Control Build(object? data)
        {
            if (data != null)
            {
                var name = data.GetType().FullName!.Replace("ViewModel", "View");
                var type = Type.GetType(name);

                if (type != null)
                {
                    return (Control)Activator.CreateInstance(type)!;
                }
                else
                {
                    try
                    {
                        if (data.GetType() == typeof(AxamlExternalContentViewModel))
                        {
                            var externalView = ((AxamlExternalContentViewModel)data).GetView();
                            if (externalView != null)
                            {
                                return externalView;
                            }
                            else
                            {
                                return new TextBlock { Text = "Unable to load the external Axamal data for external view : " + ((AxamlExternalContentViewModel)data).name };
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        Log.Add(Log.LogSeverity.Error, "ViewLocator", ex);
                        return new TextBlock { Text = "An error has ocurred opening the external Axamal data for : " + name + " Error:\n"+ ex.ToString() , TextWrapping = Avalonia.Media.TextWrapping.Wrap };
                    }

                    return new TextBlock { Text = "Not Found: " + name };
                }
            }
            else
            {
                return new TextBlock { Text = "Not Found" };
            }
        }

        public bool Match(object? data)
        {
            return data is ViewModelBase;
        }
    }
}
