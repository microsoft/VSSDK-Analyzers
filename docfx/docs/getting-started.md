# Getting started

## Installation

Install the analyzer package into the project that contains your Visual Studio extension:

[![NuGet package](https://img.shields.io/nuget/v/Microsoft.VisualStudio.SDK.Analyzers.svg)](https://www.nuget.org/packages/Microsoft.VisualStudio.SDK.Analyzers)

```xml
<PackageReference Include="Microsoft.VisualStudio.SDK.Analyzers" Version="*" PrivateAssets="all" />
```

The package adds analyzers to the build. `PrivateAssets="all"` keeps the development-time tooling
from becoming a runtime dependency of your extension.

## Using the analyzers

Build your extension normally. Diagnostics appear in the compiler output and Visual Studio Error List.
Some rules include a code fix that can update the source automatically.

Review the [complete analyzer list](../analyzers/index.md) for each rule's rationale, examples, and limitations.
