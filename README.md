# WorldXaml

Portable XAML runtime based on XamlX and Avalonia. Provides a way to load XAML files at runtime and create UI elements
from them. For your game, or whatever.

## Features

- AOT-compatible, compiles all your XAML code to C# at compile time
- Hot reload support for XAML files, so you can edit your UI without restarting your game
- Doesn't require Avalonia or any render layer, bring your own UI framework
- Runs on anything
- Pretends to be Avalonia, so is compatible with all tooling that works with Avalonia
- Zero reflection by default (hot reloading requires reflection)
- Bring your own layout or use our Yoga (flexbox) based implementation

## Usage

- First declare all your Avalonia namespaces in your assembly. We have an example of this in WorldXaml.UI.Base.
- Implement NameScope, for instance crawling through all your controls and fetching the Name property
- Implement all your component types, or use `WorldXaml.UI.Yoga`
- Write your XAML, for instance using Yoga:

```xml
<?xml version="1.0" encoding="utf-8"?>

<yoga:View
    xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
    xmlns:yoga="clr-namespace:NFMWorld.UI.Yoga"
    x:Class="NFMWorld.UI.Hud.CentralTextView"
    Name="CentralText"
    AlignItems="Center"
    FlexDirection="Column">

    <yoga:Box AlignItems="Center" Flex="1">
        <yoga:TextRun Name="CenterText"
                      Color="black"
                      Font="bold 1px Adventure"
                      Display="None" />
    </yoga:Box>

    <yoga:Node Flex="1" />
</yoga:View>
```

And the code-behind:

```csharp
using NFMWorld.UI.Yoga;

namespace NFMWorld.UI.Hud;

public partial class CentralTextView : View
{
    public CentralTextView()
    {
        InitializeComponent();
    }
}
```

And update your csproj:

```xml
  <!-- I like doing it like this to debug generation bugs -->
  <PropertyGroup>
    <EmitCompilerGeneratedFiles>true</EmitCompilerGeneratedFiles>
    <CompilerGeneratedFilesOutputPath>Generated</CompilerGeneratedFilesOutputPath>
  </PropertyGroup>
  <ItemGroup>
    <!-- Exclude emitted source-generator files from implicit *.cs glob to prevent double compilation -->
    <Compile Remove="Generated/**" />
  </ItemGroup>

  <!-- This is the part you actually need -->
  <PropertyGroup>
    <WorldXamlGeneratorBehavior>WithXamlXCompilation</WorldXamlGeneratorBehavior>
    <WorldXamlGeneratorIsHotReloadingEnabled>true</WorldXamlGeneratorIsHotReloadingEnabled>

    <!-- If you are not using WorldXaml.UI.Base, set this to your hot reload implementation -->
    <WorldXamlGeneratorHotReloadTypeName>WorldXaml.UI.Base.Xaml.XamlHotReload</WorldXamlGeneratorHotReloadTypeName>

    <!-- If you are not using WorldXaml.UI.Yoga, set these to your own types -->
    <WorldXamlGeneratorStyledElementTypeName>WorldXaml.UI.Yoga.Node</WorldXamlGeneratorStyledElementTypeName>
    <WorldXamlGeneratorWindowTypeName>WorldXaml.UI.Yoga.View</WorldXamlGeneratorWindowTypeName>
  </PropertyGroup>
  <ItemGroup>
    <ProjectReference Include="..\WorldXaml\WorldXaml.Generator\WorldXaml.Generator.csproj"
                      PrivateAssets="all"
                      ReferenceOutputAssembly="false"
                      OutputItemType="Analyzer"
    />
    <!-- If you're using our Avalonia types -->
    <ProjectReference Include="..\WorldXaml\WorldXaml.UI.Base\WorldXaml.UI.Base.csproj" />
    <!-- If you're using our Yoga layout engine -->
    <ProjectReference Include="..\WorldXaml\WorldXaml.UI.Yoga\WorldXaml.UI.Yoga.csproj" />
  </ItemGroup>
  <Import Project="../WorldXaml.Generator/WorldXaml.Generator.props" />
  
  <!-- XAML files to compile -->
  <ItemGroup>
    <AvaloniaXaml Include="Mad\UI\Hud\CentralTextView.xaml" />
    <AvaloniaXaml Include="Mad\UI\Hud\LapTimerSplitsView.xaml" />
    <AvaloniaXaml Include="Mad\UI\Hud\PowerDamageBars.xaml" />
    <AvaloniaXaml Include="Mad\UI\Hud\TTLapTimerSplitsView.xaml" />
    <AvaloniaXaml Include="Mad\UI\Menu\GarageUiView.xaml" />
  </ItemGroup>

  <!-- https://platform.uno/blog/using-msbuild-items-and-properties-in-c-9-source-generators/ -->
  <Target Name="_InjectAdditionalFiles" BeforeTargets="GenerateMSBuildEditorConfigFileShouldRun">
    <ItemGroup>
      <AdditionalFiles Include="@(AvaloniaXaml)" SourceItemGroup="AvaloniaXaml"/>
    </ItemGroup>
  </Target>
```

If you're using WorldXaml.UI.Base instead of bringing your own Avalonia types, you should provide a logger
implementation:

```csharp
Logging.LogMessage = (level, message) =>
{
    if (level == LogLevel.Info)
        logger.LogInformation(message);
    else if (level == LogLevel.Warning)
        logger.LogWarning(message);
    else if (level == LogLevel.Error)
        logger.LogError(message);
    else if (level == LogLevel.Debug)
        logger.LogDebug(message);
    else
        throw new ArgumentOutOfRangeException(nameof(level), level, null);
};
```

By default it just logs to the console.

If you're using our Yoga layout engine, we expect you to assign `IXamlGraphicsBackend.Backend` to an implementation of
our graphics backend interface. It does the bare minimum so it's really simple.

```csharp
public class MyGraphicsBackend : IXamlGraphicsBackend
{
    public class MyGraphics : IXamlGraphics
    {
        // We'll set this property based on the `Opacity` property of a given element, right before rendering it.
        public float Alpha { get; set; }
    }

    // Set this to the global scale to apply to all elements. This is useful for things like DPI scaling or in-game UI
    // scaling.
    public float Scale { get; set; }

    // Set this to the size of your game's viewport in pixels. This is used for things like percentage-based sizes and
    // for clipping.
    public Vector2 Viewport { get; }

    // Set this to an implementation of IXamlGraphics.
    public IXamlGraphics Graphics { get; } = new MyGraphics();
}

IXamlGraphicsBackend.Backend = new MyGraphicsBackend();
```

That's basically all you need. Everything else works the same as Avalonia. Now you can do `new CentralTextView()`.

You can set or remove AVA_DEBUG in the generator csproj to enable or disable debug logging, which is useful when the
generator isn't doing what it's supposed to.

## Debugging

If you compile WorldXaml.UI.Yoga in debug mode you get access to a NodeDebugger which lets you pull out information
you can use to build an inspector for your nodes. To use this you must make sure to call NodeDebugger.NewFrame on every
new frame.

## Hot Reloading

This is not bundled into the library because it requires compiling the XAML with XamlX yourself. But the source
generator will put the code to initialize the hot reloader in the right place for you if you want. Copy the code from:

https://github.com/needforrewrite/NFM-World/blob/master/nfm-world/mad/ui/yoga/xaml/debug/XamlHotReload.cs

## Missing stuff

Bindings are possible but I haven't tried implementing them yet.

## Examples

We use this a ton in [NFM World](https://github.com/needforrewrite/NFM-World) for most of the non-debug UI. We use
NanoVG for rendering and Yoga (included here) for layouting.