using Avalonia.Controls;
using Avalonia.Media.Imaging;
using Knossos.NET.Converters;
using Knossos.NET.Models;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Knossos.NET.Views;

public partial class CustomHomeView : UserControl
{
    internal List<string> buttonUrls = new List<string>();

    public CustomHomeView()
    {
        InitializeComponent();

        //Generate Link Buttons
        try
        {
            if (CustomLauncher.HomeLinkButtons != null && CustomLauncher.HomeLinkButtons.Any())
            {
                var buttonPanel = this.FindControl<WrapPanel>("LinkButtons")!;
                if (buttonPanel != null)
                {
                    int index = 0;
                    foreach (var b in CustomLauncher.HomeLinkButtons)
                    {
                        var linkButton = new Button { Tag = index, Name = b.ToolTip };
                        if (b.IconPath != null)
                        {
                            var converter = new BitmapAssetValueConverter();
                            var bitmap = converter.Convert(b.IconPath, typeof(Bitmap), null, null);
                            if(bitmap != null)
                            {
                                linkButton.Content = new Image { Source = (Bitmap)bitmap, Width = 30, Height = 30 };
                            }
                        }

                        linkButton.Click += (_, __) =>
                        {
                            //This code runs when the button is clicked
                            try
                            {
                                if(linkButton.Tag != null)
                                {
                                    var url = buttonUrls[(int)linkButton.Tag];
                                    KnUtils.OpenBrowserURL(url);
                                }    
                            }
                            catch (Exception ex)
                            {
                                Log.Add(Log.LogSeverity.Error, "CustomHomeView.Constructor(LinkButtonClick)", ex);
                            }
                        };

                        buttonPanel.Children.Add(linkButton);
                        index++;
                        buttonUrls.Add(b.LinkURL);
                        ToolTip.SetTip(linkButton, b.ToolTip);
                    }
                }
                else
                {
                    Log.Add(Log.LogSeverity.Error, "CustomHomeView.Constructor()", "Unable to find LinkButtons panel.");
                }
            }
        }
        catch(Exception ex)
        {
            Log.Add(Log.LogSeverity.Error, "CustomHomeView.Constructor()", ex);
        }
    }
}