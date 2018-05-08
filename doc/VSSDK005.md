# VSSDK005 Use the JoinableTaskContext singleton 

Code running within the Visual Studio process (and primary AppDomain)
should avoid creating a new instance of [`JoinableTaskContext`][JTC],
as doing so may introduce deadlocks. Instead, everyone should share the
`JoinableTaskContext` instance offered by [`ThreadHelper.JoinableTaskContext`][TH].

This analyzer may be turned off for unit test projects,
which may have a legitimate need to create a JoinableTaskContext.

## Examples of patterns that are flagged by this analyzer

```csharp
class MyCoolPackage : Package
{
    void Foo()
    {
        var jtc = new JoinableTaskContext(); // this line flagged
    }
}
```

## Solution

Use the `JoinableTaskContext` available from the `ThreadHelper` class.

```csharp
class MyCoolPackage : Package
{
    void Foo()
    {
        var jtc = ThreadHelper.JoinableTaskContext;
    }
}
```

[JTC]: https://docs.microsoft.com/en-us/dotnet/api/microsoft.visualstudio.threading.joinabletaskcontext?view=visualstudiosdk-2017
[TH]: https://docs.microsoft.com/en-us/dotnet/api/microsoft.visualstudio.shell.threadhelper.joinabletaskcontext?view=visualstudiosdk-2017
