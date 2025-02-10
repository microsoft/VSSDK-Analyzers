// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Generic;

namespace Microsoft.VisualStudio.SDK.Analyzers
{
    /// <summary>
    /// Gets arrays that describe various popular namespaces.
    /// </summary>
    internal static class Namespaces
    {
        /// <summary>
        /// Gets an array for each element in the namespace Microsoft.
        /// </summary>
        internal static readonly IReadOnlyList<string> Microsoft = new[]
        {
            nameof(Microsoft),
        };

        /// <summary>
        /// Gets an array for each element in the namespace Microsoft.VisualStudio.Shell.
        /// </summary>
        internal static readonly IReadOnlyList<string> MicrosoftVisualStudioShell = new[]
        {
            nameof(Microsoft),
            nameof(VisualStudio),
            "Shell",
        };

        /// <summary>
        /// Gets an array for each element in the namespace Microsoft.VisualStudio.Shell.Interop.
        /// </summary>
        internal static readonly IReadOnlyList<string> MicrosoftVisualStudioShellInterop = new[]
        {
            nameof(Microsoft),
            nameof(VisualStudio),
            "Shell",
            "Interop",
        };

        /// <summary>
        /// Gets an array for each element in the namespace Microsoft.VisualStudio.OLE.Interop.
        /// </summary>
        internal static readonly IReadOnlyList<string> MicrosoftVisualStudioOLEInterop = new[]
        {
            nameof(Microsoft),
            nameof(VisualStudio),
            "OLE",
            "Interop",
        };

        /// <summary>
        /// Gets an array for each element in the namespace Microsoft.VisualStudio.Threading.
        /// </summary>
        internal static readonly IReadOnlyList<string> MicrosoftVisualStudioThreading = new[]
        {
            nameof(Microsoft),
            nameof(VisualStudio),
            "Threading",
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
