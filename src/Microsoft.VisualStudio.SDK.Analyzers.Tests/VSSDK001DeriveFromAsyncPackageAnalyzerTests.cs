// Copyright (c) Microsoft Corporation. All rights reserved.

using System.Threading.Tasks;
using Xunit;
using Verify = CSharpCodeFixVerifier<
    Microsoft.VisualStudio.SDK.Analyzers.VSSDK001DeriveFromAsyncPackageAnalyzer,
    Microsoft.VisualStudio.SDK.Analyzers.VSSDK001DeriveFromAsyncPackageCodeFix>;

public class VSSDK001DeriveFromAsyncPackageAnalyzerTests
{
    [Fact]
    public async Task AsyncPackageDerivedClassProducesNoDiagnosticAsync()
    {
        var test = @"
using Microsoft.VisualStudio.Shell;

class Test : AsyncPackage {
}
";

        await Verify.VerifyAnalyzerAsync(test);
    }

    [Fact]
    public async Task NoBaseTypeProducesNoDiagnosticAsync()
    {
        var test = @"
class Test {
}
";

        await Verify.VerifyAnalyzerAsync(test);
    }

    [Fact]
    public async Task PackageDerivedClassProducesDiagnosticAsync()
    {
        var test = @"
using Microsoft.VisualStudio.Shell;

class Test : Package {
}
";

        var expected = Verify.Diagnostic().WithSpan(4, 14, 4, 21);
        await Verify.VerifyAnalyzerAsync(test, expected);
    }

    [Fact]
    public async Task PackageDerivedClassWithInterfacesProducesDiagnosticAsync()
    {
        var test = @"
using System;
using Microsoft.VisualStudio.Shell;

class Test : Package, IDisposable {
    public void Dispose() { }
}
";

        var expected = Verify.Diagnostic().WithSpan(5, 14, 5, 21);
        await Verify.VerifyAnalyzerAsync(test, expected);
    }
}
