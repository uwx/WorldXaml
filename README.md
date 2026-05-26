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
- Bindings/MVVM support
- Animations with support for all easing functions and keyframes
  - Transitions similar to Avalonia's control transitions

## Usage

- First declare all your Avalonia namespaces in your assembly. We have an example of this in WorldXaml.UI.Base.
- Implement NameScope, for instance crawling through all your controls and fetching the Name property
- Implement all your component types, or use `WorldXaml.UI.Yoga`
- Write your XAML, for instance using Yoga:

```xml
<View
    xmlns="https://github.com/uwx/worldxaml"
    xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
    xmlns:mc="http://schemas.openxmlformats.org/markup-compatibility/2006"
    xmlns:w="https://github.com/needforrewrite/nfm-world"
    xmlns:d="http://schemas.microsoft.com/expression/blend/2008"
    mc:Ignorable="d" d:DataContext="{d:DesignInstance w:CentralTextViewModel}"
    x:Class="NFMWorld.UI.Hud.CentralTextView"
    DataType="w:CentralTextViewModel"
    AlignItems="Center"
    FlexDirection="Column">

    <Box AlignItems="Center" Flex="1">
        <!--
            alternatively: Opacity="{Animation Shown, Easing=Linear, KeyFrameFrom=0, KeyFrameTo=1, KeyFrameDuration=250, KeyFrameOffset=0}"
            but there's no way to combine animations yet (e.g shown + hidden) so we use transitions instead
        -->
        <w:TextRun
            Opacity="{Binding CenterTextOpacity, Easing=Linear, TransitionDuration=250}"
            Color="{Binding CenterTextColor}"
            Font="{Binding CenterTextFont}"
            Text="{Binding CenterText}"
            StrokeColor="{Binding CenterTextStrokeColor}"
            Display="Flex" />
    </Box>

    <Node Flex="1" />
</View>
```
<small>NB: Both DataType and x:DataType are supported for setting the type of a view's data context, but x: will show red squiggles in Rider</small>

And the code-behind:

```csharp
using NFMWorld.UI.Yoga;

namespace NFMWorld.UI.Hud;

public partial class CentralTextView : View
{
    public CentralTextView()
    {
        InitializeComponent();
        DataContext = new CentralTextViewModel();
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

    <!-- If you are not using WorldXaml.UI.Base, override the appropriate properties from WorldXaml.Generator.props -->
    <WorldXamlGeneratorHotReloadTypeName>WorldXaml.UI.Base.Xaml.XamlHotReload</WorldXamlGeneratorHotReloadTypeName>
    <!-- ...etc -->

    <!-- If you are not using WorldXaml.UI.Yoga, override the appropriate properties from WorldXaml.Generator.props -->
    <WorldXamlGeneratorStyledElementTypeName>WorldXaml.UI.Yoga.Node</WorldXamlGeneratorStyledElementTypeName>
    <WorldXamlGeneratorWindowTypeName>WorldXaml.UI.Yoga.View</WorldXamlGeneratorWindowTypeName>
    <!-- ...etc -->
  </PropertyGroup>
  <ItemGroup>
    <ProjectReference Include="..\WorldXaml\WorldXaml.Generator\WorldXaml.Generator.csproj"
                      PrivateAssets="all"
                      ReferenceOutputAssembly="false"
                      OutputItemType="Analyzer"
    />
    <!-- If you're using our Avalonia-inspired types -->
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
XamlConfig.LogMessage = (level, message) =>
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

And an interpolator implementation if you want to use animations or transitions:

```csharp
XamlConfig.InterpolatorProvider = new MyInterpolatorProvider();
```

By default it just logs to the console and only primitive number/vector/Yoga property transitions are supported.

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

### Bindings

You can use `{Binding …}` markup extensions in your XAML to create bindings. For instance:

```xml
<View DataType="w:CentralTextViewModel" xmlns:w="clr-namespace:YourNamespace">
    <Box>
        <!-- TextRun is a custom control (example, bring your own) -->
        <w:TextRun Text="{Binding HelloWorld}" />
    </Box>
</View>
```

You should set the `DataType` or `x:DataType` of your view to the type of your view model to get AOT compiled bindings,
otherwise it will use reflection which is not AOT-safe.

### Animations

We do it differently from Avalonia because I thought it would be neater this way.

Create an AnimationTrigger property on your view codebehind, and then you can use {Animation …} markup extensions in your XAML to create animations that trigger when the property is set to a certain value. For instance:

```xml
<View xmlns:w="clr-namespace:YourNamespace">
    <Box>
        <!-- Shown is a default animation triggered when Visibility is set to Visible -->
        <!-- TextRun is a custom control (example, bring your own) -->
        <w:TextRun
            Opacity="{Animation Shown, Easing=Linear, KeyFrameFrom=0, KeyFrameTo=1, KeyFrameDuration=250, KeyFrameOffset=0}"
            Text="Hello world!" />
    </Box>
</View>
```

If you're not using Yoga you need to implement `IAnimationCallback` and call `AnimationFrameBegan` periodically (e.g.
every frame, before rendering) to update the animations.

#### Transitions

This is basically the same system as Avalonia's control transitions except we use properties of the Binding.

```xml
<View xmlns:w="clr-namespace:YourNamespace">
    <Box>
        <!-- Linearly interpolates the value when MyOpacity changes -->
        <!-- TextRun is a custom control (example, bring your own) -->
        <w:TextRun
            Opacity="{Binding MyOpacity, Easing=Linear, TransitionDuration=250, TransitionOffset=0}"
            Text="Hello world!" />
    </Box>
</View>
```

### Debugging

If you compile WorldXaml.UI.Yoga in debug mode you get access to a NodeDebugger which lets you pull out information
you can use to build an inspector for your nodes. To use this you must make sure to call NodeDebugger.NewFrame on every
new frame.

### Hot Reloading

Either use WorldXaml.UI.Base (includes hot reloading by default) or implement your own XamlHotReload.Register method.

## Examples

We use this a ton in [NFM World](https://github.com/needforrewrite/NFM-World) for most of the non-debug UI. We use
NanoVG for rendering and Yoga (included here) for layouting.