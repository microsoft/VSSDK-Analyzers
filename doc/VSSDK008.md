# VSSDK008 Avoid UI thread in MEF Part construction

Historically, the Visual Studio Editor initialized its components on the UI thread. However, to improve startup performance, Visual Studio is evolving to load Editor components on background threads.

The Visual Studio Editor is composed using MEF (Managed Extensibility Framework), and Editor components are referred to as **MEF parts**.

**MEF activation code paths** (importing constructors, `OnImportsSatisfied` callbacks, and any code called from them) should be free-threaded to ensure proper Visual Studio performance.

**Free-threaded code** is code that (and all code it invokes transitively) can complete on the caller's thread, no matter which thread that is.

**Thread-affinitized code** requires execution on a particular thread, typically the UI thread. Such code may directly or indirectly call methods or access objects that must run on the UI thread.

## Analyzer
This analyzer identifies classes or member decorated with `[Export]`, `[InheritedExport]` or their derivatives. It then checks if initialization of these members has UI thread affinity.
When Visual Studio makes attempt to load a UI thread-affinitized part, its initialization will either throw an exception, rendering the MEF unusable, or lead to a deadlock, freezing Visual Studio.

The analyzer examines:
- Default constructors
- Constructors decorated with `[ImportingConstructor]`
- Field or property initializers
- Implementations of `IPartImportsSatisfiedNotification.OnImportsSatisfied`

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

### Defer UI thread work

Instead of accessing UI thread-affined members during MEF part construction, defer that work until after construction by using techniques like lazy initialization:
```
using System.ComponentModel.Composition;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Threading;

[Export]
class MyMEFComponent
{
    // Option 1: Use a property
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

    // Option 2: use AsyncLazy to defer UI thread work
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

### Use thread-safe initialization

Initialize UI thread-affinitized dependencies on demand, after the MEF part has been constructed.
```
using System.ComponentModel.Composition;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Threading;

[Export]
class MyMEFComponent
{
    private readonly JoinableTaskContext _joinableTaskContext;
    
    [ImportingConstructor]
    public MyMEFComponent(JoinableTaskContext joinableTaskContext)
    {
        _joinableTaskContext = joinableTaskContext;
    }
    
    public async Task InitializeAsync()
    {
        // Switch to UI thread only when actually needed
        await _joinableTaskContext.Factory.SwitchToMainThreadAsync();
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
For example, the allows defining `AsyncLazy` in the constructor, which would be evaluated on demand after initialization.

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

## Benefits

Visual Studio 17.14 can preload MEF parts on background threads when the following Preview Features are enabled:
- "Initialize editor parts asynchronously during solution load"
- "Asynchronously Load Documents During Solution Load"

In the future releases, these Preview Features will be enabled. 
By following this guidance, your extension will continue to work as Visual Studio begins to enforce the threading rule 
requiring MEF parts contruction to be free-threaded.

[VisualStudioThreadingCookbook]: https://microsoft.github.io/vs-threading/docs/cookbook_vs.html#how-do-i-effectively-verify-that-my-code-is-fully-free-threaded
[FireAndForget]: https://aka.ms/vsthreadingcookbook#void-returning-fire-and-forget-methods
