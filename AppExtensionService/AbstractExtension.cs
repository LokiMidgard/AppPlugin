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
    public abstract class AbstractExtension<TIn, TOut, TProgress> : AbstractBaseExtension<TOut>
    {
        public AbstractExtension(bool useSyncronisationContext) : base(useSyncronisationContext)
        {

        }

        public static ExtensionFinder<T, TIn, TOut, TProgress> Find<T>() where T : AbstractExtension<TIn, TOut, TProgress>
        {
            return new ExtensionFinder<T, TIn, TOut, TProgress>();
        }

        protected abstract Task<TOut> Execute(TIn input, IProgress<TProgress> progress, CancellationToken cancelToken);




        internal override async Task<TOut> PerformStart(AppServiceConnection sender, AppServiceRequestReceivedEventArgs args, Guid? id, CancellationTokenSource cancellationTokenSource)
        {
            var inputString = args.Request.Message[START_KEY] as String;

            var input = Helper.DeSerilize<TIn>(inputString);

            var progress = new Progress<TProgress>(async r =>
            {
                var data = Helper.Serilize(r);
                var dataSet = new ValueSet();
                dataSet.Add(PROGRESS_KEY, data);
                dataSet.Add(ID_KEY, id);
                await sender.SendMessageAsync(dataSet);
            });

            var output = await Execute(input, progress, cancellationTokenSource.Token);
            return output;
        }



    }
}
