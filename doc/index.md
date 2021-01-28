# Diagnostic Analyzers

The following are the diagnostic analyzers installed with the [Microsoft.VisualStudio.SDK.Analyzers][1]
NuGet package.

ID | Title | Category
---- | --- | --- |
[VSSDK001](VSSDK001.md) | Derive from AsyncPackage | Performance
[VSSDK002](VSSDK002.md) | PackageRegistration matches Package | Performance
[VSSDK003](VSSDK003.md) | Support async tool windows | Performance
[VSSDK004](VSSDK004.md) | Use BackgroundLoad flag in ProvideAutoLoad attribute | Performance
[VSSDK005](VSSDK005.md) | Use the JoinableTaskContext singleton | Reliability
[VSSDK006](VSSDK006.md) | Check service exists | Reliability

This analyzer package also depends on the [Microsoft.VisualStudio.Threading.Analyzers][2] package, which adds [many more analyzers][3].

[1]: https://nuget.org/packages/microsoft.visualstudio.sdk.analyzers
[2]: https://nuget.org/packages/microsoft.visualstudio.threading.analyzers
[3]: https://github.com/Microsoft/vs-threading/blob/main/doc/analyzers/index.md
