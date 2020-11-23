# IntelliSenseExtender

[![Build status](https://ci.appveyor.com/api/projects/status/q0abdwegw5vh3pl9?svg=true)](https://ci.appveyor.com/project/Dreamescaper/intellisenseextender)

[VS Marketplace - IntelliSense Extender 2017](https://marketplace.visualstudio.com/items?itemName=Dreamescaper.IntelliSenseExtender)

[VS Marketplace - IntelliSense Extender 2019](https://marketplace.visualstudio.com/items?itemName=Dreamescaper.IntellisenseExtender2019)

# Features:
- Suggest interfaces implementations or derived types during assignments
- Suggest static factory methods if they are present for type during assignments 
- Suggest suitable locals and members first

# :warning: Deprecation Warning:
- Microsoft has added the ability to show items from unimported namespaces when autocompleting with Intellisense. As of Visual Studio 16.8, this plugin conflicts with that feature and causes no unimported types to be shown. If using Visual Studio >16.8, disable this plugin and enable "Show items from unimported namespaces" in the Visual Studio options window.

![example](https://i.imgur.com/hTQh0E1.png)
