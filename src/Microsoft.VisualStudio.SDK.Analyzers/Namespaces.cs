// Copyright (c) Microsoft Corporation. All rights reserved.

namespace Microsoft.VisualStudio.SDK.Analyzers
{
    using System.Collections.Generic;

    /// <summary>
    /// Gets arrays that describe various popular namespaces.
    /// </summary>
    internal static class Namespaces
    {
        /// <summary>
        /// Gets an array for each element in the namespace Microsoft.VisualStudio.Shell.
        /// </summary>
        internal static readonly IReadOnlyList<string> MicrosoftVisualStudioShell = new[]
        {
            nameof(Microsoft),
            nameof(VisualStudio),
            nameof(Shell),
        };
    }
}
