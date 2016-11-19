using Sample.Definition;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace Sample.Component
{

    class ReversePluginOptions : Options
    {
        public static readonly Guid ID = new Guid("AB577183-9A64-47E0-B4B6-E8B5D309F537");

        private readonly AbstractOption[] abstractOption;

        public ReversePluginOptions()
        {
            this.abstractOption = new AbstractOption[]
            {
                new IntOption("Delay", "Sets the delay for each reverse operation in ms.", 0, 1000) { Value = 200 }
            };
        }


        public ReversePluginOptions(Options o)
        {
            if (o.OptionsIdentifier != ID)
                throw new ArgumentException();
            this.abstractOption = o.Settings;
        }

        public IntOption Delay => this[0] as IntOption;

        public override Guid OptionsIdentifier => ID;

        protected override AbstractOption[] GetSettings() => this.abstractOption;

    }
}
