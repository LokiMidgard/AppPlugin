using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;
using Plugin = AppPlugin.PluginList.PluginList<string, string, Sample.Definition.TransfareOptions, double>.PluginProvider;

// Die Elementvorlage "Leere Seite" wird unter https://go.microsoft.com/fwlink/?LinkId=402352&clcid=0x407 dokumentiert.

namespace Sample.Consumer
{
    /// <summary>
    /// Eine leere Seite, die eigenständig verwendet oder zu der innerhalb eines Rahmens navigiert werden kann.
    /// </summary>
    public sealed partial class MainPage : Page
    {
        public MainPage()
        {
            this.InitializeComponent();
        }

        private async void Button_Click(object sender, RoutedEventArgs e)
        {
            this.Progress.IsActive = true;
            this.List.IsEnabled = false;
            try
            {

                var b = sender as Button;
                var p = b.DataContext as Plugin;
                var o = await p.PrototypeOptions;
                var erg = await p.ExecuteAsync(this.Input.Text ?? "", o);
                this.Output.Text = erg;

            }
            finally
            {
                this.List.IsEnabled = true;

                this.Progress.IsActive = false;
            }
        }
    }
}
