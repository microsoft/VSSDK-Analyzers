// Copyright (c) Microsoft Corporation. All rights reserved.

using System;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.VisualStudio.SDK.Analyzers;
using Microsoft.VisualStudio.SDK.Analyzers.Tests;
using Xunit;
using Xunit.Abstractions;

public class VSSDK002PackageRegistrationMatchesBaseTypeAnalyzerTests : DiagnosticVerifier
{
    private DiagnosticResult expect = new DiagnosticResult
    {
        Id = VSSDK002PackageRegistrationMatchesBaseTypeAnalyzer.Id,
        SkipVerifyMessage = true,
        Severity = DiagnosticSeverity.Error,
    };

    public VSSDK002PackageRegistrationMatchesBaseTypeAnalyzerTests(ITestOutputHelper logger)
        : base(logger)
    {
    }

    [Fact]
    public void AsyncPackageWithNoAttributeProducesNoDiagnostic()
    {
        var test = @"
using Microsoft.VisualStudio.Shell;

class Test : AsyncPackage {
}
";

        this.VerifyCSharpDiagnostic(test);
    }

    [Fact]
    public void PackageRegistrationWithNoBaseType()
    {
        var test = @"
using Microsoft.VisualStudio.Shell;

[PackageRegistration(UseManagedResourcesOnly = true)]
class Test {
}
";

        this.VerifyCSharpDiagnostic(test);
    }

    [Fact]
    public void PackageRegistrationWithIrrelevant()
    {
        var test = @"
using System;
using Microsoft.VisualStudio.Shell;

[PackageRegistration(UseManagedResourcesOnly = true)]
class Test : IDisposable {
    public void Dispose() { }
}
";

        this.VerifyCSharpDiagnostic(test);
    }

    [Fact]
    public void AsyncPackageMatchProducesNoDiagnostic()
    {
        var test = @"
using Microsoft.VisualStudio.Shell;

[PackageRegistration(UseManagedResourcesOnly = true, AllowsBackgroundLoading = true)]
class Test : AsyncPackage {
}
";

        this.VerifyCSharpDiagnostic(test);
    }

    [Fact]
    public void PackageMatchProducesNoDiagnostic()
    {
        var test = @"
using Microsoft.VisualStudio.Shell;

[PackageRegistration(UseManagedResourcesOnly = true)]
class MyCoolPackage : Package {
}
";

        this.VerifyCSharpDiagnostic(test);
    }

    [Fact]
    public void AsyncPackageMismatchProducesDiagnostic()
    {
        var test = @"
using Microsoft.VisualStudio.Shell;

[PackageRegistration(UseManagedResourcesOnly = true)]
class Test : AsyncPackage {
}
";

        this.expect.Locations = new[] { new DiagnosticResultLocation("Test0.cs", 5, 14, 5, 26) };
        this.VerifyCSharpDiagnostic(test, this.expect);
    }

    [Fact]
    public void PackageMismatchProducesDiagnostic()
    {
        var test = @"
using Microsoft.VisualStudio.Shell;

[PackageRegistration(UseManagedResourcesOnly = true, AllowsBackgroundLoading = true)]
class MyCoolPackage : Package {
}
";

        this.expect.Locations = new[] { new DiagnosticResultLocation("Test0.cs", 4, 54, 4, 84) };
        this.VerifyCSharpDiagnostic(test, this.expect);
    }

    protected override DiagnosticAnalyzer GetCSharpDiagnosticAnalyzer() => new VSSDK002PackageRegistrationMatchesBaseTypeAnalyzer();
}
