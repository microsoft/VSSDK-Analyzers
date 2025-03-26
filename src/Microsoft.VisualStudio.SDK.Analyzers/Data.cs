using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.CodeAnalysis;

namespace Microsoft.VisualStudio.SDK.Analyzers
{

    public readonly struct TypeMatchSpec
    {
        public TypeMatchSpec(QualifiedType type, QualifiedMember member, bool inverted)
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
        /// Gets a value indicating whether a member match is reuqired.
        /// </summary>
        public bool IsMember => this.Member.Name is object;

        /// <summary>
        /// Gets a value indicating whether the typename is a wildcard.
        /// </summary>
        public bool IsWildcard => this.Type.Name == "*";

        /// <summary>
        /// Gets a value indicating whether this is an uninitialized (default) instance.
        /// </summary>
        public bool IsEmpty => this.Type.Namespace is null;

        /// <summary>
        /// Tests whether a given symbol matches the description of a type (independent of its <see cref="InvertedLogic"/> property).
        /// </summary>
        public bool IsMatch(/*[NotNullWhen(true)]*/ ITypeSymbol? typeSymbol, ISymbol? memberSymbol)
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

    public readonly struct QualifiedType
    {
        public QualifiedType(IReadOnlyList<string> containingTypeNamespace, string typeName)
        {
            this.Namespace = containingTypeNamespace;
            this.Name = typeName;
        }

        public IReadOnlyList<string> Namespace { get; }

        public string Name { get; }

        public bool IsMatch(ISymbol symbol)
        {
            return symbol?.Name == this.Name
                && symbol.BelongsToNamespace(this.Namespace);
        }

        public override string ToString() => string.Join(".", this.Namespace.Concat(new[] { this.Name }));
    }

    public readonly struct QualifiedMember
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
