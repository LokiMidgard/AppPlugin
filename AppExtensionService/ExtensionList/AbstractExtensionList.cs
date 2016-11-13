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

namespace AppExtensionService.ExtensionList
{

    [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
    public abstract class AbstractExtensionList<T, TOut, TExtensionProvider>
        where T : AbstractBaseExtension<TOut>
        where TExtensionProvider : AbstractExtensionList<T, TOut, TExtensionProvider>.ExtensionProvider
    {
        private readonly CoreDispatcher dispatcher;
        private AppExtensionCatalog catalog;
        private const string SERVICE_KEY = "Service";

        private ObservableCollection<TExtensionProvider> extensions { get; } = new ObservableCollection<TExtensionProvider>();
        public ReadOnlyObservableCollection<TExtensionProvider> Extensions { get; }

        internal AbstractExtensionList()
        {
            dispatcher = Windows.UI.Core.CoreWindow.GetForCurrentThread().Dispatcher;
            Extensions = new ReadOnlyObservableCollection<TExtensionProvider>(extensions);
        }

        public async Task Init()
        {
            catalog = Windows.ApplicationModel.AppExtensions.AppExtensionCatalog.Open(typeof(T).FullName);

            // set up extension management events
            catalog.PackageInstalled += Catalog_PackageInstalled;
            catalog.PackageUpdated += Catalog_PackageUpdated;
            catalog.PackageUninstalling += Catalog_PackageUninstalling;
            catalog.PackageUpdating += Catalog_PackageUpdating;
            catalog.PackageStatusChanged += Catalog_PackageStatusChanged;



            // Scan all extensions

            await FindAllExtensions();
        }



        private async Task FindAllExtensions()
        {
            // load all the extensions currently installed
            var extensions = await catalog.FindAllAsync();

            foreach (var ext in extensions)
            {
                // load this extension
                await LoadExtension(ext);
            }
        }


        private async void Catalog_PackageInstalled(AppExtensionCatalog sender, AppExtensionPackageInstalledEventArgs args)
        {
            await dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, async () =>
            {
                foreach (AppExtension ext in args.Extensions)
                    await LoadExtension(ext);
            });
        }


        // package has been updated, so reload the extensions

        private async void Catalog_PackageUpdated(AppExtensionCatalog sender, AppExtensionPackageUpdatedEventArgs args)
        {
            await dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, async () =>
            {
                foreach (AppExtension ext in args.Extensions)
                    await LoadExtension(ext);
            });
        }



        // package is updating, so just unload the extensions

        private async void Catalog_PackageUpdating(AppExtensionCatalog sender, AppExtensionPackageUpdatingEventArgs args)
        {
            await UnloadExtensions(args.Package);
        }



        // package is removed, so unload all the extensions in the package and remove it

        private async void Catalog_PackageUninstalling(AppExtensionCatalog sender, AppExtensionPackageUninstallingEventArgs args)
        {
            await RemoveExtensions(args.Package);
        }





        // package status has changed, could be invalid, licensing issue, app was on USB and removed, etc
        private async void Catalog_PackageStatusChanged(AppExtensionCatalog sender, AppExtensionPackageStatusChangedEventArgs args)
        {
            // get package status
            if (!(args.Package.Status.VerifyIsOK()))
            {
                // if it's offline unload only
                if (args.Package.Status.PackageOffline)
                    await UnloadExtensions(args.Package);
                // package is being serviced or deployed
                else if (args.Package.Status.Servicing || args.Package.Status.DeploymentInProgress)
                {                    // ignore these package status events                
                }
                // package is tampered or invalid or some other issue
                // glyphing the extensions would be a good user experience
                else
                    await RemoveExtensions(args.Package);

            }
            // if package is now OK, attempt to load the extensions
            else
            {
                // try to load any extensions associated with this package
                await LoadExtensions(args.Package);
            }
        }



        // loads an extension
        private async Task LoadExtension(AppExtension ext)
        {
            // get unique identifier for this extension
            string identifier = ext.AppInfo.AppUserModelId + "!" + ext.Id;

            // load the extension if the package is OK
            if (!ext.Package.Status.VerifyIsOK()
                || ext.Package.SignatureKind != PackageSignatureKind.None)
            {
                // if this package doesn't meet our requirements
                // ignore it and return
                return;
            }

            // if its already existing then this is an update
            var existingExt = this.extensions.Where(e => e.UniqueId == identifier).FirstOrDefault();

            // new extension
            if (existingExt == null)
            {
                // get extension properties
                var properties = await ext.GetExtensionPropertiesAsync() as PropertySet;

                var servicesProperty = properties[SERVICE_KEY] as PropertySet;
                var serviceName = servicesProperty["#text"].ToString();


                // get logo 
                var filestream = await (ext.AppInfo.DisplayInfo.GetLogo(new Windows.Foundation.Size(1, 1))).OpenReadAsync();
                BitmapImage logo = new BitmapImage();
                logo.SetSource(filestream);

                // create new extension
                var nExt = CreateExtensionProvider(ext, serviceName, logo);

                // Add it to extension list
                extensions.Add(nExt);
                nExt.IsEnabled = true;
            }
            // update
            else
            {

                // update the extension
                await existingExt.Update(ext);
            }
        }

        internal abstract TExtensionProvider CreateExtensionProvider(AppExtension ext, string serviceName, BitmapImage logo);

        // loads all extensions associated with a package - used for when package status comes back
        private async Task LoadExtensions(Package package)
        {
            await dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, () =>
            {
                extensions.Where(ext => ext.Extension.Package.Id.FamilyName == package.Id.FamilyName).ToList().ForEach(e => { e.IsEnabled = true; });
            });
        }

        // unloads all extensions associated with a package - used for updating and when package status goes away
        private async Task UnloadExtensions(Package package)
        {
            await dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, () =>
            {
                extensions.Where(ext => ext.Extension.Package.Id.FamilyName == package.Id.FamilyName).ToList().ForEach(e => { e.IsEnabled = false; });
            });
        }

        // removes all extensions associated with a package - used when removing a package or it becomes invalid
        private async Task RemoveExtensions(Package package)
        {
            await dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, () =>
            {
                extensions.Where(ext => ext.Extension.Package.Id.FamilyName == package.Id.FamilyName).ToList().ForEach(e => { e.IsEnabled = false; extensions.Remove(e); });
            });
        }


        public abstract class ExtensionProvider
        {
            public AppExtension Extension { get; private set; }
            public BitmapImage Logo { get; private set; }
            protected string serviceName { get; private set; }

            internal ExtensionProvider(AppExtension ext, string serviceName, BitmapImage logo)
            {
                this.Extension = ext;
                this.serviceName = serviceName;
                this.Logo = logo;
            }


            public string UniqueId => Extension.AppInfo.AppUserModelId + "!" + Extension.Id;

            public bool IsEnabled { get; internal set; }



            internal async Task Update(AppExtension ext)
            {
                // ensure this is the same uid
                string identifier = ext.AppInfo.AppUserModelId + "!" + ext.Id;
                if (identifier != this.UniqueId)
                {
                    return;
                }

                // get extension properties
                var properties = await ext.GetExtensionPropertiesAsync() as PropertySet;

                // get logo 
                var filestream = await (ext.AppInfo.DisplayInfo.GetLogo(new Windows.Foundation.Size(1, 1))).OpenReadAsync();
                BitmapImage logo = new BitmapImage();
                logo.SetSource(filestream);

                // update the extension
                this.Extension = ext;

                this.Logo = logo;

                #region Update Properties
                // update app service information
                serviceName = null;
                if (properties != null)
                {
                    if (properties.ContainsKey("Service"))
                    {
                        PropertySet serviceProperty = properties["Service"] as PropertySet;
                        this.serviceName = serviceProperty["#text"].ToString();
                    }
                }

                if (serviceName == null)
                    throw new Exception();
                #endregion
            }
        }

        private abstract class ExtensionConnection : IDisposable
        {
            private readonly AppServiceConnection connection;
            private bool isDisposed;
            private readonly CancellationToken cancelTokem;
            private readonly Guid id = Guid.NewGuid();

            private ExtensionConnection(AppServiceConnection connection, CancellationToken cancelTokem = default(CancellationToken))
            {
                this.connection = connection;
                connection.ServiceClosed += Connection_ServiceClosed;
                connection.RequestReceived += Connection_RequestReceived;

                this.cancelTokem = cancelTokem;
                cancelTokem.Register(Canceld);
            }

            private async void Canceld()
            {
                var valueSet = new ValueSet();

                valueSet.Add(AbstractExtension<object, object, object>.ID_KEY, id);
                valueSet.Add(AbstractExtension<object, object, object>.CANCEL_KEY, true);

                await connection.SendMessageAsync(valueSet);
            }

            private async void Connection_RequestReceived(AppServiceConnection sender, AppServiceRequestReceivedEventArgs args)
            {
                if (!args.Request.Message.ContainsKey(AbstractExtension<object, object, object>.ID_KEY))
                    return;

                var id = (Guid)args.Request.Message[AbstractExtension<object, object, object>.ID_KEY];
                if (this.id != id)
                    return;
                var valueSet = await RequestRecived(sender, args);
                await args.Request.SendResponseAsync(new ValueSet());
            }

            protected abstract Task<ValueSet> RequestRecived(AppServiceConnection sender, AppServiceRequestReceivedEventArgs args);

            private void Connection_ServiceClosed(AppServiceConnection sender, AppServiceClosedEventArgs args)
            {
                Dispose();
            }

            public void Dispose()
            {
                if (isDisposed)
                    return;
                connection.Dispose();
                isDisposed = true;
            }

        }
    }
}
