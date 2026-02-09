// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using System.Collections.Immutable;

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
        internal static readonly ImmutableArray<string> Microsoft = [
            nameof(Microsoft),
        ];

        /// <summary>
        /// Gets an array for each element in the namespace Microsoft.VisualStudio.Shell.
        /// </summary>
        internal static readonly ImmutableArray<string> MicrosoftVisualStudioShell = [
            nameof(Microsoft),
            nameof(VisualStudio),
            "Shell",
        ];

        /// <summary>
        /// Gets an array for each element in the namespace Microsoft.VisualStudio.Shell.Interop.
        /// </summary>
        internal static readonly ImmutableArray<string> MicrosoftVisualStudioShellInterop = [
            nameof(Microsoft),
            nameof(VisualStudio),
            "Shell",
            "Interop",
        ];

        /// <summary>
        /// Gets an array for each element in the namespace Microsoft.VisualStudio.OLE.Interop.
        /// </summary>
        internal static readonly ImmutableArray<string> MicrosoftVisualStudioOLEInterop = [
            nameof(Microsoft),
            nameof(VisualStudio),
            "OLE",
            "Interop",
        ];

        /// <summary>
        /// Gets an array for each element in the namespace Microsoft.VisualStudio.Threading.
        /// </summary>
        internal static readonly ImmutableArray<string> MicrosoftVisualStudioThreading = [
            nameof(Microsoft),
            nameof(VisualStudio),
            "Threading",
        ];

        /// <summary>
        /// Gets an array for each element in the namespace System.
        /// </summary>
        internal static readonly ImmutableArray<string> System = [
            nameof(System),
        ];

        /// <summary>
        /// Gets an array for each element in the namespace System.Threading.
        /// </summary>
        internal static readonly ImmutableArray<string> SystemThreading = [
            nameof(System),
            nameof(global::System.Threading),
        ];

        /// <summary>
        /// Gets an array for each element in the namespace System.Threading.Tasks.
        /// </summary>
        internal static readonly ImmutableArray<string> SystemThreadingTasks = [
            nameof(System),
            nameof(global::System.Threading),
            nameof(global::System.Threading.Tasks),
        ];

        /// <summary>
        /// Gets an array for each element in the namespace System.ComponentModel.Composition.
        /// </summary>
        internal static readonly ImmutableArray<string> SystemComponentModelComposition = [
            nameof(System),
            nameof(global::System.ComponentModel),
            nameof(global::System.ComponentModel.Composition),
        ];

        /// <summary>
        /// Gets an array for each element in the namespace System.Composition.
        /// </summary>
        internal static readonly ImmutableArray<string> SystemComposition = [
            nameof(System),
            nameof(global::System.Composition),
        ];
    }
}
