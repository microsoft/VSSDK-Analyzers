// Copyright (c) Microsoft Corporation. All rights reserved.

namespace Microsoft.VisualStudio.SDK.Analyzers
{
    using System;
    using Microsoft.CodeAnalysis;

    /// <summary>
    /// Internal utilities for use by analyzers.
    /// </summary>
    internal static class Utils
    {
        /// <summary>
        /// Gets the help link to use for each analyzer.
        /// </summary>
        /// <param name="analyzerId">The <see cref="DiagnosticDescriptor.Id"/> for the diagnostic to get help on.</param>
        /// <returns>The absolute URL with documentation on the specified analyzer.</returns>
        internal static string GetHelpLink(string analyzerId)
        {
            return $"https://github.com/Microsoft/VSSDK-Analyzers/blob/master/doc/{analyzerId}.md";
        }
    }
}