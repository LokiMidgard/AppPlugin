[![GitHub license](https://img.shields.io/github/license/LokiMidgard/AppExtensionService.svg?style=flat-squar)](https://tldrlegal.com/license/mit-license#summary)
[![Build status](https://ci.appveyor.com/api/projects/status/7yumsvqwno7l65gc?svg=true)](https://ci.appveyor.com/project/LokiMidgard/appextensionservice)


# <img src="https://raw.githubusercontent.com/LokiMidgard/AppExtensionService/master/Assets/Logo.png" width="60px" height="60px" /> AppPlugin
This project combines UWP AppServices with AppExtensions and DataContracts in order to realize a more Code orientated way to write pluginss.

## Overview

In order to provide Plugins, this library uses AppServices and appExtensions provided by UWP. A Plugin definition describs a funcion that takes one argument and returns
a value. Normaly this is defined in an Library that can be consumed by other developers to implement those Plugins. They can just implement the abstract class that is
the Plugin definition and provide minimal information in there AppManifest. The CorssApp communication will be handeld by this library.

In addition to a Plugin that takes an argument and returns a value, there is an alternative. This will take two arguments. The seccond argument is used to set some
Settings in the Plugin. The Plugin has an additinal Property which returns the possible Values that can be set. What kind of Values and how those can be used needs to
be defined in the Plugin definition. (see below)


## Usage

### Plugin definition

To define a Plugin definition, reference the AppExtensionService library and extend AbstractPlugin.
The following sample class defines a Plugin definition that transforms strings:

``` c#

    public abstract class StringManipulationPlugin : AbstractPlugin<string,string,double>
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
    var list = StringManipulationPlugin.List("MyPlugins.StringPlugins");
    await list.Init();
    var anPlugin = list.Plugins.FirstOrDefault();
    var result = await anPlugin.Execute("Test String");

    return b.ToString();

```




### Implement a Plugin

 
In order to implement a Plugin definition the class defined earlyer needs to be extended and the abstract Method ```Execute``` needs to be implemented.
For this sample a Reverse Plugin is shown:

**Important**: A plugin must be implemented in an _Windows Runtime Component_ Project.

```c#
    internal class ReversePluginIntern : StringManipulationPlugin
    {
        protected override async Task<string> Execute(string input, IProgress<double> progress, CancellationToken cancelToken)
        {

            var b = new StringBuilder();
            for (int i = input.Length - 1; i >= 0; i--)
            {
                b.Append(input[i]);
                progress.Report(b.Length / (double)input.Length);
                if (cancelToken.IsCancellationRequested)
                    break;
                await Task.Delay(500);
            }

            return b.ToString();
        }
    }
```

Consider that a Windows Runtime Component has special requirements. So the implementation must not be public.

To access the plugin a simple wrapper needs to be created:

```c#
    public sealed class ReveresPlugin : Windows.ApplicationModel.Background.IBackgroundTask
    {
        private IBackgroundTask internalTask = new ReversePluginIntern();
        public void Run(IBackgroundTaskInstance taskInstance)
         => internalTask.Run(taskInstance);
    }

```
Remember that this class may not extend any other class and must be sealed. The internal implementation already implements the interface ```IBackgroundTask``` explicitly
so its ```Run```-Methode can just be called.

### Deploy a Plugin

To Deploy a Plugin just add following code to your AppManifest:

```xml
        <uap:Extension Category="windows.appService" EntryPoint="ReversePlugin.ReveresPlugin">
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

- ```ReversePlugin.ReveresPlugin``` is the EntryPoint of the Plugin, the full quallified class name. (```ReversePlugin``` is the namespace).
- ```MyService``` It must be identical in the AppService
   tag and Service tag. And also unique if you implement more Plugins.
- ```MyPlugins.StringPlugins``` is the Plugin name that was used at the beginning.

## Options

In order to Support Options for your Plugins the Plugin definition must define what Type of Options can be set. The intended useage of Options is to
define a Type for the options that has a list of Option values also defined by the Plugin definition. E.g a Plugin definition may define that every Plugin
can have multiple int values as settings and must provide for every value a name, description, min and max value. 

```c#
    public class Options
    {
        public List<IntOption> Settings { get; set; }
    }

    public class IntOption
    {
        public int Min { get; set; }
        public int Max { get; set; }
        public int Value { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
    }
```

A Plugin can now define what configuration it needs and the client can show the User for every setting an apropriate UI. Following extends the sample to let the user
decide how long the Delay is.

```c#
    public abstract class StringManipulationPlugin : AbstractPlugin<string, string, Options, double>
    {


    }

    internal class ReversePluginIntern : StringManipulationPlugin
    {
        protected override async Task<string> Execute(string input, Options options, IProgress<double> progress, CancellationToken cancelToken)
        {

            var b = new StringBuilder();
            for (int i = input.Length - 1; i >= 0; i--)
            {
                b.Append(input[i]);
                if (cancelToken.IsCancellationRequested)
                    break;
                progress.Report(b.Length / (double)input.Length);
                await Task.Delay(options.Settings[0].Value);
            }
            return b.ToString();
        }

        protected override Task<Options> GetDefaultOptions()
        {
            return Task.FromResult(new Options()
            {
                Settings =
                {
                    new IntOption()
                    {
                        Min=0,
                        Max=1000,
                        Name= "Delay",
                        Description="Defines how long it takes to write one letter."
                    }
                }
            });
        }
    }
```
