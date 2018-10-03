# VSSDK006 Check service exists

`IServiceProvider.GetService` and `IAsyncServiceProvider.GetServiceAsync` may return null in any of several conditions, including:

1. The service is not installed.
2. The Package that proffers the service fails to initialize itself or the service.
3. Visual Studio is running in "safe mode" and the package proffering the service is not allowed to load.
4. Visual Studio is shutting down, and not activating services any more. With the proliferation of async packages and services, this happens with increasing frequency even for core services, so *all* service queries should be checked for null.

Most of these conditions are *not* failures in the system but rather ordinary conditions that can occur that callers should be prepared for.
Always check the result of service queries before dereferencing those services.
If reduced functionality is an option when a service is absent, that is preferred.

If the service is mandatory for proper functioning of your feature, use `Assumes.Present<T>(T)` to check that the value is non-null and throw an informative exception if it is null that tells you exactly which service was not present. This is preferable to skipping the check and letting a `NullReferenceException` be thrown because `NullReferenceException` merely tells you that *some* dereference was on a null variable, but it doesn't tell you which one, leading to a much longer investigation into the failure.

Alternatively, if throwing an exception in your context will lead to a product crash, display an error to the user and exit your feature.

Crashing the product leads to data loss and/or the inability of the user to use other areas of the product. Deactivating your own feature when it does not have what it needs to run is therefore preferable.

## Examples of patterns that are flagged by this analyzer

```csharp
class MyCoolPackage : AsyncPackage
{
    public async Task InitializeAsync(/*...*/)
    {
        var bma = await this.GetServiceAsync(typeof(IVsBuildManagerAccessor));
        bma.BeginDesignTimeBuild(); // warning reported here
    }
}
```

## Solution

Check for null first, using either `Assumes.Present` or an `if` check:

```csharp
using Microsoft;

class MyCoolPackage : AsyncPackage
{
    public async Task InitializeAsync(/*...*/)
    {
        var bma = await this.GetServiceAsync(typeof(IVsBuildManagerAccessor));
        Assumes.Present(bma);
        bma.BeginDesignTimeBuild();
    }
}
```

or

```csharp
using Microsoft;

class MyCoolPackage : AsyncPackage
{
    public async Task InitializeAsync(/*...*/)
    {
        var bma = await this.GetServiceAsync(typeof(IVsBuildManagerAccessor));
        if (bma != null) {
            bma.BeginDesignTimeBuild();
        }
    }
}
```
