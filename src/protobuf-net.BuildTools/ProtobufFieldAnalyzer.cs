﻿using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using ProtoBuf.BuildTools.Internal;
using System.Collections.Immutable;
using System.Linq;

namespace ProtoBuf.BuildTools
{
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public class ProtoBufFieldAnalyzer : DiagnosticAnalyzer
    {
        internal static readonly DiagnosticDescriptor InvalidFieldNumber = new DiagnosticDescriptor(
            id: "PBN0001",
            title: nameof(ProtoBufFieldAnalyzer) + "." + nameof(InvalidFieldNumber),
            messageFormat: "The specified field number {0} is invalid; the valid range is 1-536870911, omitting 19000-19999.",
            category: Literals.CategoryUsage,
            defaultSeverity: DiagnosticSeverity.Error,
            isEnabledByDefault: true);

        internal static readonly DiagnosticDescriptor MemberNotFound = new DiagnosticDescriptor(
            id: "PBN0002",
            title: nameof(ProtoBufFieldAnalyzer) + "." + nameof(MemberNotFound),
            messageFormat: "The specified type member '{0}' could not be resolved.",
            category: Literals.CategoryUsage,
            defaultSeverity: DiagnosticSeverity.Error,
            isEnabledByDefault: true);

        internal static readonly DiagnosticDescriptor DuplicateFieldNumber = new DiagnosticDescriptor(
            id: "PBN0003",
            title: nameof(ProtoBufFieldAnalyzer) + "." + nameof(DuplicateFieldNumber),
            messageFormat: "The specified field number {0} is duplicated; field numbers must be unique between all declared members and includes on a single type.",
            category: Literals.CategoryUsage,
            defaultSeverity: DiagnosticSeverity.Error,
            isEnabledByDefault: true);

        internal static readonly DiagnosticDescriptor ReservedFieldName = new DiagnosticDescriptor(
            id: "PBN0004",
            title: nameof(ProtoBufFieldAnalyzer) + "." + nameof(ReservedFieldName),
            messageFormat: "The specified field name '{0}' is explicitly reserved.",
            category: Literals.CategoryUsage,
            defaultSeverity: DiagnosticSeverity.Warning,
            isEnabledByDefault: true);

        internal static readonly DiagnosticDescriptor ReservedFieldNumber = new DiagnosticDescriptor(
            id: "PBN0005",
            title: nameof(ProtoBufFieldAnalyzer) + "." + nameof(ReservedFieldNumber),
            messageFormat: "The specified field number {0} is explicitly reserved.",
            category: Literals.CategoryUsage,
            defaultSeverity: DiagnosticSeverity.Warning,
            isEnabledByDefault: true);

        internal static readonly DiagnosticDescriptor DuplicateFieldName = new DiagnosticDescriptor(
            id: "PBN0006",
            title: nameof(ProtoBufFieldAnalyzer) + "." + nameof(DuplicateFieldName),
            messageFormat: "The specified field name '{0}' is duplicated; field names should be unique between all declared members on a single type.",
            category: Literals.CategoryUsage,
            defaultSeverity: DiagnosticSeverity.Warning,
            isEnabledByDefault: true);

        internal static readonly DiagnosticDescriptor DuplicateReservation = new DiagnosticDescriptor(
            id: "PBN0007",
            title: nameof(ProtoBufFieldAnalyzer) + "." + nameof(DuplicateReservation),
            messageFormat: "The reservations {0} and {1} overlap each-other.",
            category: Literals.CategoryUsage,
            defaultSeverity: DiagnosticSeverity.Info,
            isEnabledByDefault: true);

        internal static readonly DiagnosticDescriptor DuplicateMemberName = new DiagnosticDescriptor(
            id: "PBN0008",
            title: nameof(ProtoBufFieldAnalyzer) + "." + nameof(DuplicateMemberName),
            messageFormat: "The underlying member '{0}' is described multiple times.",
            category: Literals.CategoryUsage,
            defaultSeverity: DiagnosticSeverity.Error,
            isEnabledByDefault: true);

        internal static readonly DiagnosticDescriptor ShouldBeProtoContract = new DiagnosticDescriptor(
            id: "PBN0009",
            title: nameof(ProtoBufFieldAnalyzer) + "." + nameof(ShouldBeProtoContract),
            messageFormat: "The type is not marked as a proto-contract; additional annotations will be ignored.",
            category: Literals.CategoryUsage,
            defaultSeverity: DiagnosticSeverity.Error,
            isEnabledByDefault: true);

        internal static readonly DiagnosticDescriptor DeclaredAndIgnored = new DiagnosticDescriptor(
            id: "PBN0010",
            title: nameof(ProtoBufFieldAnalyzer) + "." + nameof(DeclaredAndIgnored),
            messageFormat: "The member '{0}' is marked to be ignored; additional annotations will be ignored.",
            category: Literals.CategoryUsage,
            defaultSeverity: DiagnosticSeverity.Error,
            isEnabledByDefault: true);

        internal static readonly DiagnosticDescriptor DuplicateInclude = new DiagnosticDescriptor(
            id: "PBN0011",
            title: nameof(ProtoBufFieldAnalyzer) + "." + nameof(DuplicateInclude),
            messageFormat: "The type '{0}' is declared as an include multiple times.",
            category: Literals.CategoryUsage,
            defaultSeverity: DiagnosticSeverity.Error,
            isEnabledByDefault: true);

        internal static readonly DiagnosticDescriptor IncludeNonDerived = new DiagnosticDescriptor(
            id: "PBN0012",
            title: nameof(ProtoBufFieldAnalyzer) + "." + nameof(IncludeNonDerived),
            messageFormat: "The type '{0}' is declared as an include, but is not a direct sub-type.",
            category: Literals.CategoryUsage,
            defaultSeverity: DiagnosticSeverity.Error,
            isEnabledByDefault: true);


        internal static readonly DiagnosticDescriptor IncludeNotDeclared = new DiagnosticDescriptor(
            id: "PBN0013",
            title: nameof(ProtoBufFieldAnalyzer) + "." + nameof(IncludeNotDeclared),
            messageFormat: "The base-type '{0}' is a proto-contract, but no include is declared for '{1}' and the " + nameof(ProtoContractAttribute.IgnoreUnknownSubTypes) + " flag is not set.",
            category: Literals.CategoryUsage,
            defaultSeverity: DiagnosticSeverity.Warning,
            isEnabledByDefault: true);

        internal static readonly DiagnosticDescriptor SubTypeShouldBeProtoContract = new DiagnosticDescriptor(
            id: "PBN0014",
            title: nameof(ProtoBufFieldAnalyzer) + "." + nameof(SubTypeShouldBeProtoContract),
            messageFormat: "The base-type '{0}' is a proto-contract and the " + nameof(ProtoContractAttribute.IgnoreUnknownSubTypes) + " flag is not set; '{1}' should also be a proto-contract.",
            category: Literals.CategoryUsage,
            defaultSeverity: DiagnosticSeverity.Warning,
            isEnabledByDefault: true);

        internal static readonly DiagnosticDescriptor ConstructorMissing = new DiagnosticDescriptor(
            id: "PBN0015",
            title: nameof(ProtoBufFieldAnalyzer) + "." + nameof(ConstructorMissing),
            messageFormat: "There is no suitable (parameterless) constructor available for the proto-contract, and the " + nameof(ProtoContractAttribute.SkipConstructor) + " flag is not set.",
            category: Literals.CategoryUsage,
            defaultSeverity: DiagnosticSeverity.Error,
            isEnabledByDefault: true);

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; } = Utils.GetDeclared(typeof(ProtoBufFieldAnalyzer));

        private static readonly ImmutableArray<SyntaxKind> s_syntaxKinds =
            ImmutableArray.Create(SyntaxKind.ClassDeclaration, SyntaxKind.StructDeclaration);
        public override void Initialize(AnalysisContext ctx)
        {
            ctx.EnableConcurrentExecution();
            ctx.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.Analyze);
            ctx.RegisterSyntaxNodeAction(ConsiderPossibleProtoBufType, s_syntaxKinds);
        }

        private static void ConsiderPossibleProtoBufType(SyntaxNodeAnalysisContext context)
        {
            if (!(context.ContainingSymbol is ITypeSymbol type)) return;
            
            var attribs = type.GetAttributes();

            TypeContext? typeContext = null;
            TypeContext Context() => typeContext ??= new TypeContext();
            bool hasAnyConstructor = false, hasParameterlessConstructor = false;
            foreach (var attrib in type.GetAttributes())
            {
                var ac = attrib.AttributeClass;
                if (ac is null) continue;
                switch (ac.Name)
                {
                    case nameof(ProtoContractAttribute) when ac.InProtoBufNamespace():
                        Context().SetContract(type, attrib);
                        break;
                    case nameof(ProtoIncludeAttribute) when ac.InProtoBufNamespace():
                        Context().AddInclude(type, attrib);
                        break;
                    case nameof(ProtoReservedAttribute) when ac.InProtoBufNamespace():
                        Context().AddReserved(type, attrib);
                        break;
                    case nameof(ProtoPartialMemberAttribute) when ac.InProtoBufNamespace():
                        Context().AddMember(type, attrib, null);
                        break;
                    case nameof(ProtoPartialIgnoreAttribute) when ac.InProtoBufNamespace():
                        Context().AddIgnore(type, attrib, null);
                        break;
                    case nameof(CompatibilityLevelAttribute) when ac.InProtoBufNamespace():
                        break;
                }
            }

            foreach (var member in type.GetMembers())
            {
                switch (member)
                {
                    case IPropertySymbol:
                    case IFieldSymbol:
                        var memberAttribs = member.GetAttributes();
                        foreach (var attrib in memberAttribs)
                        {
                            var ac = attrib.AttributeClass;
                            if (ac is null) continue;

                            switch (ac.Name)
                            {
                                case nameof(ProtoMemberAttribute) when ac.InProtoBufNamespace():
                                    Context().AddMember(member, attrib, member.Name);
                                    break;
                                case nameof(ProtoIgnoreAttribute) when ac.InProtoBufNamespace():
                                    Context().AddIgnore(member, attrib, member.Name);
                                    break;
                                case nameof(ProtoMapAttribute) when ac.InProtoBufNamespace():
                                    break;
                                case nameof(CompatibilityLevelAttribute) when ac.InProtoBufNamespace():
                                    break;
                            }
                        }
                        break;
                    case IMethodSymbol method when method.MethodKind == MethodKind.Constructor:
                        hasAnyConstructor = true;
                        if (!method.Parameters.Any()) hasParameterlessConstructor = true;
                        break;
                }
            }
            if (typeContext is not null)
            {
                if (hasAnyConstructor && !hasParameterlessConstructor
                    && typeContext.HasFlag(TypeContextFlags.IsProtoContract)
                    && !typeContext.HasFlag(TypeContextFlags.SkipConstructor)
                )
                {
                    context.ReportDiagnostic(Diagnostic.Create(
                        descriptor: ProtoBufFieldAnalyzer.ConstructorMissing,
                        location: Utils.PickLocation(ref context, type),
                        messageArgs: null,
                        additionalLocations: null,
                        properties: null
                    ));
                }
                typeContext.ReportProblems(context, type);
            }

            if (type.BaseType is not null)
            {
                bool baseIsContract = false, currentTypeIsDeclared = false, baseSkipsUnknownSubtypes = false;
                foreach (var attrib in type.BaseType.GetAttributes())
                {
                    var ac = attrib.AttributeClass;
                    if (ac is null) continue;
                    switch (ac.Name)
                    {
                        case nameof(ProtoContractAttribute) when ac.InProtoBufNamespace():
                            if (attrib.TryGetBooleanByName(nameof(ProtoContractAttribute.IgnoreUnknownSubTypes), out var b))
                                baseSkipsUnknownSubtypes = b;
                            baseIsContract = true;
                            break;
                        case nameof(ProtoIncludeAttribute) when ac.InProtoBufNamespace():
                            if (attrib.TryGetTypeByName(nameof(ProtoIncludeAttribute.KnownType), out var knownType) &&
                                SymbolEqualityComparer.Default.Equals(knownType, type))
                            {
                                currentTypeIsDeclared = true;
                            }
                            break;
                    }
                }
                if (baseIsContract && !baseSkipsUnknownSubtypes)
                {
                    if (typeContext is null || !typeContext.HasFlag(TypeContextFlags.IsProtoContract))
                    {
                        context.ReportDiagnostic(Diagnostic.Create(
                            descriptor: ProtoBufFieldAnalyzer.SubTypeShouldBeProtoContract,
                            location: Utils.PickLocation(ref context, type),
                            messageArgs: new object[] { type.BaseType.ToDisplayString(), type.ToDisplayString() },
                            additionalLocations: null,
                            properties: null
                        ));
                    }
                    if (!currentTypeIsDeclared)
                    {
                        context.ReportDiagnostic(Diagnostic.Create(
                            descriptor: ProtoBufFieldAnalyzer.IncludeNotDeclared,
                            location: Utils.PickLocation(ref context, type),
                            messageArgs: new object[] { type.BaseType.ToDisplayString(), type.ToDisplayString() },
                            additionalLocations: null,
                            properties: null
                        ));
                    }

                }
            }
        }
    }
}