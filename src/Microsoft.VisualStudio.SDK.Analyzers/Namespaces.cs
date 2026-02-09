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
        internal static readonly ImmutableArray<string> Microsoft = ImmutableArray.Create(
            nameof(Microsoft));

        /// <summary>
        /// Gets an array for each element in the namespace Microsoft.VisualStudio.Shell.
        /// </summary>
        internal static readonly ImmutableArray<string> MicrosoftVisualStudioShell = ImmutableArray.Create(
            nameof(Microsoft),
            nameof(VisualStudio),
            "Shell");

        /// <summary>
        /// Gets an array for each element in the namespace Microsoft.VisualStudio.Shell.Interop.
        /// </summary>
        internal static readonly ImmutableArray<string> MicrosoftVisualStudioShellInterop = ImmutableArray.Create(
            nameof(Microsoft),
            nameof(VisualStudio),
            "Shell",
            "Interop");

        /// <summary>
        /// Gets an array for each element in the namespace Microsoft.VisualStudio.OLE.Interop.
        /// </summary>
        internal static readonly ImmutableArray<string> MicrosoftVisualStudioOLEInterop = ImmutableArray.Create(
            nameof(Microsoft),
            nameof(VisualStudio),
            "OLE",
            "Interop");

        /// <summary>
        /// Gets an array for each element in the namespace Microsoft.VisualStudio.Threading.
        /// </summary>
        internal static readonly ImmutableArray<string> MicrosoftVisualStudioThreading = ImmutableArray.Create(
            nameof(Microsoft),
            nameof(VisualStudio),
            "Threading");

        /// <summary>
        /// Gets an array for each element in the namespace System.
        /// </summary>
        internal static readonly ImmutableArray<string> System = ImmutableArray.Create(
            nameof(System));

        /// <summary>
        /// Gets an array for each element in the namespace System.Threading.
        /// </summary>
        internal static readonly ImmutableArray<string> SystemThreading = ImmutableArray.Create(
            nameof(System),
            nameof(global::System.Threading));

        /// <summary>
        /// Gets an array for each element in the namespace System.Threading.Tasks.
        /// </summary>
        internal static readonly ImmutableArray<string> SystemThreadingTasks = ImmutableArray.Create(
            nameof(System),
            nameof(global::System.Threading),
            nameof(global::System.Threading.Tasks));

        /// <summary>
        /// Gets an array for each element in the namespace System.ComponentModel.Composition.
        /// </summary>
        internal static readonly ImmutableArray<string> SystemComponentModelComposition = ImmutableArray.Create(
            nameof(System),
            nameof(global::System.ComponentModel),
            "Composition");

        /// <summary>
        /// Gets an array for each element in the namespace System.Composition.
        /// </summary>
        internal static readonly ImmutableArray<string> SystemComposition = ImmutableArray.Create(
            nameof(System),
            "Composition");
    }
}
