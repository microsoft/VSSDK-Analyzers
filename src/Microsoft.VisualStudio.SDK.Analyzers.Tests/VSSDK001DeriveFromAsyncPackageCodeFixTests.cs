// Copyright (c) Microsoft Corporation. All rights reserved.

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.VisualStudio.SDK.Analyzers;
using Microsoft.VisualStudio.SDK.Analyzers.Tests;
using Xunit;
using Xunit.Abstractions;

public class VSSDK001DeriveFromAsyncPackageCodeFixTests : CodeFixVerifier
{
    public VSSDK001DeriveFromAsyncPackageCodeFixTests(ITestOutputHelper logger)
        : base(logger)
    {
    }

    [Fact]
    public void BaseTypeChangesToAsyncPackage()
    {
        var test = @"
using Microsoft.VisualStudio.Shell;

class Test : Package
{
}
";
        var withFix = @"
using Microsoft.VisualStudio.Shell;

class Test : AsyncPackage
{
}
";
        this.VerifyCSharpFix(test, withFix);
    }

    [Fact]
    public void BaseTypeChangesToAsyncPackage_NoUsings()
    {
        var test = @"
class Test : Microsoft.VisualStudio.Shell.Package
{
}
";
        var withFix = @"
class Test : Microsoft.VisualStudio.Shell.AsyncPackage
{
}
";
        this.VerifyCSharpFix(test, withFix);
    }

    [Fact]
    public void BaseTypeChangesToAsyncPackage_InPartiallyMatchingNamespace()
    {
        var test = @"
namespace Microsoft.VisualStudio
{
    class Test : Microsoft.VisualStudio.Shell.Package
    {
    }
}
";
        var withFix = @"
namespace Microsoft.VisualStudio
{
    class Test : Shell.AsyncPackage
    {
    }
}
";
        this.VerifyCSharpFix(test, withFix);
    }

    protected override CodeFixProvider GetCSharpCodeFixProvider() => new VSSDK001DeriveFromAsyncPackageCodeFix();

    protected override DiagnosticAnalyzer GetCSharpDiagnosticAnalyzer() => new VSSDK001DeriveFromAsyncPackageAnalyzer();
}
