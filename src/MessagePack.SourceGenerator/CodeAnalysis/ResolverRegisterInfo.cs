// Copyright (c) All contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using Microsoft.CodeAnalysis;

namespace MessagePack.SourceGenerator.CodeAnalysis;

public record ResolverRegisterInfo
{
    protected static readonly SymbolDisplayFormat ShortTypeNameFormat = new SymbolDisplayFormat(
        typeQualificationStyle: SymbolDisplayTypeQualificationStyle.NameAndContainingTypes);

    protected static readonly SymbolDisplayFormat BinaryWriteFormat = new SymbolDisplayFormat(
        genericsOptions: SymbolDisplayGenericsOptions.IncludeTypeParameters,
        miscellaneousOptions: SymbolDisplayMiscellaneousOptions.ExpandNullable,
        typeQualificationStyle: SymbolDisplayTypeQualificationStyle.NameOnly);

    /// <summary>
    /// Creates formatter and resolver info for a data type that should be serializable.
    /// </summary>
    /// <param name="dataType">The type to be serialized.</param>
    /// <param name="resolverOptions">The resolver options.</param>
    /// <param name="formatterLocation">Position the formatter under the data type when it must access private members.</param>
    /// <returns>The resolver and formatter information.</returns>
    public static ResolverRegisterInfo Create(INamedTypeSymbol dataType, ResolverOptions resolverOptions, FormatterPosition formatterLocation = FormatterPosition.UnderResolver)
    {
        QualifiedNamedTypeName dataTypeName = new(dataType);

        return new ResolverRegisterInfo
        {
            FormatterLocation = formatterLocation,
            DataType = dataTypeName,
            Formatter = new QualifiedNamedTypeName(TypeKind.Class)
            {
                Container = NestingTypeContainer.From(formatterLocation switch
                {
                    // Keep the real containing chain for namespaced nested types.
                    FormatterPosition.UnderResolver when dataType.ContainingNamespace is { IsGlobalNamespace: false } &&
                        dataTypeName.Container is NestingTypeContainer { NestingType: var containingType } => containingType,
                    FormatterPosition.UnderResolver => GetDefaultFormatterContainer(dataTypeName, resolverOptions),
                    FormatterPosition.UnderDataType => dataTypeName,
                    _ => throw new NotImplementedException(),
                }),
                Name = $"{dataTypeName.Name}Formatter",
                Kind = TypeKind.Class,
                AccessModifier = dataTypeName.AccessModifier,
                TypeParameters = formatterLocation == FormatterPosition.UnderDataType ? ImmutableArray<GenericTypeParameterInfo>.Empty : dataTypeName.TypeParameters,
                TypeArguments = formatterLocation == FormatterPosition.UnderDataType ? ImmutableArray<QualifiedTypeName>.Empty : dataTypeName.TypeArguments,
            },
        };
    }

    /// <summary>
    /// Creates formatter and resolver info for an array data type that should be serializable.
    /// </summary>
    /// <param name="dataType">The type to be serialized.</param>
    /// <param name="resolverOptions">The resolver options.</param>
    /// <param name="formatterLocation">Position the formatter under the data type when it must access private members.</param>
    /// <returns>The resolver and formatter information.</returns>
    public static ResolverRegisterInfo CreateArray(IArrayTypeSymbol dataType, ResolverOptions resolverOptions, FormatterPosition formatterLocation = FormatterPosition.UnderResolver)
    {
        QualifiedArrayTypeName dataTypeName = new(dataType);

        return new ResolverRegisterInfo
        {
            FormatterLocation = formatterLocation,
            DataType = dataTypeName,
            Formatter = new QualifiedNamedTypeName(TypeKind.Class)
            {
                Name = $"XXXFormatter",
            },
        };
    }

    /// <summary>
    /// Gets the name of the type to be formatted.
    /// </summary>
    public required QualifiedTypeName DataType { get; init; }

    /// <summary>
    /// Gets the name of the formatter.
    /// </summary>
    public required QualifiedNamedTypeName Formatter { get; init; }

    /// <summary>
    /// Gets the declaration parent of the formatter.
    /// </summary>
    /// <remarks>
    /// The formatter is always a nested class of something else (the resolver or the data type).
    /// Default placement keeps namespaced types in their partial type hierarchy and uses the
    /// resolver for types in the global namespace.
    /// </remarks>
    public FormatterPosition FormatterLocation { get; init; }

    public virtual string GetFormatterNameForResolver(GenericParameterStyle style = GenericParameterStyle.Identifiers) => this.Formatter.GetQualifiedName(Qualifiers.GlobalNamespace, style);

    /// <summary>
    /// Gets the C# expression that will provide an instance of the formatter.
    /// </summary>
    public virtual string GetFormatterInstanceForResolver() => $"new {this.GetFormatterNameForResolver(GenericParameterStyle.Arguments)}()";

    /// <summary>
    /// Gets a value indicating whether this data type is a generic type with unknown type arguments.
    /// </summary>
    public virtual bool IsUnboundGenericType => this.DataType is QualifiedNamedTypeName { Arity: > 0 };

    public string FileNameHint => $"Formatters.{this.Formatter.GetQualifiedName(Qualifiers.Namespace, GenericParameterStyle.ArityOnly)}";

    private static QualifiedNamedTypeName? GetDefaultFormatterContainer(QualifiedNamedTypeName dataType, ResolverOptions resolverOptions)
    {
        if (dataType.Container is NestingTypeContainer { NestingType: var nestingType })
        {
            // Global-namespace nested types keep their synthetic containing types under the resolver.
            return nestingType with
            {
                Container = NestingTypeContainer.From(GetDefaultFormatterContainer(nestingType, resolverOptions)),
                AccessModifier = Accessibility.Internal,
            };
        }

        if (dataType.Container is NamespaceTypeContainer { Namespace: var ns })
        {
            var name = dataType.Kind == TypeKind.Enum ? "__MessagePack__EnumFormatter" : dataType.GetQualifiedName(Qualifiers.None);
            return new QualifiedNamedTypeName(
                dataType.Kind switch
                {
                    TypeKind.Interface => TypeKind.Interface,
                    TypeKind.Struct => TypeKind.Struct,
                    _ => TypeKind.Class,
                },
                dataType.IsRecord) { Name = name, Container = new NamespaceTypeContainer(ns) };
        }

        return resolverOptions.Name;
    }

    public override int GetHashCode() => this.DataType.GetHashCode();

    public enum FormatterPosition
    {
        UnderResolver,
        UnderDataType,
    }
}
