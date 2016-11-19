using Sample.Definition;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Windows.ApplicationModel.Background;

namespace Sample.Component
{
    public sealed class AppendPlugin : IBackgroundTask
    {
        private IBackgroundTask internalTask = new AppendIntern();
        public void Run(IBackgroundTaskInstance taskInstance)
         => this.internalTask.Run(taskInstance);
    }

    internal class AppendIntern : StringPluginsWithOptions
    {

        protected override Task<string> ExecuteAsync(string input, Options options, IProgress<double> progress, CancellationToken cancelToken)
        {
            var revereseOptions = new AppendPluginOptions(options);

            return Task.FromResult(input + revereseOptions.Appending.Value ?? "");
        }


        protected override Task<Options> GetOptions()
            => Task.FromResult<Options>(new AppendPluginOptions());

        protected override Guid GetOptionsGuid() => AppendPluginOptions.ID;

    }
}
