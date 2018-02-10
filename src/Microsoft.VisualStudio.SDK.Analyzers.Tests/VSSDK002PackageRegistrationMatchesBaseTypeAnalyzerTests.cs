// Copyright (c) Microsoft Corporation. All rights reserved.

using System;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.VisualStudio.SDK.Analyzers;
using Microsoft.VisualStudio.SDK.Analyzers.Tests;
using Xunit;
using Xunit.Abstractions;

public class VSSDK002PackageRegistrationMatchesBaseTypeAnalyzerTests : CodeFixVerifier
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
    public void AsyncPackageImplicitMismatchProducesDiagnostic()
    {
        var test = @"
using Microsoft.VisualStudio.Shell;

[PackageRegistration(UseManagedResourcesOnly = true)]
class Test : AsyncPackage {
}
";
        var withFix = @"
using Microsoft.VisualStudio.Shell;

[PackageRegistration(UseManagedResourcesOnly = true, AllowsBackgroundLoading = true)]
class Test : AsyncPackage {
}
";

        this.expect.Locations = new[] { new DiagnosticResultLocation("Test0.cs", 4, 2, 4, 53) };
        this.VerifyCSharpDiagnostic(test, this.expect);
        this.VerifyCSharpFix(test, withFix);
    }

    [Fact]
    public void AsyncPackageImplicitMismatchViaIntermediateClass_ProducesDiagnostic()
    {
        var test = @"
using Microsoft.VisualStudio.Shell;

class Middle : AsyncPackage { }

[PackageRegistration(UseManagedResourcesOnly = true)]
class Test : Middle {
}
";
        var withFix = @"
using Microsoft.VisualStudio.Shell;

class Middle : AsyncPackage { }

[PackageRegistration(UseManagedResourcesOnly = true, AllowsBackgroundLoading = true)]
class Test : Middle {
}
";

        this.expect.Locations = new[] { new DiagnosticResultLocation("Test0.cs", 6, 2, 6, 53) };
        this.VerifyCSharpDiagnostic(test, this.expect);
        this.VerifyCSharpFix(test, withFix);
    }

    [Fact]
    public void PackageMismatchViaIntermediateClass_ProducesDiagnostic()
    {
        var test = @"
using Microsoft.VisualStudio.Shell;

class Middle : Package { }

[PackageRegistration(UseManagedResourcesOnly = true, AllowsBackgroundLoading = true)]
class MyCoolPackage : Middle {
}
";
        var withFix = @"
using Microsoft.VisualStudio.Shell;

class Middle : Package { }

[PackageRegistration(UseManagedResourcesOnly = true)]
class MyCoolPackage : Middle {
}
";

        this.expect.Locations = new[] { new DiagnosticResultLocation("Test0.cs", 6, 54, 6, 84) };
        this.VerifyCSharpDiagnostic(test, this.expect);
        this.VerifyCSharpFix(test, withFix);
    }

    [Fact]
    public void AsyncPackageExplicitMismatchProducesDiagnostic()
    {
        var test = @"
using Microsoft.VisualStudio.Shell;

[PackageRegistration(UseManagedResourcesOnly = true, AllowsBackgroundLoading = false)]
class Test : AsyncPackage {
}
";
        var withFix = @"
using Microsoft.VisualStudio.Shell;

[PackageRegistration(UseManagedResourcesOnly = true, AllowsBackgroundLoading = true)]
class Test : AsyncPackage {
}
";

        this.expect.Locations = new[] { new DiagnosticResultLocation("Test0.cs", 4, 54, 4, 85) };
        this.VerifyCSharpDiagnostic(test, this.expect);
        this.VerifyCSharpFix(test, withFix);
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
        var withFix = @"
using Microsoft.VisualStudio.Shell;

[PackageRegistration(UseManagedResourcesOnly = true)]
class MyCoolPackage : Package {
}
";

        this.expect.Locations = new[] { new DiagnosticResultLocation("Test0.cs", 4, 54, 4, 84) };
        this.VerifyCSharpDiagnostic(test, this.expect);
        this.VerifyCSharpFix(test, withFix);
    }

    [Fact]
    public void PackageMismatchProducesDiagnostic_ReverseArgumentOrder()
    {
        var test = @"
using Microsoft.VisualStudio.Shell;

[PackageRegistration(AllowsBackgroundLoading = true, UseManagedResourcesOnly = true)]
class MyCoolPackage : Package {
}
";
        var withFix = @"
using Microsoft.VisualStudio.Shell;

[PackageRegistration(UseManagedResourcesOnly = true)]
class MyCoolPackage : Package {
}
";

        this.expect.Locations = new[] { new DiagnosticResultLocation("Test0.cs", 4, 22, 4, 52) };
        this.VerifyCSharpDiagnostic(test, this.expect);
        this.VerifyCSharpFix(test, withFix);
    }

    [Fact]
    public void PackageMismatchProducesDiagnostic_AcrossPartialClass()
    {
        var test = @"
using Microsoft.VisualStudio.Shell;

partial class MyCoolPackage : Package {
}

[PackageRegistration(AllowsBackgroundLoading = true, UseManagedResourcesOnly = true)]
partial class MyCoolPackage { }
";
        var withFix = @"
using Microsoft.VisualStudio.Shell;

partial class MyCoolPackage : Package {
}

[PackageRegistration(UseManagedResourcesOnly = true)]
partial class MyCoolPackage { }
";

        this.expect.Locations = new[] { new DiagnosticResultLocation("Test0.cs", 7, 22, 7, 52) };
        this.VerifyCSharpDiagnostic(test, this.expect);
        this.VerifyCSharpFix(test, withFix);
    }

    [Fact]
    public void PackageMatchViaIntermediateClass_ProducesNoDiagnostic()
    {
        var test = @"
using Microsoft.VisualStudio.Shell;

class Middle : Package { }

[PackageRegistration(UseManagedResourcesOnly = true)]
class Test : Middle {
}
";
        this.VerifyCSharpDiagnostic(test);
    }

    [Fact]
    public void AsyncPackageMatchViaIntermediateClass_ProducesNoDiagnostic()
    {
        var test = @"
using Microsoft.VisualStudio.Shell;

class Middle : AsyncPackage { }

[PackageRegistration(UseManagedResourcesOnly = true, AllowsBackgroundLoading = true)]
class Test : Middle {
}
";
        this.VerifyCSharpDiagnostic(test);
    }

    protected override DiagnosticAnalyzer GetCSharpDiagnosticAnalyzer() => new VSSDK002PackageRegistrationMatchesBaseTypeAnalyzer();

    protected override CodeFixProvider GetCSharpCodeFixProvider() => new VSSDK002PackageRegistrationMatchesBaseTypeCodeFix();
}
