// Copyright (c) Microsoft Corporation. All rights reserved.

using System;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.VisualStudio.SDK.Analyzers;
using Microsoft.VisualStudio.SDK.Analyzers.Tests;
using Xunit;
using Xunit.Abstractions;

public class VSSDK001DeriveFromAsyncPackageTests : DiagnosticVerifier
{
    private DiagnosticResult expect = new DiagnosticResult
    {
        Id = VSSDK001DeriveFromAsyncPackage.Id,
        SkipVerifyMessage = true,
        Severity = DiagnosticSeverity.Info,
    };

    public VSSDK001DeriveFromAsyncPackageTests(ITestOutputHelper logger)
        : base(logger)
    {
    }

    [Fact]
    public void AsyncPackageDerivedClassProducesNoDiagnostic()
    {
        var test = @"
using Microsoft.VisualStudio.Shell;

class Test : AsyncPackage {
}
";

        this.VerifyCSharpDiagnostic(test);
    }

    [Fact]
    public void NoBaseTypeProducesNoDiagnostic()
    {
        var test = @"
class Test {
}
";

        this.VerifyCSharpDiagnostic(test);
    }

    [Fact]
    public void PackageDerivedClassProducesDiagnostic()
    {
        var test = @"
using Microsoft.VisualStudio.Shell;

class Test : Package {
}
";

        this.expect.Locations = new[] { new DiagnosticResultLocation("Test0.cs", 4, 14, 4, 21) };
        this.VerifyCSharpDiagnostic(test, this.expect);
    }

    [Fact]
    public void PackageDerivedClassWithInterfacesProducesDiagnostic()
    {
        var test = @"
using System;
using Microsoft.VisualStudio.Shell;

class Test : Package, IDisposable {
    public void Dispose() { }
}
";

        this.expect.Locations = new[] { new DiagnosticResultLocation("Test0.cs", 5, 14, 5, 21) };
        this.VerifyCSharpDiagnostic(test, this.expect);
    }

    protected override DiagnosticAnalyzer GetCSharpDiagnosticAnalyzer() => new VSSDK001DeriveFromAsyncPackage();
}
