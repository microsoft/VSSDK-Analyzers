// Copyright (c) Microsoft Corporation. All rights reserved.

using System;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.VisualStudio.SDK.Analyzers;
using Microsoft.VisualStudio.SDK.Analyzers.Tests;
using Xunit;
using Xunit.Abstractions;

public class VSSDK006CheckServicesExistAnalyzerTests : CodeFixVerifier
{
    private DiagnosticResult expect = new DiagnosticResult
    {
        Id = VSSDK006CheckServicesExistAnalyzer.Id,
        SkipVerifyMessage = true,
        Severity = DiagnosticSeverity.Warning,
    };

    public VSSDK006CheckServicesExistAnalyzerTests(ITestOutputHelper logger)
        : base(logger)
    {
    }

    [Fact]
    public void LocalAssigned_GetService_ThenUsed()
    {
        var test = @"
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;

class Test : Package {
    protected override void Initialize() {
        base.Initialize();
        var svc = this.GetService(typeof(SVsBuildManagerAccessor)) as Microsoft.VisualStudio.Shell.Interop.IVsBuildManagerAccessor;
        svc.BeginDesignTimeBuild();
    }
}
";

        var fix = @"
using Microsoft;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;

class Test : Package {
    protected override void Initialize() {
        base.Initialize();
        var svc = this.GetService(typeof(SVsBuildManagerAccessor)) as Microsoft.VisualStudio.Shell.Interop.IVsBuildManagerAccessor;
        Assumes.Present(svc);
        svc.BeginDesignTimeBuild();
    }
}
";

        this.VerifyCSharpDiagnostic(test, this.CreateDiagnostic(8, 13, 3, (9, 9, 3)));
        this.VerifyCSharpFix(test, fix);
    }

    [Fact]
    public void LocalAssigned_GetService_ThenUsed_WithNullConditional()
    {
        var test = @"
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;

class Test : Package {
    protected override void Initialize() {
        base.Initialize();
        var svc = this.GetService(typeof(SVsBuildManagerAccessor)) as IVsBuildManagerAccessor;
        svc?.BeginDesignTimeBuild();
    }
}
";

        this.VerifyCSharpDiagnostic(test);
    }

    [Fact]
    public void LocalAssigned_IServiceProvider_GetService_ThenUsed()
    {
        var test = @"
using System;
using Microsoft.VisualStudio.Shell.Interop;

class Test {
    void Initialize(IServiceProvider sp) {
        var svc = sp.GetService(typeof(SVsBuildManagerAccessor)) as IVsBuildManagerAccessor;
        svc.BeginDesignTimeBuild();
    }
}
";

        var fix = @"
using System;
using Microsoft;
using Microsoft.VisualStudio.Shell.Interop;

class Test {
    void Initialize(IServiceProvider sp) {
        var svc = sp.GetService(typeof(SVsBuildManagerAccessor)) as IVsBuildManagerAccessor;
        Assumes.Present(svc);
        svc.BeginDesignTimeBuild();
    }
}
";

        this.VerifyCSharpDiagnostic(test, this.CreateDiagnostic(7, 13, 3, (8, 9, 3)));
        this.VerifyCSharpFix(test, fix);
    }

    [Fact]
    public void LocalDeclarationAssignedWithDirectCast_GetService_ThenUsed()
    {
        var test = @"
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;

class Test : Package {
    protected override void Initialize() {
        base.Initialize();
        var svc = (IVsBuildManagerAccessor)this.GetService(typeof(SVsBuildManagerAccessor));
        svc.BeginDesignTimeBuild();
    }
}
";

        var fix = @"
using Microsoft;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;

class Test : Package {
    protected override void Initialize() {
        base.Initialize();
        var svc = (IVsBuildManagerAccessor)this.GetService(typeof(SVsBuildManagerAccessor));
        Assumes.Present(svc);
        svc.BeginDesignTimeBuild();
    }
}
";

        this.VerifyCSharpDiagnostic(test, this.CreateDiagnostic(8, 13, 3, (9, 9, 3)));
        this.VerifyCSharpFix(test, fix);
    }

    [Fact]
    public void LocalDeclarationAssignedWithAsCast_GetServiceAsync_ThenUsed()
    {
        var test = @"
using System;
using System.Threading;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using Task = System.Threading.Tasks.Task;

class Test : AsyncPackage {
    protected override async Task InitializeAsync(CancellationToken cancellationToken, IProgress<ServiceProgressData> progress) {
        await base.InitializeAsync(cancellationToken, progress);
        var svc = await this.GetServiceAsync(typeof(SVsBuildManagerAccessor)) as IVsBuildManagerAccessor;
        svc.BeginDesignTimeBuild();
    }
}
";

        var fix = @"
using System;
using System.Threading;
using Microsoft;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using Task = System.Threading.Tasks.Task;

class Test : AsyncPackage {
    protected override async Task InitializeAsync(CancellationToken cancellationToken, IProgress<ServiceProgressData> progress) {
        await base.InitializeAsync(cancellationToken, progress);
        var svc = await this.GetServiceAsync(typeof(SVsBuildManagerAccessor)) as IVsBuildManagerAccessor;
        Assumes.Present(svc);
        svc.BeginDesignTimeBuild();
    }
}
";

        this.VerifyCSharpDiagnostic(test, this.CreateDiagnostic(11, 13, 3, (12, 9, 3)));
        this.VerifyCSharpFix(test, fix);
    }

    [Fact]
    public void LocalDeclarationAssignedWithAsCast_GetService_InAsyncPackage_ThenUsed()
    {
        var test = @"
using System;
using System.Threading;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using Task = System.Threading.Tasks.Task;

class Test : AsyncPackage {
    protected override async Task InitializeAsync(CancellationToken cancellationToken, IProgress<ServiceProgressData> progress) {
        await base.InitializeAsync(cancellationToken, progress);
        var svc = this.GetService(typeof(SVsBuildManagerAccessor)) as IVsBuildManagerAccessor;
        svc.BeginDesignTimeBuild();
    }
}
";

        var fix = @"
using System;
using System.Threading;
using Microsoft;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using Task = System.Threading.Tasks.Task;

class Test : AsyncPackage {
    protected override async Task InitializeAsync(CancellationToken cancellationToken, IProgress<ServiceProgressData> progress) {
        await base.InitializeAsync(cancellationToken, progress);
        var svc = this.GetService(typeof(SVsBuildManagerAccessor)) as IVsBuildManagerAccessor;
        Assumes.Present(svc);
        svc.BeginDesignTimeBuild();
    }
}
";

        this.VerifyCSharpDiagnostic(test, this.CreateDiagnostic(11, 13, 3, (12, 9, 3)));
        this.VerifyCSharpFix(test, fix);
    }

    [Fact]
    public void LocalDeclarationAssignedWithAsCast_GetService_InAsyncPackage_ThenUsed_Twice()
    {
        var test = @"
using System;
using System.Threading;
using Microsoft;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using Task = System.Threading.Tasks.Task;

class Test : AsyncPackage {
    protected override async Task InitializeAsync(CancellationToken cancellationToken, IProgress<ServiceProgressData> progress) {
        await base.InitializeAsync(cancellationToken, progress);

        var svc = this.GetService(typeof(SVsBuildManagerAccessor)) as IVsBuildManagerAccessor;
        Assumes.Present(svc);
        svc.BeginDesignTimeBuild();

        var svc2 = this.GetService(typeof(SVsBuildManagerAccessor)) as IVsBuildManagerAccessor;
        svc2.BeginDesignTimeBuild();
    }
}
";

        var fix = @"
using System;
using System.Threading;
using Microsoft;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using Task = System.Threading.Tasks.Task;

class Test : AsyncPackage {
    protected override async Task InitializeAsync(CancellationToken cancellationToken, IProgress<ServiceProgressData> progress) {
        await base.InitializeAsync(cancellationToken, progress);

        var svc = this.GetService(typeof(SVsBuildManagerAccessor)) as IVsBuildManagerAccessor;
        Assumes.Present(svc);
        svc.BeginDesignTimeBuild();

        var svc2 = this.GetService(typeof(SVsBuildManagerAccessor)) as IVsBuildManagerAccessor;
        Assumes.Present(svc2);
        svc2.BeginDesignTimeBuild();
    }
}
";

        this.VerifyCSharpDiagnostic(test, this.CreateDiagnostic(17, 13, 4, (18, 9, 4)));
        this.VerifyCSharpFix(test, fix);
    }

    [Fact]
    public void LocalAssignedWithAsCast_GetServiceAsync_ThenUsed()
    {
        var test = @"
using System;
using System.Threading;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using Task = System.Threading.Tasks.Task;

class Test : AsyncPackage {
    protected override async Task InitializeAsync(CancellationToken cancellationToken, IProgress<ServiceProgressData> progress) {
        await base.InitializeAsync(cancellationToken, progress);
        IVsBuildManagerAccessor svc;
        svc = await this.GetServiceAsync(typeof(SVsBuildManagerAccessor)) as IVsBuildManagerAccessor;
        svc.BeginDesignTimeBuild();
    }
}
";

        var fix = @"
using System;
using System.Threading;
using Microsoft;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using Task = System.Threading.Tasks.Task;

class Test : AsyncPackage {
    protected override async Task InitializeAsync(CancellationToken cancellationToken, IProgress<ServiceProgressData> progress) {
        await base.InitializeAsync(cancellationToken, progress);
        IVsBuildManagerAccessor svc;
        svc = await this.GetServiceAsync(typeof(SVsBuildManagerAccessor)) as IVsBuildManagerAccessor;
        Assumes.Present(svc);
        svc.BeginDesignTimeBuild();
    }
}
";

        this.VerifyCSharpDiagnostic(test, this.CreateDiagnostic(12, 9, 3, (13, 9, 3)));
        this.VerifyCSharpFix(test, fix);
    }

    [Fact]
    public void LocalAssignedWithDirectCast_GetService_ThenUsed()
    {
        var test = @"
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;

class Test : Package {
    protected override void Initialize() {
        base.Initialize();
        IVsBuildManagerAccessor svc;
        svc = (IVsBuildManagerAccessor)this.GetService(typeof(SVsBuildManagerAccessor));
        svc.BeginDesignTimeBuild();
    }
}
";

        var fix = @"
using Microsoft;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;

class Test : Package {
    protected override void Initialize() {
        base.Initialize();
        IVsBuildManagerAccessor svc;
        svc = (IVsBuildManagerAccessor)this.GetService(typeof(SVsBuildManagerAccessor));
        Assumes.Present(svc);
        svc.BeginDesignTimeBuild();
    }
}
";

        this.VerifyCSharpDiagnostic(test, this.CreateDiagnostic(9, 9, 3, (10, 9, 3)));
        this.VerifyCSharpFix(test, fix);
    }

    [Fact]
    public void FieldAssigned_GetService_ThenUsed()
    {
        var test = @"
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;

class Test : Package {
    IVsBuildManagerAccessor svc;
    protected override void Initialize() {
        base.Initialize();
        this.svc = this.GetService(typeof(SVsBuildManagerAccessor)) as IVsBuildManagerAccessor;
        this.svc.BeginDesignTimeBuild();
    }
}
";

        var fix = @"
using Microsoft;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;

class Test : Package {
    IVsBuildManagerAccessor svc;
    protected override void Initialize() {
        base.Initialize();
        this.svc = this.GetService(typeof(SVsBuildManagerAccessor)) as IVsBuildManagerAccessor;
        Assumes.Present(this.svc);
        this.svc.BeginDesignTimeBuild();
    }
}
";

        this.VerifyCSharpDiagnostic(test, this.CreateDiagnostic(9, 9, 8, (10, 9, 8)));
        this.VerifyCSharpFix(test, fix);
    }

    [Fact]
    public void FieldAssigned_GetService_ThenUsedElsewhere()
    {
        var test = @"
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;

class Test : Package {
    IVsBuildManagerAccessor svc;
    protected override void Initialize() {
        base.Initialize();
        this.svc = this.GetService(typeof(SVsBuildManagerAccessor)) as IVsBuildManagerAccessor;
    }

    void Foo() {
        this.svc.BeginDesignTimeBuild();
    }
}
";

        var fix = @"
using Microsoft;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;

class Test : Package {
    IVsBuildManagerAccessor svc;
    protected override void Initialize() {
        base.Initialize();
        this.svc = this.GetService(typeof(SVsBuildManagerAccessor)) as IVsBuildManagerAccessor;
        Assumes.Present(this.svc);
    }

    void Foo() {
        this.svc.BeginDesignTimeBuild();
    }
}
";

        this.VerifyCSharpDiagnostic(test, this.CreateDiagnostic(9, 9, 8, (13, 9, 8)));
        this.VerifyCSharpFix(test, fix);
    }

    [Fact]
    public void FieldAssigned_GetServiceAsync_ThenUsedElsewhere()
    {
        var test = @"
using System;
using System.Threading;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using Task = System.Threading.Tasks.Task;

class Test : AsyncPackage {
    IVsBuildManagerAccessor svc;
    protected override async Task InitializeAsync(CancellationToken cancellationToken, IProgress<ServiceProgressData> progress) {
        await base.InitializeAsync(cancellationToken, progress);
        this.svc = await this.GetServiceAsync(typeof(SVsBuildManagerAccessor)) as IVsBuildManagerAccessor;
    }

    void Foo() {
        this.svc.BeginDesignTimeBuild();
    }
}
";

        var fix = @"
using System;
using System.Threading;
using Microsoft;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using Task = System.Threading.Tasks.Task;

class Test : AsyncPackage {
    IVsBuildManagerAccessor svc;
    protected override async Task InitializeAsync(CancellationToken cancellationToken, IProgress<ServiceProgressData> progress) {
        await base.InitializeAsync(cancellationToken, progress);
        this.svc = await this.GetServiceAsync(typeof(SVsBuildManagerAccessor)) as IVsBuildManagerAccessor;
        Assumes.Present(this.svc);
    }

    void Foo() {
        this.svc.BeginDesignTimeBuild();
    }
}
";

        this.VerifyCSharpDiagnostic(test, this.CreateDiagnostic(12, 9, 8, (16, 9, 8)));
        this.VerifyCSharpFix(test, fix);
        this.VerifyCSharpDiagnostic(fix);
    }

    [Fact]
    public void FieldAssigned_GetService_ThenUsedElsewhereWithIfCheck()
    {
        var test = @"
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;

class Test : Package {
    IVsBuildManagerAccessor svc;
    protected override void Initialize() {
        base.Initialize();
        this.svc = this.GetService(typeof(SVsBuildManagerAccessor)) as IVsBuildManagerAccessor;
    }

    void Foo() {
        if (svc != null) {
            svc.BeginDesignTimeBuild();
        }
    }
}
";

        this.VerifyCSharpDiagnostic(test);
    }

    [Fact]
    public void PropertyAssigned_GetService_ThenUsedWithinClass()
    {
        var test = @"
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;

class Test : Package {
    IVsBuildManagerAccessor svc { get; set; }
    protected override void Initialize() {
        base.Initialize();
        this.svc = this.GetService(typeof(SVsBuildManagerAccessor)) as IVsBuildManagerAccessor;
    }

    void Foo() {
        this.svc.BeginDesignTimeBuild();
    }
}
";

        var fix = @"
using Microsoft;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;

class Test : Package {
    IVsBuildManagerAccessor svc { get; set; }
    protected override void Initialize() {
        base.Initialize();
        this.svc = this.GetService(typeof(SVsBuildManagerAccessor)) as IVsBuildManagerAccessor;
        Assumes.Present(this.svc);
    }

    void Foo() {
        this.svc.BeginDesignTimeBuild();
    }
}
";

        this.VerifyCSharpDiagnostic(test, this.CreateDiagnostic(9, 9, 8, (13, 9, 8)));
        this.VerifyCSharpFix(test, fix);
    }

    [Fact]
    public void GetService_DirectlyUsed()
    {
        var test = @"
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;

class Test : Package {
    protected override void Initialize() {
        base.Initialize();
        ((IVsBuildManagerAccessor)this.GetService(typeof(SVsBuildManagerAccessor))).BeginDesignTimeBuild();
    }
}
";

        this.VerifyCSharpDiagnostic(test, this.CreateDiagnostic(8, 35, 15));
        this.VerifyNoCSharpFixOffered(test);
    }

    [Fact]
    public void GetService_DirectlyUsed_WithConditionalMemberAccess()
    {
        var test = @"
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;

class Test : Package {
    protected override void Initialize() {
        base.Initialize();
        ((IVsBuildManagerAccessor)this.GetService(typeof(SVsBuildManagerAccessor)))?.BeginDesignTimeBuild();
    }
}
";

        this.VerifyCSharpDiagnostic(test);
    }

    [Fact]
    public void GetServiceAsync_DirectlyUsed()
    {
        var test = @"
using System;
using System.Threading;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using Task = System.Threading.Tasks.Task;

class Test : AsyncPackage {
    protected override async Task InitializeAsync(CancellationToken cancellationToken, IProgress<ServiceProgressData> progress) {
        await base.InitializeAsync(cancellationToken, progress);
        ((IVsBuildManagerAccessor)await this.GetServiceAsync(typeof(SVsBuildManagerAccessor))).BeginDesignTimeBuild();
    }
}
";

        this.VerifyCSharpDiagnostic(test, this.CreateDiagnostic(11, 41, 20));
        this.VerifyNoCSharpFixOffered(test);
    }

    [Fact]
    public void LocalAssigned_CheckedByThrow_GetService_ThenUsed()
    {
        var test = @"
using Microsoft;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;

class Test : Package {
    protected override void Initialize() {
        base.Initialize();
        var svc = this.GetService(typeof(SVsBuildManagerAccessor)) as IVsBuildManagerAccessor;
        Assumes.Present(svc);
        svc.BeginDesignTimeBuild();
    }
}
";
        this.VerifyCSharpDiagnostic(test);
    }

    [Fact]
    public void LocalAssigned_CheckedByIf_GetService_ThenUsed()
    {
        var test = @"
using Microsoft;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;

class Test : Package {
    protected override void Initialize() {
        base.Initialize();
        var svc = this.GetService(typeof(SVsBuildManagerAccessor)) as IVsBuildManagerAccessor;
        if (svc != null) {
            svc.BeginDesignTimeBuild();
        }
    }
}
";
        this.VerifyCSharpDiagnostic(test);
    }

    [Fact]
    public void FieldAssigned_Checked_GetService_ThenUsed()
    {
        var test = @"
using Microsoft;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;

class Test : Package {
    IVsBuildManagerAccessor svc;
    protected override void Initialize() {
        base.Initialize();
        this.svc = this.GetService(typeof(SVsBuildManagerAccessor)) as IVsBuildManagerAccessor;
        Assumes.Present(svc);
        svc.BeginDesignTimeBuild();
    }
}
";
        this.VerifyCSharpDiagnostic(test);
    }

    protected override DiagnosticAnalyzer GetCSharpDiagnosticAnalyzer() => new VSSDK006CheckServicesExistAnalyzer();

    protected override CodeFixProvider GetCSharpCodeFixProvider() => new VSSDK006CheckServicesExistCodeFix();

    private DiagnosticResult CreateDiagnostic(int line, int column, int length, params (int line, int column, int length)[] additionalLocations)
    {
        var allLocations = new DiagnosticResultLocation[additionalLocations.Length + 1];
        allLocations[0] = new DiagnosticResultLocation("Test0.cs", line, column, line, column + length);
        for (int i = 0; i < additionalLocations.Length; i++)
        {
            var addlLoc = additionalLocations[i];
            allLocations[i + 1] = new DiagnosticResultLocation("Test0.cs", addlLoc.line, addlLoc.column, addlLoc.line, addlLoc.column + addlLoc.length);
        }

        return new DiagnosticResult
        {
            Id = this.expect.Id,
            Locations = allLocations,
            Severity = this.expect.Severity,
            SkipVerifyMessage = this.expect.SkipVerifyMessage,
        };
    }
}
