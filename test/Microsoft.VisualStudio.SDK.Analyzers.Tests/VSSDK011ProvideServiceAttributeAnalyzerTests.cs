// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Testing;
using Xunit;
using Verify = CSharpCodeFixVerifier<
    Microsoft.VisualStudio.SDK.Analyzers.VSSDK011ProvideServiceAttributeAnalyzer,
    Microsoft.CodeAnalysis.Testing.EmptyCodeFixProvider>;

public class VSSDK011ProvideServiceAttributeAnalyzerTests
{
    [Fact]
    public async Task AsyncPackageWithoutProvideServiceProducesNoDiagnosticAsync()
    {
        const string Test = /* lang=c#-test */ """
            using Microsoft.VisualStudio.Shell;

            class TestPackage : AsyncPackage
            {
            }
            """;

        await Verify.VerifyAnalyzerAsync(Test);
    }

    [Fact]
    public async Task PackageWithSynchronousServiceProducesNoDiagnosticAsync()
    {
        const string Test = /* lang=c#-test */ """
            using System;
            using System.ComponentModel.Design;
            using Microsoft.VisualStudio.Shell;

            class TestService
            {
            }

            [ProvideService(typeof(TestService))]
            class TestPackage : Package
            {
                private void RegisterService()
                {
                    ((IServiceContainer)this).AddService(typeof(TestService), this.CreateService);
                }

                private object CreateService(IServiceContainer container, Type serviceType)
                {
                    return new TestService();
                }
            }
            """;

        await Verify.VerifyAnalyzerAsync(Test);
    }

    [Fact]
    public async Task AsyncPackageWithAsyncServiceProducesNoDiagnosticAsync()
    {
        const string Test = /* lang=c#-test */ """
            using System;
            using System.Threading;
            using System.Threading.Tasks;
            using Microsoft.VisualStudio.Shell;

            class TestService
            {
            }

            [ProvideService(typeof(TestService), IsAsyncQueryable = true)]
            class TestPackage : AsyncPackage
            {
                protected override async Task InitializeAsync(
                    CancellationToken cancellationToken,
                    IProgress<ServiceProgressData> progress)
                {
                    await base.InitializeAsync(cancellationToken, progress);
                    this.AddService(typeof(TestService), this.CreateServiceAsync);
                }

                private Task<object> CreateServiceAsync(
                    IAsyncServiceContainer container,
                    CancellationToken cancellationToken,
                    Type serviceType)
                {
                    return Task.FromResult<object>(new TestService());
                }
            }
            """;

        await Verify.VerifyAnalyzerAsync(Test);
    }

    [Fact]
    public async Task AsyncPackageWithDefaultServiceProducesDiagnosticAsync()
    {
        const string Test = /* lang=c#-test */ """
            using Microsoft.VisualStudio.Shell;

            class TestService
            {
            }

            [[|ProvideService(typeof(TestService))|]]
            class TestPackage : AsyncPackage
            {
            }
            """;

        await Verify.VerifyAnalyzerAsync(Test);
    }

    [Fact]
    public async Task AsyncPackageWithExplicitSynchronousServiceProducesDiagnosticAsync()
    {
        const string Test = /* lang=c#-test */ """
            using Microsoft.VisualStudio.Shell;

            class TestService
            {
            }

            [ProvideService(typeof(TestService), [|IsAsyncQueryable = false|])]
            class TestPackage : AsyncPackage
            {
            }
            """;

        await Verify.VerifyAnalyzerAsync(Test);
    }

    [Fact]
    public async Task AsyncPackageWithSynchronousServiceFactoryProducesDiagnosticAsync()
    {
        const string Test = /* lang=c#-test */ """
            using System;
            using System.ComponentModel.Design;
            using Microsoft.VisualStudio.Shell;

            class TestService
            {
            }

            [ProvideService(typeof(TestService), IsAsyncQueryable = true)]
            class TestPackage : AsyncPackage
            {
                private void RegisterService()
                {
                    ((IServiceContainer)this).AddService(typeof(TestService), [|this.CreateService|]);
                }

                private object CreateService(IServiceContainer container, Type serviceType)
                {
                    return new TestService();
                }
            }
            """;

        await Verify.VerifyAnalyzerAsync(Test);
    }

    [Fact]
    public async Task AsyncPackageWithServiceInstanceProducesNoDiagnosticAsync()
    {
        const string Test = /* lang=c#-test */ """
            using System.ComponentModel.Design;
            using Microsoft.VisualStudio.Shell;

            class TestService
            {
            }

            [ProvideService(typeof(TestService), IsAsyncQueryable = true)]
            class TestPackage : AsyncPackage
            {
                private void RegisterService()
                {
                    ((IServiceContainer)this).AddService(typeof(TestService), new TestService());
                }
            }
            """;

        await Verify.VerifyAnalyzerAsync(Test);
    }

    [Fact]
    public async Task AsyncPackageWithSynchronousServiceFactoryThroughLocalAliasProducesDiagnosticAsync()
    {
        const string Test = /* lang=c#-test */ """
            using System;
            using System.ComponentModel.Design;
            using Microsoft.VisualStudio.Shell;

            class TestService
            {
            }

            class TestPackage : AsyncPackage
            {
                private void RegisterService()
                {
                    IServiceContainer container = this;
                    container.AddService(typeof(TestService), [|this.CreateService|]);
                }

                private object CreateService(IServiceContainer container, Type serviceType)
                {
                    return new TestService();
                }
            }
            """;

        await Verify.VerifyAnalyzerAsync(Test);
    }

    [Fact]
    public async Task AsyncPackageWithSynchronousServiceFactoryThroughFieldAliasProducesDiagnosticAsync()
    {
        const string Test = /* lang=c#-test */ """
            using System;
            using System.ComponentModel.Design;
            using Microsoft.VisualStudio.Shell;

            class TestService
            {
            }

            class TestPackage : AsyncPackage
            {
                private IServiceContainer serviceContainer;

                private void RegisterService()
                {
                    this.serviceContainer = this;
                    this.serviceContainer.AddService(typeof(TestService), [|this.CreateService|]);
                }

                private object CreateService(IServiceContainer container, Type serviceType)
                {
                    return new TestService();
                }
            }
            """;

        await Verify.VerifyAnalyzerAsync(Test);
    }

    [Fact]
    public async Task AsyncPackageAddingServiceToAnotherContainerProducesNoDiagnosticAsync()
    {
        const string Test = /* lang=c#-test */ """
            using System;
            using System.ComponentModel.Design;
            using Microsoft.VisualStudio.Shell;

            class TestService
            {
            }

            class TestPackage : AsyncPackage
            {
                private void RegisterService(IServiceContainer container)
                {
                    container.AddService(typeof(TestService), this.CreateService);
                }

                private object CreateService(IServiceContainer container, Type serviceType)
                {
                    return new TestService();
                }
            }
            """;

        await Verify.VerifyAnalyzerAsync(Test);
    }

    [Fact]
    public async Task DerivedAsyncPackageWithMultipleServicesProducesDiagnosticsForSynchronousServicesAsync()
    {
        const string Test = /* lang=c#-test */ """
            using Microsoft.VisualStudio.Shell;
            using ServiceAttribute = Microsoft.VisualStudio.Shell.ProvideServiceAttribute;

            class FirstService
            {
            }

            class SecondService
            {
            }

            class ThirdService
            {
            }

            abstract class IntermediatePackage : AsyncPackage
            {
            }

            [[|ServiceAttribute(typeof(FirstService))|]]
            [ServiceAttribute(typeof(SecondService), IsAsyncQueryable = true)]
            [ServiceAttribute(typeof(ThirdService), [|IsAsyncQueryable = false|])]
            class TestPackage : IntermediatePackage
            {
            }
            """;

        await Verify.VerifyAnalyzerAsync(Test);
    }

    [Fact]
    public async Task CompilationWithoutVisualStudioSdkProducesNoDiagnosticAsync()
    {
        const string Test = /* lang=c#-test */ """
            class Test
            {
            }
            """;

        await new Verify.Test(includeVisualStudioSdk: false)
        {
            TestCode = Test,
        }.RunAsync(TestContext.Current.CancellationToken);
    }
}
