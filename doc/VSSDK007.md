# VSSDK007 Avoid ThreadHelper for fire and forget tasks

Tasks created from `ThreadHelper.JoinableTaskFactory.RunAsync` must be awaited or joined.

The `JoinableTaskFactory` instance on [`Microsoft.VisualStudio.Shell.ThreadHelper`](https://docs.microsoft.com/en-us/dotnet/api/microsoft.visualstudio.shell.threadhelper?view=visualstudiosdk-2019) is unsuitable for fire and forget tasks. Prefer the `JoinableTaskFactory` instance on `AsyncPackage` or, when a package is unavailable (e.g. MEF), consider [implementing](https://github.com/microsoft/vs-threading/blob/main/doc/cookbook_vs.md#void-returning-fire-and-forget-methods) your own equivalent like [`Community.VisualStudio.Toolkit.ToolkitThreadHelper`](https://github.com/VsixCommunity/Community.VisualStudio.Toolkit/blob/master/src/Community.VisualStudio.Toolkit.Shared/ToolkitThreadHelper.cs).

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
		}).Join();  // We meant to wait for the result or completion
		
		// Or even better:
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
	
		// We meant to wait for the result or completion
		await task;  // or task.Join()
    }
}
```
