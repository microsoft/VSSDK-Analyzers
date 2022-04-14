// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Threading.Tasks;
using Xunit;
using Verify = CSharpCodeFixVerifier<
    Microsoft.VisualStudio.SDK.Analyzers.VSSDK002PackageRegistrationMatchesBaseTypeAnalyzer,
    Microsoft.VisualStudio.SDK.Analyzers.VSSDK002PackageRegistrationMatchesBaseTypeCodeFix>;

public class VSSDK002PackageRegistrationMatchesBaseTypeAnalyzerTests
{
    [Fact]
    public async Task AsyncPackageWithNoAttributeProducesNoDiagnosticAsync()
    {
        var test = @"
using Microsoft.VisualStudio.Shell;

class Test : AsyncPackage {
}
";

        await Verify.VerifyAnalyzerAsync(test);
    }

    [Fact]
    public async Task PackageRegistrationWithNoBaseTypeAsync()
    {
        var test = @"
using Microsoft.VisualStudio.Shell;

[PackageRegistration(UseManagedResourcesOnly = true)]
class Test {
}
";

        await Verify.VerifyAnalyzerAsync(test);
    }

    [Fact]
    public async Task PackageRegistrationWithIrrelevantAsync()
    {
        var test = @"
using System;
using Microsoft.VisualStudio.Shell;

[PackageRegistration(UseManagedResourcesOnly = true)]
class Test : IDisposable {
    public void Dispose() { }
}
";

        await Verify.VerifyAnalyzerAsync(test);
    }

    [Fact]
    public async Task AsyncPackageMatchProducesNoDiagnosticAsync()
    {
        var test = @"
using Microsoft.VisualStudio.Shell;

[PackageRegistration(UseManagedResourcesOnly = true, AllowsBackgroundLoading = true)]
class Test : AsyncPackage {
}
";

        await Verify.VerifyAnalyzerAsync(test);
    }

    [Fact]
    public async Task PackageMatchProducesNoDiagnosticAsync()
    {
        var test = @"
using Microsoft.VisualStudio.Shell;

[PackageRegistration(UseManagedResourcesOnly = true)]
class MyCoolPackage : Package {
}
";

        await Verify.VerifyAnalyzerAsync(test);
    }

    [Fact]
    public async Task AsyncPackageImplicitMismatchProducesDiagnosticAsync()
    {
        var test = @"
using Microsoft.VisualStudio.Shell;

[[|PackageRegistration(UseManagedResourcesOnly = true)|]]
class Test : AsyncPackage {
}
";
        var withFix = @"
using Microsoft.VisualStudio.Shell;

[PackageRegistration(UseManagedResourcesOnly = true, AllowsBackgroundLoading = true)]
class Test : AsyncPackage {
}
";

        await Verify.VerifyCodeFixAsync(test, withFix);
    }

    [Fact]
    public async Task AsyncPackageImplicitMismatchViaIntermediateClass_ProducesDiagnosticAsync()
    {
        var test = @"
using Microsoft.VisualStudio.Shell;

class Middle : AsyncPackage { }

[[|PackageRegistration(UseManagedResourcesOnly = true)|]]
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

        await Verify.VerifyCodeFixAsync(test, withFix);
    }

    [Fact]
    public async Task PackageMismatchViaIntermediateClass_ProducesDiagnosticAsync()
    {
        var test = @"
using Microsoft.VisualStudio.Shell;

class Middle : Package { }

[PackageRegistration(UseManagedResourcesOnly = true, [|AllowsBackgroundLoading = true|])]
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

        await Verify.VerifyCodeFixAsync(test, withFix);
    }

    [Fact]
    public async Task AsyncPackageExplicitMismatchProducesDiagnosticAsync()
    {
        var test = @"
using Microsoft.VisualStudio.Shell;

[PackageRegistration(UseManagedResourcesOnly = true, [|AllowsBackgroundLoading = false|])]
class Test : AsyncPackage {
}
";
        var withFix = @"
using Microsoft.VisualStudio.Shell;

[PackageRegistration(UseManagedResourcesOnly = true, AllowsBackgroundLoading = true)]
class Test : AsyncPackage {
}
";

        await Verify.VerifyCodeFixAsync(test, withFix);
    }

    [Fact]
    public async Task PackageMismatchProducesDiagnosticAsync()
    {
        var test = @"
using Microsoft.VisualStudio.Shell;

[PackageRegistration(UseManagedResourcesOnly = true, [|AllowsBackgroundLoading = true|])]
class MyCoolPackage : Package {
}
";
        var withFix = @"
using Microsoft.VisualStudio.Shell;

[PackageRegistration(UseManagedResourcesOnly = true)]
class MyCoolPackage : Package {
}
";

        await Verify.VerifyCodeFixAsync(test, withFix);
    }

    [Fact]
    public async Task PackageMismatchProducesDiagnostic_ReverseArgumentOrderAsync()
    {
        var test = @"
using Microsoft.VisualStudio.Shell;

[PackageRegistration([|AllowsBackgroundLoading = true|], UseManagedResourcesOnly = true)]
class MyCoolPackage : Package {
}
";
        var withFix = @"
using Microsoft.VisualStudio.Shell;

[PackageRegistration(UseManagedResourcesOnly = true)]
class MyCoolPackage : Package {
}
";

        await Verify.VerifyCodeFixAsync(test, withFix);
    }

    [Fact]
    public async Task PackageMismatchProducesDiagnostic_AcrossPartialClassAsync()
    {
        var test = @"
using Microsoft.VisualStudio.Shell;

partial class MyCoolPackage : Package {
}

[PackageRegistration([|AllowsBackgroundLoading = true|], UseManagedResourcesOnly = true)]
partial class MyCoolPackage { }
";
        var withFix = @"
using Microsoft.VisualStudio.Shell;

partial class MyCoolPackage : Package {
}

[PackageRegistration(UseManagedResourcesOnly = true)]
partial class MyCoolPackage { }
";

        await Verify.VerifyCodeFixAsync(test, withFix);
    }

    [Fact]
    public async Task PackageMatchViaIntermediateClass_ProducesNoDiagnosticAsync()
    {
        var test = @"
using Microsoft.VisualStudio.Shell;

class Middle : Package { }

[PackageRegistration(UseManagedResourcesOnly = true)]
class Test : Middle {
}
";
        await Verify.VerifyAnalyzerAsync(test);
    }

    [Fact]
    public async Task AsyncPackageMatchViaIntermediateClass_ProducesNoDiagnosticAsync()
    {
        var test = @"
using Microsoft.VisualStudio.Shell;

class Middle : AsyncPackage { }

[PackageRegistration(UseManagedResourcesOnly = true, AllowsBackgroundLoading = true)]
class Test : Middle {
}
";
        await Verify.VerifyAnalyzerAsync(test);
    }
}
