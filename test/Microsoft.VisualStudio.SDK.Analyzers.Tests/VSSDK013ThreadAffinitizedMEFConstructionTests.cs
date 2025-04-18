// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Testing;
using Xunit;
using Verify = CSharpCodeFixVerifier<
    Microsoft.VisualStudio.SDK.Analyzers.VSSDK013ThreadAffinitizedMEFConstruction,
    Microsoft.CodeAnalysis.Testing.EmptyCodeFixProvider>;

public class VSSDK013ThreadAffinitizedMEFConstructionTests
{
    [Fact]
    public async Task NoExport_NoWarning()
    {
        var test = /* lang=c#-test */ @"
using Microsoft.VisualStudio.Shell;

class C
{
    public C()
    {
        Microsoft.VisualStudio.Shell.ThreadHelper.ThrowIfNotOnUIThread();
    }
}";

        await Verify.VerifyAnalyzerAsync(test);
    }

    [Fact]
    public async Task Constructor_MainThreadRequired_Flagged()
    {
        var test = /* lang=c#-test */ @"
using System.ComponentModel.Composition;

[Export]
class C
{
    public C()
    {
        Microsoft.VisualStudio.Shell.ThreadHelper.ThrowIfNotOnUIThread();
    }
}";

        DiagnosticResult expected = Verify.Diagnostic().WithSpan(9, 9, 9, 74);
        await Verify.VerifyAnalyzerAsync(test, expected);
    }

    [Fact]
    public async Task ImportingConstructor_MainThreadAsserted_Flagged()
    {
        var test = /* lang=c#-test */ @"
using System.ComponentModel.Composition;

[Export]
class C
{
    public C()
    {
    }

    [ImportingConstructor]
    public C(bool b)
    {
        Microsoft.VisualStudio.Shell.ThreadHelper.ThrowIfNotOnUIThread();
    }
}";

        DiagnosticResult expected = Verify.Diagnostic().WithSpan(14, 9, 14, 74);
        await Verify.VerifyAnalyzerAsync(test, expected);
    }

    [Fact]
    public async Task PartImportSatisfiedNotificationExplicit_MainThreadAsserted_Flagged()
    {
        var test = /* lang=c#-test */ @"
using System.ComponentModel.Composition;

[Export]
class C : IPartImportsSatisfiedNotification
{
    public C()
    {
    }

    void IPartImportsSatisfiedNotification.OnImportsSatisfied()
    {
        Microsoft.VisualStudio.Shell.ThreadHelper.ThrowIfNotOnUIThread();
    }
}";

        DiagnosticResult expected = Verify.Diagnostic().WithSpan(13, 9, 13, 74);
        await Verify.VerifyAnalyzerAsync(test, expected);
    }

    [Fact]
    public async Task PartImportSatisfiedNotificationImplicit_MainThreadAsserted_Flagged()
    {
        var test = /* lang=c#-test */ @"
using System.ComponentModel.Composition;

[Export]
class C : IPartImportsSatisfiedNotification
{
    public C()
    {
    }

    public void OnImportsSatisfied()
    {
        Microsoft.VisualStudio.Shell.ThreadHelper.ThrowIfNotOnUIThread();
    }
}";

        DiagnosticResult expected = Verify.Diagnostic().WithSpan(13, 9, 13, 74);
        await Verify.VerifyAnalyzerAsync(test, expected);
    }

    [Fact]
    public async Task FieldInitializer_MainThreadAsserted_Flagged()
    {
        var test = /* lang=c#-test */ @"
using System.ComponentModel.Composition;

[Export]
class C
{
    object o = Microsoft.VisualStudio.Shell.UIContext.FromUIContextGuid(System.Guid.Empty);

    public C()
    {
    }
}";

        DiagnosticResult expected = Verify.Diagnostic().WithSpan(7, 16, 7, 67);
        await Verify.VerifyAnalyzerAsync(test, expected);
    }


    [Fact]
    public async Task PropertyInitializer_MainThreadAsserted_Flagged()
    {
        var test = /* lang=c#-test */ @"
using System.ComponentModel.Composition;

[Export]
class C
{
    object o { get; } = Microsoft.VisualStudio.Shell.UIContext.FromUIContextGuid(System.Guid.Empty);

    public C()
    {
    }
}";

        DiagnosticResult expected = Verify.Diagnostic().WithSpan(7, 25, 7, 99);
        await Verify.VerifyAnalyzerAsync(test, expected);
    }


    [Fact]
    public async Task PropertyGetter_MainThreadAsserted_NoWarning()
    {
        var test = /* lang=c#-test */ @"
using System.ComponentModel.Composition;

[Export]
class C
{
    object o => Microsoft.VisualStudio.Shell.UIContext.FromUIContextGuid(System.Guid.Empty);

    public C()
    {
    }
}";

        await Verify.VerifyAnalyzerAsync(test);
    }

    [Fact]
    public async Task Constructor_FreeThreaded_NoWarning()
    {
        var test = /* lang=c#-test */ @"
using System.ComponentModel.Composition;

[Export]
class C
{
    public C()
    {
        _ = Microsoft.VisualStudio.Shell.ThreadHelper.JoinableTaskContext;
    }
}";

        await Verify.VerifyAnalyzerAsync(test);
    }

    [Fact]
    public async Task Member_MainThreadRequired_NoWarning()
    {
        var test = /* lang=c#-test */ @"
using System.ComponentModel.Composition;

[Export]
class C
{
    public C()
    {
    }

    public void X()
    {
        Microsoft.VisualStudio.Shell.ThreadHelper.ThrowIfNotOnUIThread();
    }
}";

        await Verify.VerifyAnalyzerAsync(test);
    }

    // TODO: This test would require flow analysis
    [Fact]
    public async Task Constructor_IndirectMainThreadRequired_NoWarning()
    {
        var test = /* lang=c#-test */ @"
using System.ComponentModel.Composition;

[Export]
class C
{
    public C()
    {
        X();
    }

    public void X()
    {
        Microsoft.VisualStudio.Shell.ThreadHelper.ThrowIfNotOnUIThread();
    }
}";

        await Verify.VerifyAnalyzerAsync(test);
    }
}
