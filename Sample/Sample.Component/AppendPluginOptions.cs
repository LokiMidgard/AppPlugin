using System;
using Sample.Definition;

namespace Sample.Component
{
    internal class AppendPluginOptions : Options
    {
        internal static readonly Guid ID = new Guid("{ED1E7DD5-7059-461D-9FEE-A5F0F6C7CE5A}");
        private readonly AbstractOption[] abstractOption;

        public AppendPluginOptions()
        {
            this.abstractOption = new AbstractOption[]
            {
                new StringOption("To Append", "Defines the String that will be Append.") { Value = "[Please Set a Value]" }
            };
        }


        public AppendPluginOptions(Options o)
        {
            if (o.OptionsIdentifier != ID)
                throw new ArgumentException();
            this.abstractOption = o.Settings;
        }

        public StringOption Appending => this[0] as StringOption;

        public override Guid OptionsIdentifier => ID;

        protected override AbstractOption[] GetSettings() => this.abstractOption;
    }
}