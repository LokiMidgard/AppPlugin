using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Input;
using System.Xml;
using Windows.ApplicationModel;
using Windows.ApplicationModel.AppExtensions;
using Windows.ApplicationModel.AppService;
using Windows.Foundation.Collections;
using Windows.UI.Core;
using Windows.UI.Xaml.Media.Imaging;

namespace AppPlugin.PluginList
{

    public class PluginList<TIn, TOut, TOption, TProgress> : AbstractPluginList<TOut, PluginList< TIn, TOut, TOption, TProgress>.PluginProvider>
    {

        internal PluginList(string pluginName) : base(pluginName)
        {

        }

        public new sealed class PluginProvider : AbstractPluginList< TOut, PluginProvider>.PluginProvider, IPlugin<TIn, TOut, TOption, TProgress>
        {
            public Task<TOption> PrototypeOptions { get; }

            internal PluginProvider(AppExtension ext, string serviceName, BitmapImage logo) : base(ext, serviceName, logo)
            {
                PrototypeOptions = GetPlugin(null, default(CancellationToken)).ContinueWith(x => x.Result.RequestOptions()).Unwrap();
            }


            private Task<PluginConnection> GetPlugin(IProgress<TProgress> progress, CancellationToken cancelTokem) => PluginConnection.Create(serviceName, Extension, progress, cancelTokem);
            
            public async Task<TOut> Execute(TIn input, TOption options, IProgress<TProgress> progress = null, CancellationToken cancelTokem = default(CancellationToken))
            {
                using (var plugin = await GetPlugin(progress, cancelTokem))
                    return await plugin.Execute(input, options);
            }
        }

        internal override PluginProvider CreatePluginProvider(AppExtension ext, string serviceName, BitmapImage logo)
                    => new PluginProvider(ext, serviceName, logo);




        private sealed class PluginConnection : IDisposable
        {
            private readonly AppServiceConnection connection;
            private bool isDisposed;
            private readonly IProgress<TProgress> progress;
            private readonly CancellationToken cancelTokem;
            private readonly Guid id = Guid.NewGuid();


            private PluginConnection(AppServiceConnection connection, IProgress<TProgress> progress, CancellationToken cancelTokem = default(CancellationToken))
            {
                this.connection = connection;
                connection.ServiceClosed += Connection_ServiceClosed;
                connection.RequestReceived += Connection_RequestReceived;
                this.progress = progress;
                this.cancelTokem = cancelTokem;
                cancelTokem.Register(Canceld);
            }

            private async void Canceld()
            {
                var valueSet = new ValueSet();

                valueSet.Add(AbstractPlugin<object, object, object>.ID_KEY, id);
                valueSet.Add(AbstractPlugin<object, object, object>.CANCEL_KEY, true);

                await connection.SendMessageAsync(valueSet);
            }

            public async Task<TOption> RequestOptions()
            {
                if (isDisposed)
                    throw new ObjectDisposedException(this.ToString());


                var inputs = new ValueSet();
                inputs.Add(AbstractPlugin<object, object, object>.OPTIONS_REQUEST_KEY, true);

                AppServiceResponse response = await connection.SendMessageAsync(inputs);

                if (response.Status != AppServiceResponseStatus.Success)
                    throw new Exceptions.ConnectionFailureException(response.Status);
                if (response.Message.ContainsKey(AbstractPlugin<object, object, object>.ERROR_KEY))
                    throw new Exceptions.PluginException(response.Message[AbstractPlugin<object, object, object>.ERROR_KEY] as string);
                if (!response.Message.ContainsKey(AbstractPlugin<object, object, object>.RESULT_KEY))
                    return default(TOption);
                var resultString = response.Message[AbstractPlugin<object, object, object>.RESULT_KEY] as string;

                if (String.IsNullOrWhiteSpace(resultString))
                    return default(TOption);

                var output = Helper.DeSerilize<TOption>(resultString);

                return output;
            }

            private async void Connection_RequestReceived(AppServiceConnection sender, AppServiceRequestReceivedEventArgs args)
            {
                if (!args.Request.Message.ContainsKey(AbstractPlugin<object, object, object>.PROGRESS_KEY))
                    return;
                if (!args.Request.Message.ContainsKey(AbstractPlugin<object, object, object>.ID_KEY))
                    return;

                var id = (Guid)args.Request.Message[AbstractPlugin<object, object, object>.ID_KEY];
                if (this.id != id)
                    return;

                var progressString = args.Request.Message[AbstractPlugin<object, object, object>.PROGRESS_KEY] as string;

                var progress = Helper.DeSerilize<TProgress>(progressString);


                this.progress?.Report(progress);
                await args.Request.SendResponseAsync(new ValueSet());
            }

            private void Connection_ServiceClosed(AppServiceConnection sender, AppServiceClosedEventArgs args)
            {
                Dispose();
            }

            public static async Task<PluginConnection> Create(string serviceName, AppExtension appExtension, IProgress<TProgress> progress, CancellationToken cancelTokem = default(CancellationToken))
            {
                var connection = new AppServiceConnection();

                var pluginConnection = new PluginConnection(connection, progress, cancelTokem);
                connection.AppServiceName = serviceName;

                connection.PackageFamilyName = appExtension.Package.Id.FamilyName;

                AppServiceConnectionStatus status = await connection.OpenAsync();

                //If the new connection opened successfully we're done here
                if (status == AppServiceConnectionStatus.Success)
                {
                    return pluginConnection;
                }
                else
                {
                    //Clean up before we go
                    var exception = new Exceptions.ConnectionFailureException(status, connection);
                    connection.Dispose();
                    connection = null;
                    throw exception;
                }
            }

            public void Dispose()
            {
                if (isDisposed)
                    return;
                connection.Dispose();
                isDisposed = true;
            }

            public async Task<TOut> Execute(TIn input, TOption option)
            {
                if (isDisposed)
                    throw new ObjectDisposedException(this.ToString());

                string inputString = Helper.Serilize(input);
                string optionString = Helper.Serilize(option);

                var inputs = new ValueSet();
                inputs.Add(AbstractPlugin<object, object, object>.START_KEY, inputString);
                inputs.Add(AbstractPlugin<object, object, object>.OPTION_KEY, optionString);
                inputs.Add(AbstractPlugin<object, object, object>.ID_KEY, id);

                AppServiceResponse response = await connection.SendMessageAsync(inputs);

                if (response.Status != AppServiceResponseStatus.Success)
                    throw new Exceptions.ConnectionFailureException(response.Status);
                if (response.Message.ContainsKey(AbstractPlugin<object, object, object>.ERROR_KEY))
                    throw new Exceptions.PluginException(response.Message[AbstractPlugin<object, object, object>.ERROR_KEY] as string);
                if (!response.Message.ContainsKey(AbstractPlugin<object, object, object>.RESULT_KEY))
                    return default(TOut);
                var outputString = response.Message[AbstractPlugin<object, object, object>.RESULT_KEY] as string;

                if (String.IsNullOrWhiteSpace(outputString))
                    return default(TOut);

                var output = Helper.DeSerilize<TOut>(outputString);

                return output;

            }
        }
    }
}
