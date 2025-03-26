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
            nameof(Shell),
        };

        /// <summary>
        /// Gets an array for each element in the namespace Microsoft.VisualStudio.Shell.Interop.
        /// </summary>
        internal static readonly IReadOnlyList<string> MicrosoftVisualStudioShellInterop = new[]
        {
            nameof(Microsoft),
            nameof(VisualStudio),
            nameof(Shell),
            nameof(Shell.Interop),
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
            nameof(Threading),
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

        /// <summary>
        /// Gets an array for each element in the namespace System.ComponentModel.Composition.
        /// </summary>
        internal static readonly IReadOnlyList<string> SystemComponentModelComposition = new[]
        {
            nameof(System),
            nameof(global::System.ComponentModel),
            nameof(global::System.ComponentModel.Composition),
        };

        /// <summary>
        /// Gets an array for each element in the namespace System.Composition.
        /// </summary>
        internal static readonly IReadOnlyList<string> SystemComposition = new[]
        {
            nameof(System),
            nameof(global::System.Composition),
        };
    }
}
