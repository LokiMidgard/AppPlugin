
using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Linq;
using Nito.AsyncEx;
using System.Threading.Tasks;
using Sample.Definition;

namespace Sample.Test
{
    [TestClass]
    public class UnitTest1
    {
        [TestMethod]
        public void TestReversePlugin()
        {
            AsyncContext.Run(async () =>
            {
                var list = await Definition.StringPluginsWithOptions.ListAsync(Definition.StringPluginsWithOptions.PLUGIN_NAME);
              //  await Task.Delay(10000);
                var reversePlugin = list.Plugins.SingleOrDefault(x => x.Extension.DisplayName == "String Reverser");
                Assert.IsNotNull(reversePlugin, "No Plugin Found Be shure to deploy the Project Sample.Provider");

                var options = await reversePlugin.PrototypeOptions;

                Assert.IsNotNull(options);

                const string toReverse = "Test";
                var result = await reversePlugin.ExecuteAsync(toReverse, options);
                Assert.AreEqual(new String(toReverse.Reverse().ToArray()), result);

            });
        }

        [TestMethod]
        public void TestAppendPlugin()
        {
            AsyncContext.Run(async () =>
            {
                var list = await Definition.StringPluginsWithOptions.ListAsync(Definition.StringPluginsWithOptions.PLUGIN_NAME);
//await Task.Delay(10000);
                var reversePlugin = list.Plugins.SingleOrDefault(x => x.Extension.DisplayName == "String Appender");
                Assert.IsNotNull(reversePlugin, "No Plugin Found Be shure to deploy the Project Sample.Provider");

                var options = await reversePlugin.PrototypeOptions;

                Assert.IsNotNull(options);
                Assert.AreEqual(1, options.Count);
                Assert.IsInstanceOfType(options[0], typeof(StringOption));

                var strOptions = options[0] as StringOption;
                const string toAppend = "Appending";
                strOptions.Value = toAppend;

                const string input = "Test";
                var result = await reversePlugin.ExecuteAsync(input, options);
                Assert.AreEqual(input + toAppend, result);

            });
        }
    }
}
