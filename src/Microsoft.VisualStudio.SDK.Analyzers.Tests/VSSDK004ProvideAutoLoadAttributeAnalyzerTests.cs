// Copyright (c) Microsoft Corporation. All rights reserved.

using System;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.VisualStudio.SDK.Analyzers;
using Microsoft.VisualStudio.SDK.Analyzers.Tests;
using Xunit;
using Xunit.Abstractions;

public class VSSDK004ProvideAutoLoadAttributeAnalyzerTests : DiagnosticVerifier
{
    private DiagnosticResult expect = new DiagnosticResult
    {
        Id = VSSDK004ProvideAutoLoadAttributeAnalyzer.Id,
        SkipVerifyMessage = true,
        Severity = DiagnosticSeverity.Info,
    };

    public VSSDK004ProvideAutoLoadAttributeAnalyzerTests(ITestOutputHelper logger)
        : base(logger)
    {
    }

    [Fact]
    public void NoProvideAutoLoadProducesNoDiagnostics()
    {
        var test = @"
using Microsoft.VisualStudio.Shell;

class Test : AsyncPackage {
}
";
        this.VerifyCSharpDiagnostic(test);
    }

    [Fact]
    public void BasicProvideAutoLoadProducesDiagnostics()
    {
        var test = @"
using Microsoft.VisualStudio.Shell;

[ProvideAutoLoad(""{F184B08F-C81C-45F6-A57F-5ABD9991F28F}"")]
class Test : AsyncPackage {
}
";
        this.expect.Locations = new[] { new DiagnosticResultLocation("Test0.cs", 4, 2, 4, 59) };
        this.VerifyCSharpDiagnostic(test, this.expect);
    }

    [Fact]
    public void ProvideAutoLoadWithFlagsButNoBackgroundLoadProducesDiagnostics()
    {
        var test = @"
using Microsoft.VisualStudio.Shell;

[ProvideAutoLoad(""{F184B08F-C81C-45F6-A57F-5ABD9991F28F}"", PackageAutoLoadFlags.None)]
class Test : AsyncPackage {
}
";
        this.expect.Locations = new[] { new DiagnosticResultLocation("Test0.cs", 4, 2, 4, 86) };
        this.VerifyCSharpDiagnostic(test, this.expect);
    }

    [Fact]
    public void ProvideAutoLoadWithNamedFlagsButNoBackgroundLoadProducesDiagnostics()
    {
        var test = @"
using Microsoft.VisualStudio.Shell;

[ProvideAutoLoad(""{F184B08F-C81C-45F6-A57F-5ABD9991F28F}"", flags: PackageAutoLoadFlags.None)]
class Test : AsyncPackage {
}
";
        this.expect.Locations = new[] { new DiagnosticResultLocation("Test0.cs", 4, 2, 4, 93) };
        this.VerifyCSharpDiagnostic(test, this.expect);
    }

    [Fact]
    public void ProvideAutoLoadWithSkipFlagProducesNoDiagnostics()
    {
        var test = @"
using Microsoft.VisualStudio.Shell;

[ProvideAutoLoad(""{F184B08F-C81C-45F6-A57F-5ABD9991F28F}"", PackageAutoLoadFlags.SkipWhenUIContextRulesActive)]
class Test : AsyncPackage {
}
";
        this.VerifyCSharpDiagnostic(test);
    }

    [Fact]
    public void ProvideAutoLoadWithBackgroundLoadFlagProducesNoDiagnostics()
    {
        var test = @"
using Microsoft.VisualStudio.Shell;

[ProvideAutoLoad(""{F184B08F-C81C-45F6-A57F-5ABD9991F28F}"", PackageAutoLoadFlags.BackgroundLoad)]
class Test : AsyncPackage {
}
";
        this.VerifyCSharpDiagnostic(test);
    }

    [Fact]
    public void ProvideAutoLoadOnPackageWithBackgroundLoadFlagProducesNoDiagnostics()
    {
        var test = @"
using Microsoft.VisualStudio.Shell;

[ProvideAutoLoad(""{F184B08F-C81C-45F6-A57F-5ABD9991F28F}"", PackageAutoLoadFlags.BackgroundLoad)]
class Test : Package {
}
";
        this.VerifyCSharpDiagnostic(test);
    }

    protected override DiagnosticAnalyzer GetCSharpDiagnosticAnalyzer() => new VSSDK004ProvideAutoLoadAttributeAnalyzer();
}
