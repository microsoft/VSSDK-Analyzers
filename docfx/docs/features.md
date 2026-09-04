# Features

## Visual Studio extension guidance

The analyzers cover common performance, reliability, and threading practices for Visual Studio packages
and MEF components. They identify issues such as synchronous package loading, unsafe service assumptions,
and UI-thread affinity during composition.

## Code fixes

Where a safe transformation is available, the package offers a code fix directly in the IDE.
Rules whose fixes could change behavior provide guidance only.

## Complementary analyzers

This package also depends on [Microsoft.VisualStudio.Threading.Analyzers](https://www.nuget.org/packages/Microsoft.VisualStudio.Threading.Analyzers),
which adds additional threading diagnostics to Visual Studio extension projects.
