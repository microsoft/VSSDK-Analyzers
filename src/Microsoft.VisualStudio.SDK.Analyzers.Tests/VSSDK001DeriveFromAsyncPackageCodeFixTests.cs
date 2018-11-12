// Copyright (c) Microsoft Corporation. All rights reserved.

using System.Threading.Tasks;
using Xunit;
using Verify = CSharpCodeFixVerifier<
    Microsoft.VisualStudio.SDK.Analyzers.VSSDK001DeriveFromAsyncPackageAnalyzer,
    Microsoft.VisualStudio.SDK.Analyzers.VSSDK001DeriveFromAsyncPackageCodeFix>;

public class VSSDK001DeriveFromAsyncPackageCodeFixTests
{
    [Fact]
    public async Task BaseTypeChangesToAsyncPackageAsync()
    {
        var test = @"
using Microsoft.VisualStudio.Shell;

class Test : [|Package|]
{
}
";
        var withFix = @"
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
        var test = @"
using Microsoft.VisualStudio.Shell;

[PackageRegistration(UseManagedResourcesOnly = true)]
class Test : [|Package|]
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
        await Verify.VerifyCodeFixAsync(test, withFix);
    }

    [Fact]
    public async Task PackageRegistrationUpdated_ExistingArgumentAsync()
    {
        var test = @"
using Microsoft.VisualStudio.Shell;

[PackageRegistration(UseManagedResourcesOnly = true, AllowsBackgroundLoading = false)]
class Test : [|Package|]
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
        await Verify.VerifyCodeFixAsync(test, withFix);
    }

    [Fact]
    public async Task BaseTypeChangesToAsyncPackage_WithInterfacesAsync()
    {
        var test = @"
using System;
using Microsoft.VisualStudio.Shell;

class Test : [|Package|], IDisposable
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
        await Verify.VerifyCodeFixAsync(test, withFix);
    }

    [Fact]
    public async Task BaseTypeChangesToAsyncPackage_NoUsingsAsync()
    {
        var test = @"
class Test : [|Microsoft.VisualStudio.Shell.Package|]
{
}
";
        var withFix = @"
class Test : Microsoft.VisualStudio.Shell.AsyncPackage
{
}
";
        await Verify.VerifyCodeFixAsync(test, withFix);
    }

    [Fact]
    public async Task BaseTypeChangesToAsyncPackage_InPartiallyMatchingNamespaceAsync()
    {
        var test = @"
namespace Microsoft.VisualStudio
{
    class Test : [|Microsoft.VisualStudio.Shell.Package|]
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
        await Verify.VerifyCodeFixAsync(test, withFix);
    }

    [Fact]
    public async Task InitializeOverride_BecomesAsync()
    {
        var test = @"
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
";
        var withFix = @"
using System;

class Test : Microsoft.VisualStudio.Shell.AsyncPackage
{
    protected override async System.Threading.Tasks.Task InitializeAsync(System.Threading.CancellationToken cancellationToken, IProgress<Microsoft.VisualStudio.Shell.ServiceProgressData> progress)
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
";
        await Verify.VerifyCodeFixAsync(test, withFix);
    }

    [Fact]
    public async Task InitializeOverride_GetServiceCallsUpdatedAsync()
    {
        var test = @"
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
        var withFix = @"
using System;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;

class Test : AsyncPackage
{
    protected override async System.Threading.Tasks.Task InitializeAsync(System.Threading.CancellationToken cancellationToken, IProgress<ServiceProgressData> progress)
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
