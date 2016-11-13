using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Xml;
using Windows.ApplicationModel.AppService;
using Windows.ApplicationModel.Background;
using Windows.Foundation.Collections;

namespace AppExtensionService
{
    public abstract class AbstractExtension<TIn, TOut, TOption, TProgress> : AbstractBaseExtension<TOut>
    {


        public AbstractExtension(bool useSyncronisationContext) : base(useSyncronisationContext)
        {

        }

        public static ExtensionFinder<T, TIn, TOut, TOption, TProgress> Find<T>() where T : AbstractExtension<TIn, TOut, TOption, TProgress>
        {
            return new ExtensionFinder<T, TIn, TOut, TOption, TProgress>();
        }

        protected abstract Task<TOut> Execute(TIn input, TOption options, IProgress<TProgress> progress, CancellationToken cancelToken);
        protected abstract Task<TOption> GetDefaultOptions();


        internal override async Task RequestRecived(AppServiceConnection sender, AppServiceRequestReceivedEventArgs args)
        {

            if (args.Request.Message.ContainsKey(OPTIONS_REQUEST_KEY))
            {
                var options = await GetDefaultOptions();

                var optionString = Helper.Serilize(options);
                var valueSet = new ValueSet();
                valueSet.Add(RESULT_KEY, optionString);
                await args.Request.SendResponseAsync(valueSet);

            }
            else
                await base.RequestRecived(sender, args);
        }


        internal override async Task<TOut> PerformStart(AppServiceConnection sender, AppServiceRequestReceivedEventArgs args, Guid? id, CancellationTokenSource cancellationTokenSource)
        {
            var inputString = args.Request.Message[START_KEY] as String;
            var optionString = args.Request.Message[OPTION_KEY] as String;

            var input = Helper.DeSerilize<TIn>(inputString);
            var options = Helper.DeSerilize<TOption>(optionString);

            var progress = new Progress<TProgress>(async r =>
            {
                var data = Helper.Serilize(r);
                var dataSet = new ValueSet();
                dataSet.Add(PROGRESS_KEY, data);
                dataSet.Add(ID_KEY, id);
                await sender.SendMessageAsync(dataSet);
            });

            var output = await Execute(input, options, progress, cancellationTokenSource.Token);
            return output;
        }

    }
}
