// Copyright (c) Microsoft Corporation. All rights reserved.

namespace Microsoft.VisualStudio.SDK.Analyzers
{
    using System.Collections.Generic;

    /// <summary>
    /// Gets magic strings that describe types and their members.
    /// </summary>
    internal static class Types
    {
        /// <summary>
        /// Describes the <see cref="Shell.AsyncPackage"/> type.
        /// </summary>
        internal static class AsyncPackage
        {
            /// <summary>
            /// Gets the simple name of the <see cref="Shell.AsyncPackage"/> type.
            /// </summary>
            internal const string TypeName = nameof(Shell.AsyncPackage);

            /// <summary>
            /// Gets an array of the nesting namespaces for this type.
            /// </summary>
            internal static readonly IReadOnlyList<string> Namespace = Namespaces.MicrosoftVisualStudioShell;
        }
    }
}
