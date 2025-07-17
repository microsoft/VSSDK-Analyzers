﻿// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Xunit;
using Verify = CSharpCodeFixVerifier<
    Microsoft.VisualStudio.SDK.Analyzers.VSSDK001DeriveFromAsyncPackageAnalyzer,
    Microsoft.VisualStudio.SDK.Analyzers.VSSDK001DeriveFromAsyncPackageCodeFix>;

public class VSSDK001DeriveFromAsyncPackageCodeFixTests
{
    [Fact]
    public async Task BaseTypeChangesToAsyncPackageAsync()
    {
        var test = /* lang=c#-test */ @"
using Microsoft.VisualStudio.Shell;

class Test : [|Package|]
{
}
";
        var withFix = /* lang=c#-test */ @"
using Microsoft.VisualStudio.Shell;

class Test : AsyncPackage
{
}
";
        await Verify.VerifyCodeFixAsync(test, withFix);
    }

    [Fact]
    public async Task PackageRegistrationUpdated_NewArgumentAsync()
    {
        var test = /* lang=c#-test */ @"
using Microsoft.VisualStudio.Shell;

[PackageRegistration(UseManagedResourcesOnly = true)]
class Test : [|Package|]
{
}
";
        var withFix = /* lang=c#-test */ @"
using Microsoft.VisualStudio.Shell;

[PackageRegistration(UseManagedResourcesOnly = true, AllowsBackgroundLoading = true)]
class Test : AsyncPackage
{
}
";
        await Verify.VerifyCodeFixAsync(test, withFix);
    }

    [Fact]
    public async Task PackageRegistrationUpdated_ExistingArgumentAsync()
    {
        var test = /* lang=c#-test */ @"
using Microsoft.VisualStudio.Shell;

[PackageRegistration(UseManagedResourcesOnly = true, AllowsBackgroundLoading = false)]
class Test : [|Package|]
{
}
";
        var withFix = /* lang=c#-test */ @"
using Microsoft.VisualStudio.Shell;

[PackageRegistration(UseManagedResourcesOnly = true, AllowsBackgroundLoading = true)]
class Test : AsyncPackage
{
}
";
        await Verify.VerifyCodeFixAsync(test, withFix);
    }

    [Fact]
    public async Task BaseTypeChangesToAsyncPackage_WithInterfacesAsync()
    {
        var test = /* lang=c#-test */ @"
using System;
using Microsoft.VisualStudio.Shell;

class Test : [|Package|], IDisposable
{
    public void Dispose()
    {
    }
}
";
        var withFix = /* lang=c#-test */ @"
using System;
using Microsoft.VisualStudio.Shell;

class Test : AsyncPackage, IDisposable
{
    public void Dispose()
    {
    }
}
";
        await Verify.VerifyCodeFixAsync(test, withFix);
    }

    [Fact]
    public async Task BaseTypeChangesToAsyncPackage_NoUsingsAsync()
    {
        var test = /* lang=c#-test */ @"
class Test : [|Microsoft.VisualStudio.Shell.Package|]
{
}
";
        var withFix = /* lang=c#-test */ @"
using Microsoft.VisualStudio.Shell;

class Test : AsyncPackage
{
}
";
        await Verify.VerifyCodeFixAsync(test, withFix);
    }

    [Fact]
    public async Task BaseTypeChangesToAsyncPackage_NoUsings_AndInitializeMethod()
    {
        var test = /* lang=c#-test */ @"
class Test : [|Microsoft.VisualStudio.Shell.Package|]
{
    protected override void Initialize()
    {
        base.Initialize(); // base invocation
    }
}
";
        var withFix = /* lang=c#-test */ @"using System;
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
        await Verify.VerifyCodeFixAsync(test, withFix);
    }

    [Fact]
    public async Task BaseTypeChangesToAsyncPackage_InPartiallyMatchingNamespaceAsync()
    {
        var test = /* lang=c#-test */ @"
namespace Microsoft.VisualStudio
{
    class Test : [|Microsoft.VisualStudio.Shell.Package|]
    {
    }
}
";
        var withFix = /* lang=c#-test */ @"
using Microsoft.VisualStudio.Shell;

namespace Microsoft.VisualStudio
{
    class Test : AsyncPackage
    {
    }
}
";
        await Verify.VerifyCodeFixAsync(test, withFix);
    }

    [Fact]
    public async Task BaseTypeChangesToAsyncPackage_InPartiallyMatchingNamespace_UsingsInsideNamespace()
    {
        var test = /* lang=c#-test */ @"
namespace Microsoft.VisualStudio
{
    using System;

    class Test : [|Microsoft.VisualStudio.Shell.Package|]
    {
        public String Member { get; set; }
    }
}
";
        var withFix = /* lang=c#-test */ @"
namespace Microsoft.VisualStudio
{
    using System;
    using Microsoft.VisualStudio.Shell;

    class Test : AsyncPackage
    {
        public String Member { get; set; }
    }
}
";
        await Verify.VerifyCodeFixAsync(test, withFix);
    }

    [Fact]
    public async Task InitializeOverride_BecomesAsync()
    {
        var test = /* lang=c#-test */ @"
namespace NS
{
    using System;

    class Test : [|Microsoft.VisualStudio.Shell.Package|]
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
        var withFix = /* lang=c#-test */ @"using Task = System.Threading.Tasks.Task;

namespace NS
{
    using System;
    using System.Threading;
    using System.Threading.Tasks;
    using Microsoft.VisualStudio.Shell;

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
        await Verify.VerifyCodeFixAsync(test, withFix);
    }

    [Fact]
    public async Task InitializeOverride_AlreadyDefinesTaskUsing()
    {
        var test = /* lang=c#-test */ @"
using System;
using Task = System.Threading.Tasks.Task;

class Test : [|Microsoft.VisualStudio.Shell.Package|]
{
    protected override void Initialize()
    {
        base.Initialize();
    }
}
";
        var withFix = /* lang=c#-test */ @"
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
        await Verify.VerifyCodeFixAsync(test, withFix);
    }

    [Fact]
    public async Task InitializeOverride_MissingBaseInitializeCall()
    {
        var test = /* lang=c#-test */ @"
using System;

class Test : [|Microsoft.VisualStudio.Shell.Package|]
{
    protected override void Initialize()
    {
        Console.WriteLine();
    }
}
";
        var withFix = /* lang=c#-test */ @"
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
        await Verify.VerifyCodeFixAsync(test, withFix);
    }

    [Fact]
    public async Task InitializeOverride_GetServiceCallsUpdatedAsync()
    {
        var test = /* lang=c#-test */ @"
using System;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;

class Test : [|Package|]
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
        var withFix = /* lang=c#-test */ @"
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
        await Verify.VerifyCodeFixAsync(test, withFix);
    }
}
