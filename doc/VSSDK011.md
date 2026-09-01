# VSSDK011 Provide services asynchronously from AsyncPackage

Services proffered by an `AsyncPackage` should be asynchronously queryable so that requesting a service
through `IAsyncServiceProvider.GetServiceAsync` does not synchronously load the package.

This analyzer flags:

- `ProvideService` attributes on an `AsyncPackage`-derived class when `IsAsyncQueryable` is omitted
  or set to `false`.
- Services registered on an `AsyncPackage` through the `IServiceContainer.AddService` overload that
  accepts a synchronous `ServiceCreatorCallback`. The overload that accepts a service instance
  directly is allowed.

## Example of a pattern that is flagged by this analyzer

```csharp
[ProvideService(typeof(SMyService))]
class MyPackage : AsyncPackage
{
    private void RegisterService()
    {
        ((IServiceContainer)this).AddService(typeof(SMyService), this.CreateMyService);
    }

    private object CreateMyService(IServiceContainer container, Type serviceType)
    {
        return new MyService();
    }
}
```

## Solution

Set `IsAsyncQueryable` to `true` and register the service with an asynchronous, `Task`-returning service
factory. If the service instance already exists, it may instead be registered directly with the
`IServiceContainer.AddService(Type, object)` overload.

```csharp
[ProvideService(typeof(SMyService), IsAsyncQueryable = true)]
class MyPackage : AsyncPackage
{
    protected override async Task InitializeAsync(
        CancellationToken cancellationToken,
        IProgress<ServiceProgressData> progress)
    {
        await base.InitializeAsync(cancellationToken, progress);
        this.AddService(typeof(SMyService), this.CreateMyServiceAsync);
    }

    private Task<object> CreateMyServiceAsync(
        IAsyncServiceContainer container,
        CancellationToken cancellationToken,
        Type serviceType)
    {
        return Task.FromResult<object>(new MyService());
    }
}
```
