# VSSDK008 Avoid UI thread affinity in MEF Part construction

Historically, the Visual Studio initialized its components on the UI thread. However, to improve startup performance, Visual Studio is evolving to load components on background threads.

Attempts to load extensions on background thread may lead to the extension crashing, if it uses UI-thread affinitized code. The purpose of this analyzer is to identify potential issues and ensure that VS extension can be loaded on the background thread.

### Definitions

Visual Studio is composed using [Managed Extensibility Framework (MEF)][ManagedExtensibilityFramework], and components are referred to as **MEF parts**.

**MEF activation code paths** (importing constructors, `OnImportsSatisfied` callbacks, and any code called from them) should be free-threaded to ensure proper Visual Studio performance.

**Free-threaded code** is code that (and all code it invokes transitively) can complete on the caller's thread, no matter which thread that is.

**Thread-affinitized code** requires execution on a particular thread, typically the UI thread. Such code may directly or indirectly call methods or access objects that must run on the UI thread.

For more information about verifying that your code is fully free-threaded, see the [Visual Studio Threading Cookbook][VisualStudioThreadingCookbook]

## Analyzer
This analyzer identifies classes or member decorated with `[Export]`, `[InheritedExport]` or their derivatives, from either `System.ComponentModel.Composition` or `System.Composition` namespace.
It then checks if initialization of these members has UI thread affinity.
When Visual Studio makes attempt to load a UI thread-affinitized part, its initialization will either throw an exception, rendering the MEF unusable, or lead to a deadlock, freezing Visual Studio.

The analyzer examines:
- Default constructors
- Constructors decorated with `[ImportingConstructor]`
- Field or property initializers
- Methods that implement `IPartImportsSatisfiedNotification.OnImportsSatisfied` or are decorated with `[OnImportsSatisfied]`

In addition to supporting classes decorated with `[Export]` attribute, this analyzer supports:
- Base class decorated with `[InheritedExport]`.
- Attributes derived from `ExportAttribute`.

## Examples of patterns that are flagged by this analyzer
```
using System.ComponentModel.Composition;
using Microsoft.VisualStudio.Shell;

[Export]
class MyMEFComponent
{
    public MyMEFComponent()
    {
        ThreadHelper.ThrowIfNotOnUIThread();
    }
}

[Export]
class MyMEFComponentWithImportingConstructor
{
    [ImportingConstructor]
    public MyMEFComponentWithImportingConstructor(IMyInterface dependency)
    {
        ThreadHelper.ThrowIfNotOnUIThread();
    }
}

[Export]
class MyMEFComponentWithOnImportsSatisfied : IPartImportsSatisfiedNotification
{
    public void OnImportsSatisfied()
    {
        ThreadHelper.ThrowIfNotOnUIThread();
    }
}

[Export]
class MyMEFComponentWithUIThreadField
{
    // UI thread-affined member in field initializer
    object context = UIContext.FromUIContextGuid(Guid.Empty);
}
```
## Solution

Instead of accessing UI thread-affined members during MEF part construction, defer that work until after construction by using techniques like lazy initialization.

Flagged code with UI thread affinity:
```
using System.ComponentModel.Composition;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Threading;

[Export]
class MyMEFComponent
{
    private IVsTextManager4 _textManager;

    [ImportingConstructor]
    public MyMEFComponent(
        [Import(typeof(SVsServiceProvider))] IServiceProvider serviceProvider)
    {
        _textManager = (IVsTextManager4)serviceProvider.GetService(typeof(SVsTextManager));
    }
}
```

Get service using a property:
```
using System.ComponentModel.Composition;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Threading;

[Export]
class MyMEFComponent
{
    private readonly IServiceProvider _serviceProvider;
    private readonly JoinableTaskContext _joinableTaskContext;
    private IVsTextManager4? _textManager;
    private IVsTextManager TextManager
    {
        get
        {
            _joinableTaskContext.AssertUIThread();
            return _textManager ??= (IVsTextManager)_serviceProvider.GetService(typeof(SVsTextManager));
        }
    }

    [ImportingConstructor]
    public MyMEFComponent(
        [Import(typeof(SVsServiceProvider))] IServiceProvider serviceProvider,
        JoinableTaskContext joinableTaskContext)
    {
        _joinableTaskContext = joinableTaskContext;
        _serviceProvider = serviceProvider;
    }
}
```

Get service using `AsyncLazy`:
```
using System.ComponentModel.Composition;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Threading;

[Export]
class MyMEFComponent
{
    private readonly AsyncLazy<IVsTextManager> _lazyTextManager;

    [ImportingConstructor]
    public MyMEFComponent(
        [Import(typeof(SVsServiceProvider))] IServiceProvider serviceProvider,
        JoinableTaskContext joinableTaskContext)
    {
        _lazyTextManager = new AsyncLazy<IVsTextManager>(async () =>
        {
            await joinableTaskContext.Factory.SwitchToMainThreadAsync();
            return (IVsTextManager)_serviceProvider.GetService(typeof(SVsTextManager));
        }, joinableTaskContext.Factory);
    }
}
```

### Limitations

The analyzer has some limitations and may report false positives or underreport issues in the following scenarios:

#### False positive: Fire-and-forget async methods

The analyzer may flag UI thread access inside asynchronous methods that are started but not awaited during construction (["fire-and-forget"][FireAndForget]).
Since these operations execute asynchronously and don't block MEF part construction, they are generally acceptable.

If you encounter false positives, you may suppress the warning using `#pragma warning disable VSSDK008`
with a justification comment explaining why the code is actually safe.

```
using Microsoft.VisualStudio.Threading;
using System.ComponentModel.Composition;
using System.Threading.Tasks;

[Export]
class MyMEFComponent
{
    [ImportingConstructor]
    public MyMEFComponent(JoinableTaskContext joinableTaskContext)
    {
        // This will be flagged as a false positive
        LocalFunctionAsync().Forget();

        async Task LocalFunctionAsync()
        {
            // Since this runs asynchronously after construction,
            // it's acceptable to switch to the UI thread here
#pragma warning disable VSSDK008
            await joinableTaskContext.Factory.SwitchToMainThreadAsync();
#pragma warning restore VSSDK008
        }
    }
}
```

#### False negative: No flow analysis

The analyzer does not perform flow analysis to inspect methods called by the MEF initialization methods.
```
using System.ComponentModel.Composition;

[Export]
class MyMEFComponent
{
    public MyMEFComponent()
    {
        // The analyzer doesn't flag this, despite UI thread affinity
        AnotherMethod();
    }

    public void AnotherMethod()
    {
        Microsoft.VisualStudio.Shell.ThreadHelper.ThrowIfNotOnUIThread();
    }
}";
```

#### False negative: lambdas

To reduce amount of false positives, the analyzer ignores code within lambdas, assumeing that lambdas would be invoked after initialization.
For example, see the example above where `AsyncLazy` is defined in the constructor. The lambda would be evaluated on demand after initialization.

Therefore, this analyzer will miss an edge case of synchronously executing a lambda during construction:
```
using System.ComponentModel.Composition;
using Microsoft.VisualStudio.Threading;

[Export]
class MyMEFComponent
{
    [ImportingConstructor]
    public MyMEFComponent(JoinableTaskContext joinableTaskContext)
    {
        // The analyzer doesn't flag this, despite UI thread affinity
        var result = joinableTaskContext.Factory.Run(async () =>
        {
            await joinableTaskContext.Factory.SwitchToMainThreadAsync();
            return "result";
        });
    }
}
```

## Outcome

Visual Studio 17.14 can preload MEF parts on background threads when the following Preview Features are enabled:
- "Initialize editor parts asynchronously during solution load"
- "Asynchronously Load Documents During Solution Load"

In the future releases, these Preview Features will be enabled. 
By following this guidance, your extension will continue to work as Visual Studio begins to enforce the threading rule 
requiring MEF parts contruction to be free-threaded.

[ManagedExtensibilityFramework]: https://learn.microsoft.com/en-us/dotnet/framework/mef/
[VisualStudioThreadingCookbook]: https://microsoft.github.io/vs-threading/docs/cookbook_vs.html#how-do-i-effectively-verify-that-my-code-is-fully-free-threaded
[FireAndForget]: https://aka.ms/vsthreadingcookbook#void-returning-fire-and-forget-methods
