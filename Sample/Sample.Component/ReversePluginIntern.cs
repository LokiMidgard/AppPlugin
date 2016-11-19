using Sample.Definition;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Threading;
using Windows.ApplicationModel.Background;

namespace Sample.Component
{

    public sealed class ReveresPlugin : IBackgroundTask
    {
        private IBackgroundTask internalTask = new ReversePluginIntern();
        public void Run(IBackgroundTaskInstance taskInstance)
         => this.internalTask.Run(taskInstance);
    }

    internal class ReversePluginIntern : StringPluginsWithOptions
    {

        protected override async Task<string> ExecuteAsync(string input, Options options, IProgress<double> progress, CancellationToken cancelToken)
        {
            var revereseOptions = new ReversePluginOptions(options);
            var b = new StringBuilder();
            for (int i = input.Length - 1; i >= 0; i--)
            {
                b.Append(input[i]);
                if (cancelToken.IsCancellationRequested)
                    break;
                progress.Report(b.Length / (double)input.Length);
                await Task.Delay(revereseOptions.Delay.Value);
            }
            return b.ToString();
        }


        protected override Task<Options> GetOptions()
            => Task.FromResult<Options>(new ReversePluginOptions());

        protected override Guid GetOptionsGuid() => ReversePluginOptions.ID;

    }
}
