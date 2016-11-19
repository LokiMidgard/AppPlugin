using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.UI.Xaml;
using Plugin = AppPlugin.PluginList.PluginList<string, string, Sample.Definition.TransfareOptions, double>.PluginProvider;

namespace Sample.Consumer
{
    class Viewmodel : DependencyObject
    {

        public ObservableCollection<Plugin> Plugins { get; }
                  = new ObservableCollection<Plugin>();

        public Viewmodel()
        {
#pragma warning disable CS4014 // Da dieser Aufruf nicht abgewartet wird, wird die Ausführung der aktuellen Methode fortgesetzt, bevor der Aufruf abgeschlossen ist
            InitAsync();
#pragma warning restore CS4014 // Da dieser Aufruf nicht abgewartet wird, wird die Ausführung der aktuellen Methode fortgesetzt, bevor der Aufruf abgeschlossen ist
        }

        private async Task InitAsync()
        {
            await Task.Delay(5000);
            var list = await Definition.StringPluginsWithOptions.ListAsync(Definition.StringPluginsWithOptions.PLUGIN_NAME);

            foreach (var item in list.Plugins)
                this.Plugins.Add(item);

            (list.Plugins as INotifyCollectionChanged).CollectionChanged += async (sender, e) =>
            {
                await this.Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, () =>
                {
                    if (e.NewItems != null)
                        foreach (var item in e.NewItems.OfType<Plugin>())
                            this.Plugins.Add(item);
                    if (e.OldItems != null)
                        foreach (var item in e.OldItems.OfType<Plugin>())
                            this.Plugins.Remove(item);
                });
            };
        }
    }
}
