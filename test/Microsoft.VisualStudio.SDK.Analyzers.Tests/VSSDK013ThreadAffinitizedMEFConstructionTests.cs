// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

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

        DiagnosticResult expected = Verify.Diagnostic().WithSpan(9, 9, 9, 73);
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

        DiagnosticResult expected = Verify.Diagnostic().WithSpan(14, 9, 14, 73);
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

        DiagnosticResult expected = Verify.Diagnostic().WithSpan(13, 9, 13, 73);
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

        DiagnosticResult expected = Verify.Diagnostic().WithSpan(13, 9, 13, 73);
        await Verify.VerifyAnalyzerAsync(test, expected);
    }

    [Fact]
    public async Task PartImportSatisfiedNotificationUnrelated_NoWarning()
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
    }

    void AnotherMethod()
    {
        Microsoft.VisualStudio.Shell.ThreadHelper.ThrowIfNotOnUIThread();
    }
}";

        await Verify.VerifyAnalyzerAsync(test);
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

        DiagnosticResult expected = Verify.Diagnostic().WithSpan(7, 16, 7, 91);
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

        DiagnosticResult expected = Verify.Diagnostic().WithSpan(7, 25, 7, 100);
        await Verify.VerifyAnalyzerAsync(test, expected);
    }

    [Fact]
    public async Task LazyPropertyInitializer_MainThreadAsserted_NoWarning()
    {
        var test = /* lang=c#-test */ @"
using System.ComponentModel.Composition;
using Microsoft.VisualStudio.Threading;

[Export]
class C
{
    private readonly AsyncLazy<object> _o;

    [ImportingConstructor]
    public C(JoinableTaskContext joinableTaskContext)
    {
        _o = new(async() =>
        {
            await joinableTaskContext.Factory.SwitchToMainThreadAsync(System.Threading.CancellationToken.None);
            return Microsoft.VisualStudio.Shell.UIContext.FromUIContextGuid(System.Guid.Empty);
        });
    }
}";

        await Verify.VerifyAnalyzerAsync(test);
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

    [Fact]
    public async Task NoExport_UnrelatedMethod_NoWarning()
    {
        var test = /* lang=c#-test */ @"
using System.ComponentModel.Composition;

class C
{
    public void UnrelatedMethod()
    {
        Microsoft.VisualStudio.Shell.ThreadHelper.ThrowIfNotOnUIThread();
    }
}";

        await Verify.VerifyAnalyzerAsync(test);
    }

    [Fact]
    public async Task MultipleConstructors_ParameterlessImportingConstructor_Flagged()
    {
        var test = /* lang=c#-test */ @"
using System.ComponentModel.Composition;

[Export]
class C
{
    [ImportingConstructor]
    public C()
    {
        Microsoft.VisualStudio.Shell.ThreadHelper.ThrowIfNotOnUIThread();
    }
}";

        DiagnosticResult expected = Verify.Diagnostic().WithSpan(10, 9, 10, 73);
        await Verify.VerifyAnalyzerAsync(test, expected);
    }

    [Fact]
    public async Task MultipleConstructors_OnlyImportingConstructor_Flagged()
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

    [ImportingConstructor]
    public C(bool b)
    {
        Microsoft.VisualStudio.Shell.ThreadHelper.ThrowIfNotOnUIThread();
    }
}";

        DiagnosticResult expected = Verify.Diagnostic().WithSpan(15, 9, 15, 73);
        await Verify.VerifyAnalyzerAsync(test, expected);
    }

    [Fact]
    public async Task FieldInitializer_Benign_NoWarning()
    {
        var test = /* lang=c#-test */ @"
using System.ComponentModel.Composition;

[Export]
class C
{
    object o = new object();

    public C()
    {
    }
}";

        await Verify.VerifyAnalyzerAsync(test);
    }

    [Fact]
    public async Task ExportWithoutConstructors_NoWarning()
    {
        var test = /* lang=c#-test */ @"
using System.ComponentModel.Composition;

[Export]
class C
{
}";

        await Verify.VerifyAnalyzerAsync(test);
    }

    [Fact]
    public async Task Constructor_Benign_NoWarning()
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
    public async Task OnImportsSatisfied_Benign_NoWarning()
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
        var o = new object();
    }
}";

        await Verify.VerifyAnalyzerAsync(test);
    }

    [Fact]
    public async Task OperationsWithoutType_Benign_NoWarning()
    {
        var test = /* lang=c#-test */ @"
using System.Runtime.InteropServices;
[assembly: DefaultDllImportSearchPaths(DllImportSearchPath.SafeDirectories)]";

        await Verify.VerifyAnalyzerAsync(test);
    }

    [Fact]
    public async Task InstanceReference_Benign_NoWarning()
    {
        var test = /* lang=c#-test */ @"
using System.ComponentModel.Composition;

[Export]
internal class C
{
    bool? MyProperty = null;
    public C()
    {
        MyProperty = false;
    }
}
";

        await Verify.VerifyAnalyzerAsync(test);
    }

    // TODO: it's not reproing the exception
    [Fact]
    public async Task ObjectOrCollectionInitializer_Benign_NoWarning()
    {
        var test = /* lang=c#-test */ @"
using System.ComponentModel.Composition;
using Microsoft.VisualStudio.Utilities;

[Export]
[Name(PartName)]
[Order(Before = ""default"")]
internal class C
{
    public const string PartName = ""PartName"";
    public C()
    {
    }
}
";

        await Verify.VerifyAnalyzerAsync(test);
    }

    [Fact]
    public async Task Constructor_MainThreadRequired_LocalFunction_Flagged()
    {
        var test = /* lang=c#-test */ @"
using System.ComponentModel.Composition;

[Export]
class C
{
    public C()
    {
        LocalFunction();

        void LocalFunction()
        {
            Microsoft.VisualStudio.Shell.ThreadHelper.ThrowIfNotOnUIThread();
        }
    }
}";

        DiagnosticResult expected = Verify.Diagnostic().WithSpan(13, 13, 13, 77);
        await Verify.VerifyAnalyzerAsync(test, expected);
    }

    [Fact]
    public async Task Constructor_MainThreadRequired_LocalFunction_FireAndForget_Flagged_FalsePositive()
    {
        var test = /* lang=c#-test */ @"
using Microsoft.VisualStudio.Threading;
using System.ComponentModel.Composition;
using System.Threading.Tasks;

[Export]
class C
{
    [ImportingConstructor]
    public C(JoinableTaskContext joinableTaskContext)
    {
        LocalFunctionAsync().Forget();

        async Task LocalFunctionAsync()
        {
            await joinableTaskContext.Factory.SwitchToMainThreadAsync();
            return;
        }
    }
}";

        DiagnosticResult expected = Verify.Diagnostic().WithSpan(16, 19, 16, 72);
        await Verify.VerifyAnalyzerAsync(test, expected);
    }
}
