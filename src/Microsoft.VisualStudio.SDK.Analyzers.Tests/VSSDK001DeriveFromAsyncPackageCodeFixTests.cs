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
    public void PackageRegistrationUpdated_NewArgument()
    {
        var test = @"
using Microsoft.VisualStudio.Shell;

[PackageRegistration(UseManagedResourcesOnly = true)]
class Test : Package
{
}
";
        var withFix = @"
using Microsoft.VisualStudio.Shell;

[PackageRegistration(UseManagedResourcesOnly = true, AllowsBackgroundLoading = true)]
class Test : AsyncPackage
{
}
";
        this.VerifyCSharpFix(test, withFix);
    }

    [Fact]
    public void PackageRegistrationUpdated_ExistingArgument()
    {
        var test = @"
using Microsoft.VisualStudio.Shell;

[PackageRegistration(UseManagedResourcesOnly = true, AllowsBackgroundLoading = false)]
class Test : Package
{
}
";
        var withFix = @"
using Microsoft.VisualStudio.Shell;

[PackageRegistration(UseManagedResourcesOnly = true, AllowsBackgroundLoading = true)]
class Test : AsyncPackage
{
}
";
        this.VerifyCSharpFix(test, withFix);
    }

    [Fact]
    public void BaseTypeChangesToAsyncPackage_WithInterfaces()
    {
        var test = @"
using System;
using Microsoft.VisualStudio.Shell;

class Test : Package, IDisposable
{
    public void Dispose()
    {
    }
}
";
        var withFix = @"
using System;
using Microsoft.VisualStudio.Shell;

class Test : AsyncPackage, IDisposable
{
    public void Dispose()
    {
    }
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
        var withFix = @"using Microsoft.VisualStudio.Shell;

class Test : AsyncPackage
{
}
";
        this.VerifyCSharpFix(test, withFix);
    }

    [Fact]
    public void BaseTypeChangesToAsyncPackage_NoUsings_AndInitializeMethod()
    {
        var test = @"
class Test : Microsoft.VisualStudio.Shell.Package
{
    protected override void Initialize()
    {
        base.Initialize(); // base invocation
    }
}
";
        var withFix = @"using System;
using System.Threading;
using Microsoft.VisualStudio.Shell;
using Task = System.Threading.Tasks.Task;

class Test : AsyncPackage
{
    protected override async Task InitializeAsync(CancellationToken cancellationToken, IProgress<ServiceProgressData> progress)
    {
        await base.InitializeAsync(cancellationToken, progress); // base invocation

        // When initialized asynchronously, we *may* be on a background thread at this point.
        // Do any initialization that requires the UI thread after switching to the UI thread.
        // Otherwise, remove the switch to the UI thread if you don't need it.
        await this.JoinableTaskFactory.SwitchToMainThreadAsync(cancellationToken);
    }
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
        var withFix = @"using Microsoft.VisualStudio.Shell;

namespace Microsoft.VisualStudio
{
    class Test : AsyncPackage
    {
    }
}
";
        this.VerifyCSharpFix(test, withFix);
    }

    [Fact]
    public void BaseTypeChangesToAsyncPackage_InPartiallyMatchingNamespace_UsingsInsideNamespace()
    {
        var test = @"
namespace Microsoft.VisualStudio
{
    using System;

    class Test : Microsoft.VisualStudio.Shell.Package
    {
        public String Member { get; set; }
    }
}
";
        var withFix = @"using Microsoft.VisualStudio.Shell;

namespace Microsoft.VisualStudio
{
    using System;

    class Test : AsyncPackage
    {
        public String Member { get; set; }
    }
}
";
        this.VerifyCSharpFix(test, withFix);
    }

    [Fact]
#pragma warning disable VSTHRD200 // Use "Async" suffix for async methods
    public void InitializeOverride_BecomesAsync()
#pragma warning restore VSTHRD200 // Use "Async" suffix for async methods
    {
        var test = @"
namespace NS
{
    using System;

    class Test : Microsoft.VisualStudio.Shell.Package
    {
        protected override void Initialize()
        {
            Console.WriteLine(""before"");

            base.Initialize(); // base invocation

            Console.WriteLine(""after"");
        }
    }
}
";
        var withFix = @"using System.Threading;
using Microsoft.VisualStudio.Shell;
using Task = System.Threading.Tasks.Task;

namespace NS
{
    using System;

    class Test : AsyncPackage
    {
        protected override async Task InitializeAsync(CancellationToken cancellationToken, IProgress<ServiceProgressData> progress)
        {
            Console.WriteLine(""before"");

            await base.InitializeAsync(cancellationToken, progress); // base invocation

            // When initialized asynchronously, we *may* be on a background thread at this point.
            // Do any initialization that requires the UI thread after switching to the UI thread.
            // Otherwise, remove the switch to the UI thread if you don't need it.
            await this.JoinableTaskFactory.SwitchToMainThreadAsync(cancellationToken);

            Console.WriteLine(""after"");
        }
    }
}
";
        this.VerifyCSharpFix(test, withFix);
    }

    [Fact]
#pragma warning disable VSTHRD200 // Use "Async" suffix for async methods
    public void InitializeOverride_AlreadyDefinesTaskUsing()
#pragma warning restore VSTHRD200 // Use "Async" suffix for async methods
    {
        var test = @"
using System;
using Task = System.Threading.Tasks.Task;

class Test : Microsoft.VisualStudio.Shell.Package
{
    protected override void Initialize()
    {
        base.Initialize();
    }
}
";
        var withFix = @"
using System;
using System.Threading;
using Microsoft.VisualStudio.Shell;
using Task = System.Threading.Tasks.Task;

class Test : AsyncPackage
{
    protected override async Task InitializeAsync(CancellationToken cancellationToken, IProgress<ServiceProgressData> progress)
    {
        await base.InitializeAsync(cancellationToken, progress);

        // When initialized asynchronously, we *may* be on a background thread at this point.
        // Do any initialization that requires the UI thread after switching to the UI thread.
        // Otherwise, remove the switch to the UI thread if you don't need it.
        await this.JoinableTaskFactory.SwitchToMainThreadAsync(cancellationToken);
    }
}
";
        this.VerifyCSharpFix(test, withFix);
    }

    [Fact]
    public void InitializeOverride_MissingBaseInitializeCall()
    {
        var test = @"
using System;

class Test : Microsoft.VisualStudio.Shell.Package
{
    protected override void Initialize()
    {
        Console.WriteLine();
    }
}
";
        var withFix = @"
using System;
using System.Threading;
using Microsoft.VisualStudio.Shell;
using Task = System.Threading.Tasks.Task;

class Test : AsyncPackage
{
    protected override async Task InitializeAsync(CancellationToken cancellationToken, IProgress<ServiceProgressData> progress)
    {
        // When initialized asynchronously, we *may* be on a background thread at this point.
        // Do any initialization that requires the UI thread after switching to the UI thread.
        // Otherwise, remove the switch to the UI thread if you don't need it.
        await this.JoinableTaskFactory.SwitchToMainThreadAsync(cancellationToken);
        Console.WriteLine();
    }
}
";
        this.VerifyCSharpFix(test, withFix);
    }

    [Fact]
    public void InitializeOverride_GetServiceCallsUpdated()
    {
        var test = @"
using System;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;

class Test : Package
{
    protected override void Initialize()
    {
        base.Initialize(); // base invocation

        var shell = this.GetService(typeof(SVsShell)) as IVsShell;
        var shell2 = GetService(typeof(SVsShell)) as IVsShell;
        var shell3 = GetService(typeof(SVsShell)).ToString();
    }
}
";
        var withFix = @"
using System;
using System.Threading;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using Task = System.Threading.Tasks.Task;

class Test : AsyncPackage
{
    protected override async Task InitializeAsync(CancellationToken cancellationToken, IProgress<ServiceProgressData> progress)
    {
        await base.InitializeAsync(cancellationToken, progress); // base invocation

        // When initialized asynchronously, we *may* be on a background thread at this point.
        // Do any initialization that requires the UI thread after switching to the UI thread.
        // Otherwise, remove the switch to the UI thread if you don't need it.
        await this.JoinableTaskFactory.SwitchToMainThreadAsync(cancellationToken);

        var shell = await this.GetServiceAsync(typeof(SVsShell)) as IVsShell;
        var shell2 = await GetServiceAsync(typeof(SVsShell)) as IVsShell;
        var shell3 = (await GetServiceAsync(typeof(SVsShell))).ToString();
    }
}
";
        this.VerifyCSharpFix(test, withFix);
    }

    protected override CodeFixProvider GetCSharpCodeFixProvider() => new VSSDK001DeriveFromAsyncPackageCodeFix();

    protected override DiagnosticAnalyzer GetCSharpDiagnosticAnalyzer() => new VSSDK001DeriveFromAsyncPackageAnalyzer();
}
