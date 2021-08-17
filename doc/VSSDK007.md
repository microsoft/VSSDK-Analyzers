# VSSDK007 Avoid ThreadHelper for fire and forget tasks

A `JoinableTask` created from `ThreadHelper.JoinableTaskFactory.RunAsync` must be awaited or joined.

The `JoinableTaskFactory` instance on [`Microsoft.VisualStudio.Shell.ThreadHelper`][ThreadHelper] produces `JoinableTask`s that do not block exiting the IDE, and is therefore unsuitable for fire and forget tasks that need to complete or cancel before shutdown.

All async work should be tracked so that exiting the IDE waits for the task to complete.
Normally this is done by `await`ing the work directly where it is called.

When using `JoinableTaskFactory.RunAsync` where awaiting the result is not an option,
the easiest way to ensure shutdown is blocked till the task completes is to use the `JoinableTaskFactory` instance on `AsyncPackage`.
Alternatively, when a package is unavailable (e.g. when implementing a MEF part), consider using the [`Community.VisualStudio.Toolkit.ToolkitThreadHelper`][ToolkitThreadHelper] or [implementing your own][FireAndForget] to track the async work and block a `Dispose` method till the work is done.

## Examples of patterns that are flagged by this analyzer

```csharp
class MyCoolPackage : AsyncPackage
{
    public void StartOperation1()
    {
        ThreadHelper.JoinableTaskFactory.RunAsync(async delegate   // warning reported here
        {
            // Some work where we don't care about its result/completion
            await Task.Delay(100);
        });
    }

    public void StartOperation2()
    {
        ThreadHelper.JoinableTaskFactory.RunAsync(async delegate   // warning reported here
        {
            // Some work where we don't care about its result/completion
            await Task.Delay(100);
        }).Task.Forget();

        // Note: Extension methods like Task.Forget and FileAndForget do not make ThreadHelper safe for fire and forget
    }

    public void StartOperation3()
    {
        var task = ThreadHelper.JoinableTaskFactory.RunAsync(async delegate   // warning reported here
        {
            // Some work where we don't care about its result/completion
            await Task.Delay(100);
        });

        // Note: task is not awaited/joined
    }
}
```

## Solution

### Fire and forget

If you want to call `RunAsync` without awaiting it, use the `JoinableTaskFactory` instance from `AsyncPackage` (or an alternative as described above).

```csharp
class MyCoolPackage : AsyncPackage
{
    public void StartOperation1()
    {
        this.JoinableTaskFactory.RunAsync(async delegate
        {
            // Some work where we don't care about its result/completion
            await Task.Delay(100);
        });

        // Note: Prefer an extension method like Task.Forget to signal that the task is fire and forget
    }

    public void StartOperation2()
    {
        this.JoinableTaskFactory.RunAsync(async delegate
        {
            // Some work where we don't care about its result/completion
            await Task.Delay(100);
        }).Task.Forget();
    }

    public void StartOperation3()
    {
        var task = this.JoinableTaskFactory.RunAsync(async delegate
        {
            // Some work where we don't care about its result/completion
            await Task.Delay(100);
        });

        // Note: Prefer an extension method like Task.Forget to signal that the task is fire and forget
    }
}
```

### Await or join

If you didn't intend to use `ThreadHelper.JoinableTaskFactory.RunAsync` in a fire and forget fashion then its result must be awaited or joined.

```csharp
class MyCoolPackage : AsyncPackage
{
    public void PerformOperation()
    {
        ThreadHelper.JoinableTaskFactory.RunAsync(async delegate
        {
            await Task.Delay(100);
        }).Join();  // We meant to synchronously block for the result or completion

        // Or more simply:
        ThreadHelper.JoinableTaskFactory.Run(async delegate
        {
            await Task.Delay(100);
        });
    }

    public async Task PerformOperationAsync()
    {
        var task = ThreadHelper.JoinableTaskFactory.RunAsync(async delegate
        {
            await Task.Delay(100);
        });

        // Perhaps something else here

        // We meant to await for the result or completion
        await task; // or task.Join() to block synchronously if not in an async method.
    }
}
```

[ThreadHelper]: https://docs.microsoft.com/en-us/dotnet/api/microsoft.visualstudio.shell.threadhelper?view=visualstudiosdk-2019
[ToolkitThreadHelper]: https://github.com/VsixCommunity/Community.VisualStudio.Toolkit/blob/22c9362197c29b7282075c3a7550d0445fbb313a/src/Community.VisualStudio.Toolkit.Shared/Helpers/ToolkitThreadHelper.cs#L68
[FireAndForget]: https://github.com/microsoft/vs-threading/blob/main/doc/cookbook_vs.md#void-returning-fire-and-forget-methods
