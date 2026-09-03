// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Threading.Tasks;
using Xunit;
using Verify = CSharpCodeFixVerifier<
    Microsoft.VisualStudio.SDK.Analyzers.VSSDK010RemoveUnnecessaryProvideAutoLoadAttributeAnalyzer,
    Microsoft.CodeAnalysis.Testing.EmptyCodeFixProvider>;

public class VSSDK010RemoveUnnecessaryProvideAutoLoadAttributeAnalyzerTests
{
    [Fact]
    public async Task PackageWithoutInitializeOverrideProducesDiagnosticAsync()
    {
        var test = /* lang=c#-test */ @"
using Microsoft.VisualStudio.Shell;

[{|#0:ProvideAutoLoad(""{F184B08F-C81C-45F6-A57F-5ABD9991F28F}"")|}]
class Test : Package
{
}
";

        await Verify.VerifyAnalyzerAsync(test, Verify.Diagnostic().WithLocation(0).WithArguments("Initialize"));
    }

    [Fact]
    public async Task AsyncPackageWithoutInitializeAsyncOverrideProducesDiagnosticAsync()
    {
        var test = /* lang=c#-test */ @"
using Microsoft.VisualStudio.Shell;

[{|#0:ProvideAutoLoad(""{F184B08F-C81C-45F6-A57F-5ABD9991F28F}"", PackageAutoLoadFlags.BackgroundLoad)|}]
class Test : AsyncPackage
{
}
";

        await Verify.VerifyAnalyzerAsync(test, Verify.Diagnostic().WithLocation(0).WithArguments("InitializeAsync"));
    }

    [Fact]
    public async Task MultipleProvideAutoLoadAttributesProduceMultipleDiagnosticsAsync()
    {
        var test = /* lang=c#-test */ @"
using Microsoft.VisualStudio.Shell;

[{|#0:ProvideAutoLoad(""{A184B08F-C81C-45F6-A57F-5ABD9991F28F}"")|}]
[{|#1:ProvideAutoLoad(""{F184B08F-C81C-45F6-A57F-5ABD9991F28F}"")|}]
class Test : Package
{
}
";

        await Verify.VerifyAnalyzerAsync(
            test,
            Verify.Diagnostic().WithLocation(0).WithArguments("Initialize"),
            Verify.Diagnostic().WithLocation(1).WithArguments("Initialize"));
    }

    [Fact]
    public async Task PackageWithInitializeOverrideProducesNoDiagnosticAsync()
    {
        var test = /* lang=c#-test */ @"
using Microsoft.VisualStudio.Shell;

[ProvideAutoLoad(""{F184B08F-C81C-45F6-A57F-5ABD9991F28F}"")]
class Test : Package
{
    protected override void Initialize()
    {
        base.Initialize();
    }
}
";

        await Verify.VerifyAnalyzerAsync(test);
    }

    [Fact]
    public async Task AsyncPackageWithInitializeAsyncOverrideProducesNoDiagnosticAsync()
    {
        var test = /* lang=c#-test */ @"
using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.Shell;

[ProvideAutoLoad(""{F184B08F-C81C-45F6-A57F-5ABD9991F28F}"", PackageAutoLoadFlags.BackgroundLoad)]
class Test : AsyncPackage
{
    protected override Task InitializeAsync(CancellationToken cancellationToken, IProgress<ServiceProgressData> progress)
    {
        return base.InitializeAsync(cancellationToken, progress);
    }
}
";

        await Verify.VerifyAnalyzerAsync(test);
    }

    [Fact]
    public async Task InheritedInitializeAsyncOverrideProducesNoDiagnosticAsync()
    {
        var test = /* lang=c#-test */ @"
using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.Shell;

abstract class BasePackage : AsyncPackage
{
    protected override Task InitializeAsync(CancellationToken cancellationToken, IProgress<ServiceProgressData> progress)
    {
        return base.InitializeAsync(cancellationToken, progress);
    }
}

[ProvideAutoLoad(""{F184B08F-C81C-45F6-A57F-5ABD9991F28F}"", PackageAutoLoadFlags.BackgroundLoad)]
class Test : BasePackage
{
}
";

        await Verify.VerifyAnalyzerAsync(test);
    }

    [Fact]
    public async Task ProvideAutoLoadOnAbstractBasePackageProducesNoDiagnosticAsync()
    {
        var test = /* lang=c#-test */ @"
using Microsoft.VisualStudio.Shell;

[ProvideAutoLoad(""{F184B08F-C81C-45F6-A57F-5ABD9991F28F}"")]
abstract class BasePackage : Package
{
}

class Test : BasePackage
{
    protected override void Initialize()
    {
        base.Initialize();
    }
}
";

        await Verify.VerifyAnalyzerAsync(test);
    }

    [Fact]
    public async Task UnrelatedInitializeMethodStillProducesDiagnosticAsync()
    {
        var test = /* lang=c#-test */ @"
using Microsoft.VisualStudio.Shell;

[{|#0:ProvideAutoLoad(""{F184B08F-C81C-45F6-A57F-5ABD9991F28F}"")|}]
class Test : Package
{
    private void Initialize(int value)
    {
    }
}
";

        await Verify.VerifyAnalyzerAsync(test, Verify.Diagnostic().WithLocation(0).WithArguments("Initialize"));
    }

    [Fact]
    public async Task ProvideAutoLoadOnNonPackageProducesNoDiagnosticAsync()
    {
        var test = /* lang=c#-test */ @"
using Microsoft.VisualStudio.Shell;

[ProvideAutoLoad(""{F184B08F-C81C-45F6-A57F-5ABD9991F28F}"")]
class Test
{
}
";

        await Verify.VerifyAnalyzerAsync(test);
    }

    [Fact]
    public async Task PackageWithoutProvideAutoLoadProducesNoDiagnosticAsync()
    {
        var test = /* lang=c#-test */ @"
using Microsoft.VisualStudio.Shell;

class Test : Package
{
}
";

        await Verify.VerifyAnalyzerAsync(test);
    }
}
