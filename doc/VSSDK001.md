# VSSDK001 Derive from AsyncPackage

Your VS package should be an async package to help ensure Visual Studio is fast and responsive
for your customers.

## Examples of patterns that are flagged by this analyzer

```csharp
[PackageRegistration(UseManagedResourcesOnly = true)]
class MyCoolPackage : Package {
    protected override void Initialize()
    {
        base.Initialize();
    }
}
```

## Solution

Derive from AsyncPackage instead:

```csharp
[PackageRegistration(UseManagedResourcesOnly = true, AllowsBackgroundLoading = true)]
class MyCoolPackage : AsyncPackage {
    protected override async Task InitializeAsync(CancellationToken cancellationToken, IProgress<ServiceProgressData> progress)
    {
        await base.InitializeAsync(cancellationToken, progress);

        // When initialized asynchronously, we *may* be on a background thread at this point.
        // Do any initialization that requires the UI thread after switching to the UI thread.
        // Otherwise, remove the switch to the UI thread if you don't need it.
        await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync(cancellationToken);
    }
}
```

A code fix is offered for this diagnostic to automatically apply this change.
