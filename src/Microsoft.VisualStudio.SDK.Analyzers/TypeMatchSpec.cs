// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using Microsoft.CodeAnalysis;

namespace Microsoft.VisualStudio.SDK.Analyzers
{
    // Code copied from Microsoft.VisualStudio.Threading.Analyzers.
#pragma warning disable SA1649 // File name should match first type name.
#pragma warning disable SA1600 // Elements should be documented.
#pragma warning disable SA1615 // Element return value should be documented.
#pragma warning disable SA1611 // Element parameters should be documented.
    internal readonly struct TypeMatchSpec
    {
        internal TypeMatchSpec(QualifiedType type, QualifiedMember member, bool inverted)
        {
            this.InvertedLogic = inverted;
            this.Type = type;
            this.Member = member;

            if (this.IsWildcard && this.Member.Name is object)
            {
                throw new ArgumentException("Wildcard use is not allowed when member of type is specified.");
            }
        }

        /// <summary>
        /// Gets a value indicating whether this entry appeared in a file with a leading "!" character.
        /// </summary>
        public bool InvertedLogic { get; }

        /// <summary>
        /// Gets the type described by this entry.
        /// </summary>
        public QualifiedType Type { get; }

        /// <summary>
        /// Gets the member described by this entry.
        /// </summary>
        public QualifiedMember Member { get; }

        /// <summary>
        /// Gets a value indicating whether a member match is required.
        /// </summary>
        public bool IsMember => this.Member.Name is object;

        /// <summary>
        /// Gets a value indicating whether the typename is a wildcard.
        /// </summary>
        public bool IsWildcard => this.Type.Name == "*";

        /// <summary>
        /// Gets a value indicating whether this is an uninitialized (default) instance.
        /// </summary>
        public bool IsEmpty => this.Type.Namespace.IsDefaultOrEmpty;

        /// <summary>
        /// Tests whether a given symbol matches the description of a type (independent of its <see cref="InvertedLogic"/> property).
        /// </summary>
        public bool IsMatch(ITypeSymbol? typeSymbol, ISymbol? memberSymbol)
        {
            if (typeSymbol is null)
            {
                return false;
            }

            if (!this.IsMember
                && (this.IsWildcard || typeSymbol.Name == this.Type.Name)
                && typeSymbol.BelongsToNamespace(this.Type.Namespace))
            {
                return true;
            }

            if (this.IsMember
                && memberSymbol?.Name == this.Member.Name
                && typeSymbol.Name == this.Type.Name
                && typeSymbol.BelongsToNamespace(this.Type.Namespace))
            {
                return true;
            }

            return false;
        }
    }

    /// <summary>
    /// Specifies a type and its namespace.
    /// Copied from Microsoft.VisualStudio.Threading.Analyzers.
    /// </summary>
    internal readonly struct QualifiedType
    {
        public QualifiedType(ImmutableArray<string> containingTypeNamespace, string typeName)
        {
            this.Namespace = containingTypeNamespace;
            this.Name = typeName;
        }

        public ImmutableArray<string> Namespace { get; }

        public string Name { get; }

        public bool IsMatch(ISymbol symbol)
        {
            return symbol?.Name == this.Name
                && symbol.BelongsToNamespace(this.Namespace);
        }

        public override string ToString() => string.Join(".", this.Namespace.Concat(new[] { this.Name }));
    }

    /// <summary>
    /// Specifies member within a type.
    /// Copied from Microsoft.VisualStudio.Threading.Analyzers.
    /// </summary>
    internal readonly struct QualifiedMember
    {
        public QualifiedMember(QualifiedType containingType, string methodName)
        {
            this.ContainingType = containingType;
            this.Name = methodName;
        }

        public QualifiedType ContainingType { get; }

        public string Name { get; }

        public bool IsMatch(ISymbol? symbol)
        {
            return symbol?.Name == this.Name
                && this.ContainingType.IsMatch(symbol.ContainingType);
        }

        public override string ToString() => this.ContainingType.ToString() + "." + this.Name;
    }
}
#pragma warning restore SA1649 // File name should match first type name
#pragma warning restore SA1600 // Elements should be documented
#pragma warning restore SA1611 // Element parameters should be documented
