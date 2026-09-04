# VSSDK009 Use approved `VsTaskRunContext` values with `VsTaskLibraryHelper`

`Microsoft.VisualStudio.Shell.VsTaskLibraryHelper` helpers such as `StartOnIdle`, `RunAsync`, and `WithPriority` schedule work on the UI thread or influence the priority used when switching to it.
Only these `VsTaskRunContext` values are allowed:

- `VsTaskRunContext.UIThreadBackgroundPriority`
- `VsTaskRunContext.UIThreadIdlePriority`
- `VsTaskRunContext.UIThreadNormalPriority`

## Examples of patterns that are flagged by this analyzer

```csharp
using System.Threading.Tasks;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Threading;

class MyPackage
{
    void M(JoinableTaskFactory joinableTaskFactory)
    {
        joinableTaskFactory.StartOnIdle(() => { }, VsTaskRunContext.BackgroundThread);
        joinableTaskFactory.RunAsync(VsTaskRunContext.CurrentContext, async () => await Task.Yield());
        joinableTaskFactory.WithPriority(VsTaskRunContext.UIThreadSend);
    }
}
```

## Solution

Use one of the approved values instead.

```csharp
using System.Threading.Tasks;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Threading;

class MyPackage
{
    void M(JoinableTaskFactory joinableTaskFactory)
    {
        joinableTaskFactory.StartOnIdle(() => { }, VsTaskRunContext.UIThreadBackgroundPriority);
        joinableTaskFactory.RunAsync(VsTaskRunContext.UIThreadNormalPriority, async () => await Task.Yield());
        joinableTaskFactory.WithPriority(VsTaskRunContext.UIThreadIdlePriority);
    }
}
```
