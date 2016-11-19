using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Data;

namespace Sample.Consumer
{
    class OptionsSelector : DataTemplateSelector
    {
        public DataTemplate StringOptionTemplate { get; set; }
        public DataTemplate IntOptionTemplate { get; set; }

        protected override DataTemplate SelectTemplateCore(object item, DependencyObject container)
        {
            return SelectTemplate(item);
        }

        protected override DataTemplate SelectTemplateCore(object item)
        {
            switch (item)
            {
                case Definition.IntOption io:
                    return this.IntOptionTemplate;
                case Definition.StringOption so:
                    return this.StringOptionTemplate;
                default:
                    return base.SelectTemplateCore(item);
            }
        }
    }
}
