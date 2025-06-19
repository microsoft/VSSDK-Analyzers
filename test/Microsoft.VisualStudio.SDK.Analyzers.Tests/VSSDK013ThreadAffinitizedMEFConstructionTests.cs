// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

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
        [|Microsoft.VisualStudio.Shell.ThreadHelper.ThrowIfNotOnUIThread()|];
    }
}";

        await Verify.VerifyAnalyzerAsync(test);
    }

    [Fact]
    public async Task DerivedExport_Constructor_MainThreadRequired_Flagged()
    {
        var test = /* lang=c#-test */ @"
using System.ComponentModel.Composition;

class MyExportAttribute : ExportAttribute {}

[MyExport]
class C
{
    public C()
    {
        [|Microsoft.VisualStudio.Shell.ThreadHelper.ThrowIfNotOnUIThread()|];
    }
}";

        await Verify.VerifyAnalyzerAsync(test);
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
        [|Microsoft.VisualStudio.Shell.ThreadHelper.ThrowIfNotOnUIThread()|];
    }
}";

        await Verify.VerifyAnalyzerAsync(test);
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
        [|Microsoft.VisualStudio.Shell.ThreadHelper.ThrowIfNotOnUIThread()|];
    }
}";

        await Verify.VerifyAnalyzerAsync(test);
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
        [|Microsoft.VisualStudio.Shell.ThreadHelper.ThrowIfNotOnUIThread()|];
    }
}";

        await Verify.VerifyAnalyzerAsync(test);
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
    object o = [|Microsoft.VisualStudio.Shell.UIContext.FromUIContextGuid(System.Guid.Empty)|];

    public C()
    {
    }
}";

        await Verify.VerifyAnalyzerAsync(test);
    }

    [Fact]
    public async Task PropertyInitializer_MainThreadAsserted_Flagged()
    {
        var test = /* lang=c#-test */ @"
using System.ComponentModel.Composition;

[Export]
class C
{
    object o { get; } = [|Microsoft.VisualStudio.Shell.UIContext.FromUIContextGuid(System.Guid.Empty)|];

    public C()
    {
    }
}";

        await Verify.VerifyAnalyzerAsync(test);
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
        [|Microsoft.VisualStudio.Shell.ThreadHelper.ThrowIfNotOnUIThread()|];
    }
}";

        await Verify.VerifyAnalyzerAsync(test);
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
        [|Microsoft.VisualStudio.Shell.ThreadHelper.ThrowIfNotOnUIThread()|];
    }
}";

        await Verify.VerifyAnalyzerAsync(test);
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
            [|Microsoft.VisualStudio.Shell.ThreadHelper.ThrowIfNotOnUIThread()|];
        }
    }
}";

        await Verify.VerifyAnalyzerAsync(test);
    }

    /// <summary>
    /// This test case showcases a false positive, where a file-and-forget function is flagged.
    /// It's OK to access UI thread within the local function which executes asynchronously and
    /// MEF part construction does not join on this async work.
    /// </summary>
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
            await [|joinableTaskContext.Factory.SwitchToMainThreadAsync()|];
            return;
        }
    }
}";

        await Verify.VerifyAnalyzerAsync(test);
    }

    /// <summary>
    /// This test showcases a false negative, where VSSDK013 does not report
    /// UI thread dependency inside the async lambda. To reduce number of false positives,
    /// we assume that lambdas are executed asynchronously.
    /// In this example, execution joins the lambda, effectively blocking UI thread.
    /// </summary>
    [Fact]
    public async Task JoiningAsyncLambda_MainThreadAsserted_NoWarning_FalseNegative()
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
        var s = joinableTaskContext.Factory.Run(async () =>
        {
            await joinableTaskContext.Factory.SwitchToMainThreadAsync(System.Threading.CancellationToken.None);
            return ""test"";
        });
    }
}";

        await Verify.VerifyAnalyzerAsync(test);
    }
}
