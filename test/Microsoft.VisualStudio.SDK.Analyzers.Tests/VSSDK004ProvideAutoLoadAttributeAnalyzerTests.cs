// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Testing;
using Verify = CSharpCodeFixVerifier<
    Microsoft.VisualStudio.SDK.Analyzers.VSSDK004ProvideAutoLoadAttributeAnalyzer,
    Microsoft.CodeAnalysis.Testing.EmptyCodeFixProvider>;

public class VSSDK004ProvideAutoLoadAttributeAnalyzerTests
{
    [Test]
    public async Task NoPackageBaseClassProvidesNoDiagnosticsAsync()
    {
        var test = /* lang=c#-test */ @"
using Microsoft.VisualStudio.Shell;

[ProvideAutoLoad(""{F184B08F-C81C-45F6-A57F-5ABD9991F28F}"")]
class Test {
}
";
        await Verify.VerifyAnalyzerAsync(test);
    }

    [Test]
    public async Task NoProvideAutoLoadProducesNoDiagnosticsAsync()
    {
        var test = /* lang=c#-test */ @"
using Microsoft.VisualStudio.Shell;

class Test : AsyncPackage {
}
";
        await Verify.VerifyAnalyzerAsync(test);
    }

    [Test]
    public async Task BasicProvideAutoLoadProducesDiagnosticsAsync()
    {
        var test = /* lang=c#-test */ @"
using Microsoft.VisualStudio.Shell;

[ProvideAutoLoad(""{F184B08F-C81C-45F6-A57F-5ABD9991F28F}"")]
class Test : AsyncPackage {
}
";
        DiagnosticResult expected = Verify.Diagnostic().WithSpan(4, 2, 4, 59);
        await Verify.VerifyAnalyzerAsync(test, expected);
    }

    [Test]
    public async Task ProvideAutoLoadWithFlagsButNoBackgroundLoadProducesDiagnosticsAsync()
    {
        var test = /* lang=c#-test */ @"
using Microsoft.VisualStudio.Shell;

[ProvideAutoLoad(""{F184B08F-C81C-45F6-A57F-5ABD9991F28F}"", PackageAutoLoadFlags.None)]
class Test : AsyncPackage {
}
";
        DiagnosticResult expected = Verify.Diagnostic().WithSpan(4, 2, 4, 86);
        await Verify.VerifyAnalyzerAsync(test, expected);
    }

    [Test]
    public async Task MultipleProvideAutoLoadProducesMultipleDiagnosticsAsync()
    {
        var test = /* lang=c#-test */ @"
using Microsoft.VisualStudio.Shell;

[ProvideAutoLoad(""{A184B08F-C81C-45F6-A57F-5ABD9991F28F}"", PackageAutoLoadFlags.None)]
[ProvideAutoLoad(""{F184B08F-C81C-45F6-A57F-5ABD9991F28F}"", PackageAutoLoadFlags.None)]
class Test : AsyncPackage {
}
";
        DiagnosticResult[] expected =
        {
            Verify.Diagnostic().WithSpan(4, 2, 4, 86),
            Verify.Diagnostic().WithSpan(5, 2, 5, 86),
        };
        await Verify.VerifyAnalyzerAsync(test, expected);
    }

    [Test]
    public async Task ProvideAutoLoadWithNamedFlagsButNoBackgroundLoadProducesDiagnosticsAsync()
    {
        var test = /* lang=c#-test */ @"
using Microsoft.VisualStudio.Shell;

[ProvideAutoLoad(""{F184B08F-C81C-45F6-A57F-5ABD9991F28F}"", flags: PackageAutoLoadFlags.None)]
class Test : AsyncPackage {
}
";
        DiagnosticResult expected = Verify.Diagnostic().WithSpan(4, 2, 4, 93);
        await Verify.VerifyAnalyzerAsync(test, expected);
    }

    [Test]
    public async Task ProvideAutoLoadWithSkipFlagProducesNoDiagnosticsAsync()
    {
        var test = /* lang=c#-test */ @"
using Microsoft.VisualStudio.Shell;

[ProvideAutoLoad(""{F184B08F-C81C-45F6-A57F-5ABD9991F28F}"", PackageAutoLoadFlags.SkipWhenUIContextRulesActive)]
class Test : Package {
}
";
        await Verify.VerifyAnalyzerAsync(test);
    }

    [Test]
    public async Task ProvideAutoLoadWithBackgroundLoadFlagProducesNoDiagnosticsAsync()
    {
        var test = /* lang=c#-test */ @"
using Microsoft.VisualStudio.Shell;

[ProvideAutoLoad(""{F184B08F-C81C-45F6-A57F-5ABD9991F28F}"", PackageAutoLoadFlags.BackgroundLoad)]
class Test : AsyncPackage {
}
";
        await Verify.VerifyAnalyzerAsync(test);
    }

    [Test]
    public async Task ProvideAutoLoadOnPackageWithBackgroundLoadFlagProducesDiagnosticsAsync()
    {
        var test = /* lang=c#-test */ @"
using Microsoft.VisualStudio.Shell;

[ProvideAutoLoad(""{F184B08F-C81C-45F6-A57F-5ABD9991F28F}"", PackageAutoLoadFlags.BackgroundLoad)]
class Test : Package {
}
";
        DiagnosticResult expected = Verify.Diagnostic().WithSpan(4, 2, 4, 96);
        await Verify.VerifyAnalyzerAsync(test, expected);
    }
}