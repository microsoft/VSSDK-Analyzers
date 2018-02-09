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

        /// <summary>
        /// Gets an array for each element in the namespace System.
        /// </summary>
        internal static readonly IReadOnlyList<string> System = new[]
        {
            nameof(System),
        };

        /// <summary>
        /// Gets an array for each element in the namespace System.Threading.
        /// </summary>
        internal static readonly IReadOnlyList<string> SystemThreading = new[]
        {
            nameof(System),
            nameof(global::System.Threading),
        };

        /// <summary>
        /// Gets an array for each element in the namespace System.Threading.Tasks.
        /// </summary>
        internal static readonly IReadOnlyList<string> SystemThreadingTasks = new[]
        {
            nameof(System),
            nameof(global::System.Threading),
            nameof(global::System.Threading.Tasks),
        };
    }
}
