using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Xml;
using Nito.AsyncEx;
using Windows.ApplicationModel.AppService;
using Windows.ApplicationModel.Background;
using Windows.Foundation.Collections;

namespace AppExtensionService
{
    [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
    public abstract class AbstractBaseExtension<TOut> : IBackgroundTask
    {

        internal AbstractBaseExtension(bool useSyncronisationContext)
        {
            this.useSyncronisationContext = useSyncronisationContext;
        }

        internal const string START_KEY = "Start";
        internal const string PROGRESS_KEY = "Progress";
        internal const string CANCEL_KEY = "Cancel";
        internal const string OPTIONS_REQUEST_KEY = "OptionRequested";
        internal const string ID_KEY = "Id";
        internal const string RESULT_KEY = "Result";
        internal const string ERROR_KEY = "Error";
        internal const string OPTION_KEY = "Option";

        private BackgroundTaskDeferral dereffal;
        private Dictionary<Guid, CancellationTokenSource> idDirectory = new Dictionary<Guid, CancellationTokenSource>();
        private readonly bool useSyncronisationContext;
        private AsyncContextThread worker;

        void IBackgroundTask.Run(IBackgroundTaskInstance taskInstance)
        {
            if (useSyncronisationContext)
                worker = new Nito.AsyncEx.AsyncContextThread();


            dereffal = taskInstance.GetDeferral();

            var details = taskInstance.TriggerDetails as AppServiceTriggerDetails;
            details.AppServiceConnection.RequestReceived += AppServiceConnection_RequestReceived;
            details.AppServiceConnection.ServiceClosed += AppServiceConnection_ServiceClosed; ;
            taskInstance.Canceled += TaskInstance_Canceled;

        }

        private void TaskInstance_Canceled(IBackgroundTaskInstance sender, BackgroundTaskCancellationReason reason)
        {
            worker?.Dispose();
            dereffal?.Complete();
            dereffal = null;
        }

        private void AppServiceConnection_ServiceClosed(AppServiceConnection sender, AppServiceClosedEventArgs args)
        {
            worker?.Dispose();
            dereffal?.Complete();
            dereffal = null;
        }

        private async void AppServiceConnection_RequestReceived(AppServiceConnection sender, AppServiceRequestReceivedEventArgs args)
        {
            var messageDereffal = args.GetDeferral();
            try
            {
                // if we have a worker we use that.
                if (worker != null)
                    await worker.Factory.Run(() => RequestRecived(sender, args));
                else
                    await RequestRecived(sender, args);

            }
            finally
            {
                messageDereffal.Complete();
            }
        }

        internal virtual async Task RequestRecived(AppServiceConnection sender, AppServiceRequestReceivedEventArgs args)
        {
            if (args.Request.Message.ContainsKey(START_KEY))
                await StartMessage(sender, args);
            else if (args.Request.Message.ContainsKey(CANCEL_KEY))
                CancelMessage(sender, args);
        }

        private void CancelMessage(AppServiceConnection sender, AppServiceRequestReceivedEventArgs args)
        {

            if (!args.Request.Message.ContainsKey(CANCEL_KEY))
                return;

            if (!args.Request.Message.ContainsKey(ID_KEY))
                return;


            var id = (Guid)args.Request.Message[ID_KEY];
            var shouldCancel = (bool)args.Request.Message[CANCEL_KEY];
            if (!shouldCancel)
                return;

            if (!idDirectory.ContainsKey(id))
                return;

            idDirectory[id].Cancel();

        }


        private async Task StartMessage(AppServiceConnection sender, AppServiceRequestReceivedEventArgs args)
        {
            Guid? id = null;
            try
            {
                id = (Guid)args.Request.Message[ID_KEY];
                if (idDirectory.ContainsKey(id.Value))
                    throw new Exceptions.AppExtensionServiceException("Start was already send.");
                var cancellationTokenSource = new CancellationTokenSource();
                idDirectory.Add(id.Value, cancellationTokenSource);

                object output = await PerformStart(sender, args, id, cancellationTokenSource);

                var outputString = Helper.Serilize(output);
                var valueSet = new Windows.Foundation.Collections.ValueSet();
                valueSet.Add(ID_KEY, id.Value);
                valueSet.Add(RESULT_KEY, outputString);
                await args.Request.SendResponseAsync(valueSet);

            }
            catch (Exception e)
            {
                var valueSet = new ValueSet();
                valueSet.Add(ERROR_KEY, e.Message);
                valueSet.Add(ID_KEY, id.Value);
                await args.Request.SendResponseAsync(valueSet);
            }
            finally
            {
                if (id.HasValue)
                    idDirectory.Remove(id.Value);
            }
        }

        internal abstract Task<TOut> PerformStart(AppServiceConnection sender, AppServiceRequestReceivedEventArgs args, Guid? id, CancellationTokenSource cancellationTokenSource);

    }
}
