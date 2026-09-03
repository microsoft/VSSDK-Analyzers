// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Testing;
using Xunit;
using Verify = CSharpCodeFixVerifier<
    Microsoft.VisualStudio.SDK.Analyzers.VSSDK012UseLatestVisualStudioServicesVersionAnalyzer,
    Microsoft.CodeAnalysis.Testing.EmptyCodeFixProvider>;

public class VSSDK012UseLatestVisualStudioServicesVersionAnalyzerTests
{
    [Fact]
    public async Task OlderVersionProducesDiagnosticAsync()
    {
        var test = /* lang=c#-test */ """
            using Microsoft.VisualStudio;

            class Test
            {
                void M()
                {
                    _ = {|#0:VisualStudioServices.VS2019_9|}.DiagnosticManagerService;
                }
            }

            namespace Microsoft.VisualStudio
            {
                public static class VisualStudioServices
                {
                    public static VS2019_9Services VS2019_9 => new VS2019_9Services();
                    public static VS2019_10Services VS2019_10 => new VS2019_10Services();
                }

                public class VS2019_9Services
                {
                    public object DiagnosticManagerService => new object();
                }

                public class VS2019_10Services : VS2019_9Services
                {
                }
            }
            """;

        await VerifyAnalyzerAsync(test, Verify.Diagnostic().WithLocation(0).WithArguments("VS2019_9", "VS2019_10"));
    }

    [Fact]
    public async Task VersionFromOlderVisualStudioReleaseProducesDiagnosticAsync()
    {
        var test = /* lang=c#-test */ """
            using Microsoft.VisualStudio;

            class Test
            {
                void M()
                {
                    _ = {|#0:VisualStudioServices.VS2019_11|};
                }
            }

            namespace Microsoft.VisualStudio
            {
                public static class VisualStudioServices
                {
                    public static object VS2019_11 => new object();
                    public static object VS2022 => new object();
                }
            }
            """;

        await VerifyAnalyzerAsync(test, Verify.Diagnostic().WithLocation(0).WithArguments("VS2019_11", "VS2022"));
    }

    [Fact]
    public async Task LatestVersionProducesNoDiagnosticAsync()
    {
        var test = /* lang=c#-test */ """
            using Microsoft.VisualStudio;

            class Test
            {
                void M()
                {
                    _ = VisualStudioServices.VS2022_10;
                }
            }

            namespace Microsoft.VisualStudio
            {
                public static class VisualStudioServices
                {
                    public static object VS2022_9 => new object();
                    public static object VS2022_10 => new object();
                }
            }
            """;

        await VerifyAnalyzerAsync(test);
    }

    [Fact]
    public async Task OnlyAvailableVersionProducesNoDiagnosticAsync()
    {
        var test = /* lang=c#-test */ """
            using Microsoft.VisualStudio;

            class Test
            {
                void M()
                {
                    _ = VisualStudioServices.VS2019_7;
                }
            }

            namespace Microsoft.VisualStudio
            {
                public static class VisualStudioServices
                {
                    public static object VS2019_7 => new object();
                }
            }
            """;

        await VerifyAnalyzerAsync(test);
    }

    [Fact]
    public async Task NonVersionedMemberProducesNoDiagnosticAsync()
    {
        var test = /* lang=c#-test */ """
            using Microsoft.VisualStudio;

            class Test
            {
                void M()
                {
                    _ = VisualStudioServices.Default;
                }
            }

            namespace Microsoft.VisualStudio
            {
                public static class VisualStudioServices
                {
                    public static object Default => new object();
                    public static object VS2022 => new object();
                }
            }
            """;

        await VerifyAnalyzerAsync(test);
    }

    [Fact]
    public async Task NameOfOlderVersionProducesNoDiagnosticAsync()
    {
        var test = /* lang=c#-test */ """
            using Microsoft.VisualStudio;

            class Test
            {
                string M()
                {
                    return nameof(VisualStudioServices.VS2019_7);
                }
            }

            namespace Microsoft.VisualStudio
            {
                public static class VisualStudioServices
                {
                    public static object VS2019_7 => new object();
                    public static object VS2022 => new object();
                }
            }
            """;

        await VerifyAnalyzerAsync(test);
    }

    [Fact]
    public async Task UnrelatedTypeProducesNoDiagnosticAsync()
    {
        var test = /* lang=c#-test */ """
            class Test
            {
                void M()
                {
                    _ = Contoso.VisualStudioServices.VS2019_7;
                }
            }

            namespace Contoso
            {
                public static class VisualStudioServices
                {
                    public static object VS2019_7 => new object();
                    public static object VS2022 => new object();
                }
            }
            """;

        await VerifyAnalyzerAsync(test);
    }

    private static Task VerifyAnalyzerAsync(string source, params DiagnosticResult[] expected)
    {
        var test = new Verify.Test(includeVisualStudioSdk: false)
        {
            TestCode = source,
        };
        test.ExpectedDiagnostics.AddRange(expected);
        return test.RunAsync();
    }
}
