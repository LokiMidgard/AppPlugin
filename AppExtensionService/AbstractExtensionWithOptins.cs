using AppExtensionService.ExtensionList;
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
    /// <summary>
    /// Abstract class that can be implemented to define a simple Plugin that provides one Function that takes an additional option argument.
    /// </summary>
    /// <typeparam name="TIn">The Type that will be passed to the funtion. (Must have a valid <seealso cref="DataContractAttribute"/> )</typeparam>
    /// <typeparam name="TOut">The return type of the function. (Must have a valid <seealso cref="DataContractAttribute"/> )</typeparam>
    /// <typeparam name="TOption">The Type that will be used to pass the options to f.</typeparam>
    /// <typeparam name="TProgress">The type that will be used to report progress. (Must have a valid <seealso cref="DataContractAttribute"/> )</typeparam>
    public abstract class AbstractExtension<TIn, TOut, TOption, TProgress> : AbstractBaseExtension<TOut>
    {


        /// <summary>
        /// Instanziate the Plugin.
        /// </summary>
        /// <remarks>
        /// Normaly an AppService uses its own process without UI. It also does not provide a SyncronisationContext. This results that async/await calls will run in the ThreadPool. This includes the Progress report. If the Plugin spans many Tasks, progress will be reported with higher latency.
        /// </remarks>
        /// <param name="useSyncronisationContext">Discrips if the code should be called using a SyncronisationContext.</param>
        public AbstractExtension(bool useSyncronisationContext = true) : base(useSyncronisationContext)
        {

        }

        /// <summary>
        /// Returns an Object that Lists the Availaible Plugins.
        /// </summary>
        /// <typeparam name="T">The Type of The Extension.</typeparam>
        /// <returns>The <see cref="AppExtensionService.ExtensionList<,,,,>"/></returns>
        public static ExtensionList<T, TIn, TOut, TOption, TProgress> List<T>() where T : AbstractExtension<TIn, TOut, TOption, TProgress>
        {
            return new ExtensionList<T, TIn, TOut, TOption, TProgress>();
        }

        /// <summary>
        /// Provides the Funktionality of this Plugin.
        /// </summary>
        /// <param name="input">The Input Parameter.</param>
        /// <param name="options">The options that contains additional configuration parameter for the function.</param>
        /// <param name="progress">The Progress that will report data to the Client.</param>
        /// <param name="cancelToken">The cancel token.</param>
        /// <returns>The result of the execution.</returns>
        protected abstract Task<TOut> Execute(TIn input, TOption options, IProgress<TProgress> progress, CancellationToken cancelToken);

        /// <summary>
        /// Generates the Prototype Options the client can manipulate and pass back to the Plugin.
        /// </summary>
        /// <returns>The prototype options.</returns>
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
