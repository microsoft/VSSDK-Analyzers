// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Testing;
using Xunit;
using Verify = CSharpCodeFixVerifier<
    Microsoft.VisualStudio.SDK.Analyzers.VSSDK013ThreadAffinitizedMEFConstruction,
    Microsoft.CodeAnalysis.Testing.EmptyCodeFixProvider>;

// TODO: Borrow ideas from vs-threading's VSTHRD010MainThreadUsageAnalyzerTests

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
        _ = Microsoft.VisualStudio.Shell.Interop.SampleMethod();
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
            _ = Microsoft.VisualStudio.Shell.Interop.SampleMethod();
        }
    }";

        DiagnosticResult expected = Verify.Diagnostic().WithSpan(8, 17, 8, 68);
        await Verify.VerifyAnalyzerAsync(test, expected);
    }

    [Fact]
    public async Task Constructor_MainThreadAsserted_Flagged()
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

        DiagnosticResult expected = Verify.Diagnostic().WithSpan(8, 13, 8, 77);
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
            _ = Microsoft.VisualStudio.Shell.Interop.SampleMethod();
        }
    }";

        DiagnosticResult expected = Verify.Diagnostic().WithSpan(13, 17, 13, 69);
        await Verify.VerifyAnalyzerAsync(test, expected);
    }

    [Fact]
    public async Task PartImportSatisfiedNotification_MainThreadAsserted_Flagged()
    {
        var test = /* lang=c#-test */ @"
using System.ComponentModel.Composition;

[Export]
class C : IPartImportSatisfiedNotification
    {
        public C()
        {
        }

        void IPartImportSatisfiedNotification.OnImportsSatisfied()
        {
            _ = Microsoft.VisualStudio.Shell.Interop.SampleMethod();
        }
    }";

        DiagnosticResult expected = Verify.Diagnostic().WithSpan(12, 17, 12, 69);
        await Verify.VerifyAnalyzerAsync(test, expected);
    }

    [Fact]
    public async Task FieldInitializer_MainThreadAsserted_Flagged()
    {
        var test = /* lang=c#-test */ @"
using System.ComponentModel.Composition;

[Export]
class C : IPartImportSatisfiedNotification
    {
        object o = Microsoft.VisualStudio.Shell.Interop.SampleMethod();

        public C()
        {
        }
    }";

        DiagnosticResult expected = Verify.Diagnostic().WithSpan(6, 20, 6, 71);
        await Verify.VerifyAnalyzerAsync(test, expected);
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
            _ = Microsoft.VisualStudio.Shell.Interop.IVsFeatureFlags();
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
            _ = Microsoft.VisualStudio.Shell.Interop.SampleMethod();
        }
    }";

        await Verify.VerifyAnalyzerAsync(test);
    }

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
            _ = Microsoft.VisualStudio.Shell.Interop.SampleMethod();
        }
    }";

        await Verify.VerifyAnalyzerAsync(test);
    }
}
