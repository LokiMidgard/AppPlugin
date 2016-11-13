[![GitHub license](https://img.shields.io/github/license/LokiMidgard/AppExtensionService.svg?style=flat-squar)](https://tldrlegal.com/license/mit-license#summary)



# AppExtensionService
This project combines UWP AppServices with AppExtensions and DataContracts in order to realise a more Code orientated way to write extensions.

## Usage

### Plugin definition

To define a Plugin definition, reference the AppExtensionService library and extend AbstractExtension.
The following sample class defines a Plugin definition that transforms strings:

``` c#

    public abstract class StringManipulationExtension : AbstractExtension<string,string,double>
    {

    }

```

The Plugin will take one string and returns a string. This is Specified by the first two generic type arguments. The last generic argument defines the type that is useed
to report the progress. In this case a double is used and interpret as percentage. (from 0.0 to 1.0)

Every plugin implementation that will be consumed can extend this abstract class.

### Consume a Plugin

In order to consume a Plugin the App must define that it wants to use an extensin:

``` xml

<?xml version="1.0" encoding="utf-8"?>

<Package
  xmlns="http://schemas.microsoft.com/appx/manifest/foundation/windows10"
  xmlns:mp="http://schemas.microsoft.com/appx/2014/phone/manifest"
  xmlns:uap="http://schemas.microsoft.com/appx/manifest/uap/windows10"
  xmlns:uap3="http://schemas.microsoft.com/appx/manifest/uap/windows10/3" 
  IgnorableNamespaces="uap uap3 mp">

  <!--... -->
  <Applications>
    <Application Id="App"
      Executable="$targetnametoken$.exe"
      EntryPoint="SampleApp.App">
      
      <!--... -->
      
      <Extensions>
        <!--... -->
        <uap3:Extension Category="windows.appExtensionHost">
          <uap3:AppExtensionHost>
            <uap3:Name>MyPlugins.StringPlugins</uap3:Name>
          </uap3:AppExtensionHost>
        </uap3:Extension>
      </Extensions>
    </Application>
  </Applications>

  <!--... -->
</Package>
```

In this case the Plugin name is ```MyPlugins.StringPlugins```. The name must be <= 39.

To get the Plugins following code can be used:

```c#
    var list = StringManipulationExtension.List("MyPlugins.StringPlugins");
    await list.Init();
    var anExtension = list.Extensions.FirstOrDefault();
    var result = await anExtension.Execute("Test String");

    return b.ToString();

```




### Implement a Plugin

 
In order to implement a Plugin definition the class defined earlyer needs to be extended and the abstract Method ```Execute``` needs to be implemented.
For this sample a Reverse Plugin is shown:

**Important**: A plugin must be implemented in an _Windows Runtime Component_ Project.

```c#
    internal class ReverseExtensionIntern : StringManipulationExtension
    {
        protected override async Task<string> Execute(string input, IProgress<double> progress, CancellationToken cancelToken)
        {

            var b = new StringBuilder();
            for (int i = input.Length - 1; i >= 0; i--)
            {
                b.Append(input[i]);
                progress.Report(b.Length / (double)input.Length);
                await Task.Delay(500);
            }

            return b.ToString();
        }
    }
```

Consider that a Windows Runtime Component has special requirements. So the implementation must not be public.

To access the plugin a simple wrapper needs to be created:

```c#
    public sealed class ReveresExtension : Windows.ApplicationModel.Background.IBackgroundTask
    {
        private IBackgroundTask internalTask = new ReverseExtensionIntern();
        public void Run(IBackgroundTaskInstance taskInstance)
         => internalTask.Run(taskInstance);
    }

```
Remember that this class may not extend any other class and must be sealed. The internal implementation already implements the interface ```IBackgroundTask``` explicitly
so its ```Run```-Methode can just be called.

### Deploy a Plugin

To Deploy a Plugin just add following code to your AppManifest:

```xml
        <uap:Extension Category="windows.appService" EntryPoint="ReversePlugin.ReveresExtension">
          <uap3:AppService Name="MyService" />
        </uap:Extension>
        <uap3:Extension Category="windows.appExtension">
          <uap3:AppExtension Name="MyPlugins.StringPlugins" 
                Id="Reverse" 
                DisplayName="String Reverser" 
                Description="This Plugin revereses the string." 
                PublicFolder="Assets">
            <uap3:Properties>
              <Service>MyService</Service>
            </uap3:Properties>
          </uap3:AppExtension>
        </uap3:Extension>
```

- ```ReversePlugin.ReveresExtension``` is the EntryPoint of the Plugin, the full quallified class name. (```ReversePlugin``` is the namespace).
- ```MyService``` It must be identical in the AppService
   tag and Service tag. And also unique if you implement more Plugins.
- ```MyPlugins.StringPlugins``` is the Extension name that was used at the beginning.

