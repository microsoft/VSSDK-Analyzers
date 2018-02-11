// Copyright (c) Microsoft Corporation. All rights reserved.

namespace Microsoft.VisualStudio.SDK.Analyzers.Tests
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using System.Threading.Tasks;
    using Microsoft.VisualStudio.Shell;
    using Xunit;

    public class AnalyzerAssemblyTests
    {
        /// <summary>
        /// Verifies that although we compile against MPF for convenience in referencing its types at compile-time,
        /// we maintain that we never compile into an assembly that still references it, since it may not be around at runtime for the CLR to find.
        /// </summary>
        [Fact]
        public void AssemblyHasNoReferenceToMpf()
        {
            Assert.DoesNotContain(
                typeof(VSSDK001DeriveFromAsyncPackageAnalyzer).Assembly.GetReferencedAssemblies(),
                a => a.Name.StartsWith("Microsoft.VisualStudio", StringComparison.OrdinalIgnoreCase));
        }
    }
}
