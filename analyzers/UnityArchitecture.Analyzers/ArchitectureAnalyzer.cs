using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace UnityArchitecture.Analyzers
{
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public sealed class ArchitectureAnalyzer : DiagnosticAnalyzer
    {
        private const string SELECTABLE = "UnityEngine.UI.Selectable";
        private const string INPUT_FIELD = "TMPro.TMP_InputField";
        private const string COMPONENT = "UnityEngine.Component";
        private const string SERIALIZE_FIELD = "UnityEngine.SerializeField";
        private const string EVENT_SYSTEMS = "UnityEngine.EventSystems";
        private const string LINQ = "System.Linq";

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => Rules.All;

        public override void Initialize(AnalysisContext context)
        {
            context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
            context.EnableConcurrentExecution();

            context.RegisterSymbolAction(AnalyzeType, SymbolKind.NamedType);
            context.RegisterSymbolAction(AnalyzeEvent, SymbolKind.Event);
            context.RegisterSymbolAction(AnalyzeMethod, SymbolKind.Method);
            context.RegisterSymbolAction(AnalyzeField, SymbolKind.Field);

            context.RegisterSyntaxNodeAction(AnalyzeIdentifier, SyntaxKind.IdentifierName);
            context.RegisterSyntaxNodeAction(AnalyzeAttribute, SyntaxKind.Attribute);
            context.RegisterSyntaxNodeAction(AnalyzeInvocation, SyntaxKind.InvocationExpression);
            context.RegisterSyntaxNodeAction(AnalyzeConditionalAccess, SyntaxKind.ConditionalAccessExpression);
            context.RegisterSyntaxNodeAction(
                ReportLambda,
                SyntaxKind.SimpleLambdaExpression,
                SyntaxKind.ParenthesizedLambdaExpression,
                SyntaxKind.AnonymousMethodExpression);
            context.RegisterSyntaxNodeAction(ReportLinq, SyntaxKind.QueryExpression);
        }

        private static void AnalyzeType(SymbolAnalysisContext context)
        {
            INamedTypeSymbol type = (INamedTypeSymbol)context.Symbol;
            if (type.TypeKind != TypeKind.Class)
                return;

            if (UnityTypes.IsView(type))
                ReportInputInterfaces(context, type);

            foreach (ISymbol member in type.GetMembers())
                AnalyzeMemberKnowledge(context, type, member);
        }

        private static void ReportInputInterfaces(SymbolAnalysisContext context, INamedTypeSymbol type)
        {
            foreach (INamedTypeSymbol implemented in type.AllInterfaces)
            {
                if (implemented.ContainingNamespace?.ToDisplayString() != EVENT_SYSTEMS)
                    continue;

                context.ReportDiagnostic(Diagnostic.Create(Rules.ViewHandlesInput, type.Locations[0], type.Name, implemented.Name));
                return;
            }
        }

        private static void AnalyzeMemberKnowledge(SymbolAnalysisContext context, INamedTypeSymbol owner, ISymbol member)
        {
            if (member.IsImplicitlyDeclared)
                return;

            switch (member)
            {
                case IFieldSymbol field:
                    CheckKnowledge(context, owner, field, field.Type);
                    break;
                case IPropertySymbol property:
                    CheckKnowledge(context, owner, property, property.Type);
                    break;
                case IMethodSymbol method when method.MethodKind == MethodKind.Ordinary || method.MethodKind == MethodKind.Constructor:
                    CheckKnowledge(context, owner, method, method.ReturnType);
                    foreach (IParameterSymbol parameter in method.Parameters)
                        CheckKnowledge(context, owner, parameter, parameter.Type);
                    break;
            }
        }

        private static void CheckKnowledge(SymbolAnalysisContext context, INamedTypeSymbol owner, ISymbol location, ITypeSymbol referenced)
        {
            switch (referenced)
            {
                case IArrayTypeSymbol array:
                    CheckKnowledge(context, owner, location, array.ElementType);
                    return;
                case INamedTypeSymbol named when named.IsGenericType:
                    foreach (ITypeSymbol argument in named.TypeArguments)
                        CheckKnowledge(context, owner, location, argument);
                    break;
            }

            if (!IsForbidden(owner, referenced))
                return;

            context.ReportDiagnostic(Diagnostic.Create(Rules.Knowledge, location.Locations[0], owner.Name, referenced.Name));
        }

        private static bool IsForbidden(INamedTypeSymbol owner, ITypeSymbol referenced)
        {
            bool isViewOrController = UnityTypes.IsView(referenced) || UnityTypes.IsController(referenced);

            if (UnityTypes.IsModel(owner))
            {
                bool isOtherComponent = UnityTypes.DerivesFrom(referenced, COMPONENT) && !UnityTypes.IsModel(referenced);
                return isViewOrController || isOtherComponent;
            }

            return (UnityTypes.IsView(owner) || UnityTypes.IsController(owner)) && isViewOrController;
        }

        private static void AnalyzeEvent(SymbolAnalysisContext context)
        {
            IEventSymbol symbol = (IEventSymbol)context.Symbol;
            if (symbol.Type is not INamedTypeSymbol handler || handler.DelegateInvokeMethod == null)
                return;

            if (handler.DelegateInvokeMethod.Parameters.Length > 0)
                context.ReportDiagnostic(Diagnostic.Create(Rules.EventWithParameters, symbol.Locations[0], symbol.Name));
        }

        private static void AnalyzeMethod(SymbolAnalysisContext context)
        {
            IMethodSymbol method = (IMethodSymbol)context.Symbol;
            if (method.Name != "Initialize" || method.Parameters.Length == 0)
                return;

            if (UnityTypes.IsMonoBehaviour(method.ContainingType))
                context.ReportDiagnostic(Diagnostic.Create(Rules.MethodInjection, method.Locations[0], method.ContainingType.Name));
        }

        private static void AnalyzeField(SymbolAnalysisContext context)
        {
            IFieldSymbol field = (IFieldSymbol)context.Symbol;
            if (field.IsImplicitlyDeclared || field.IsConst || field.IsStatic)
                return;

            if (UnityTypes.IsController(field.ContainingType) && IsSerializedReference(field))
                context.ReportDiagnostic(Diagnostic.Create(Rules.ControllerSerializesReference, field.Locations[0], field.ContainingType.Name, field.Name));

            if (!UnityTypes.IsMonoBehaviour(field.ContainingType))
                return;

            if (!HasValidVisibility(field, UnityTypes.HasFastIoC(context.Compilation)))
                context.ReportDiagnostic(Diagnostic.Create(Rules.FieldVisibility, field.Locations[0], field.Name));
        }

        private static bool HasValidVisibility(IFieldSymbol field, bool hasFastIoC)
        {
            if (field.DeclaredAccessibility == Accessibility.Private)
                return true;

            if (field.DeclaredAccessibility != Accessibility.Internal)
                return false;

            return !hasFastIoC || field.GetAttributes().Length == 0;
        }

        private static bool IsSerializedReference(IFieldSymbol field)
        {
            if (!UnityTypes.IsUnityObject(field.Type))
                return false;

            foreach (AttributeData attribute in field.GetAttributes())
            {
                if (attribute.AttributeClass?.ToDisplayString() == SERIALIZE_FIELD)
                    return true;
            }

            return false;
        }

        private static void AnalyzeIdentifier(SyntaxNodeAnalysisContext context)
        {
            IdentifierNameSyntax identifier = (IdentifierNameSyntax)context.Node;

            if (IsVarDeclaration(identifier))
            {
                context.ReportDiagnostic(Diagnostic.Create(Rules.Var, identifier.GetLocation()));
                return;
            }

            ISymbol symbol = context.SemanticModel.GetSymbolInfo(identifier, context.CancellationToken).Symbol;
            if (symbol is INamedTypeSymbol type && IsForbiddenSelectable(type))
                context.ReportDiagnostic(Diagnostic.Create(Rules.SelectableComponent, identifier.GetLocation(), type.Name));
        }

        private static bool IsVarDeclaration(IdentifierNameSyntax identifier)
        {
            if (!identifier.IsVar)
                return false;

            return identifier.Parent is VariableDeclarationSyntax
                   || identifier.Parent is ForEachStatementSyntax
                   || identifier.Parent is DeclarationExpressionSyntax;
        }

        private static bool IsForbiddenSelectable(INamedTypeSymbol type)
        {
            return UnityTypes.DerivesFrom(type, SELECTABLE) && !UnityTypes.DerivesFrom(type, INPUT_FIELD);
        }

        private static void AnalyzeAttribute(SyntaxNodeAnalysisContext context)
        {
            if (!UnityTypes.HasFastIoC(context.Compilation))
                return;

            ITypeSymbol attribute = context.SemanticModel.GetTypeInfo(context.Node, context.CancellationToken).Type;
            if (attribute?.Name == "InjectAttribute")
                context.ReportDiagnostic(Diagnostic.Create(Rules.InjectAttribute, context.Node.GetLocation()));
        }

        private static void AnalyzeInvocation(SyntaxNodeAnalysisContext context)
        {
            ISymbol symbol = context.SemanticModel.GetSymbolInfo(context.Node, context.CancellationToken).Symbol;
            if (symbol?.ContainingNamespace?.ToDisplayString() == LINQ)
                context.ReportDiagnostic(Diagnostic.Create(Rules.Linq, context.Node.GetLocation()));
        }

        private static void AnalyzeConditionalAccess(SyntaxNodeAnalysisContext context)
        {
            ConditionalAccessExpressionSyntax access = (ConditionalAccessExpressionSyntax)context.Node;
            ITypeSymbol type = context.SemanticModel.GetTypeInfo(access.Expression, context.CancellationToken).Type;

            if (type != null && UnityTypes.IsUnityObject(type))
                context.ReportDiagnostic(Diagnostic.Create(Rules.ConditionalAccessOnUnityObject, access.GetLocation(), type.Name));
        }

        private static void ReportLambda(SyntaxNodeAnalysisContext context)
        {
            context.ReportDiagnostic(Diagnostic.Create(Rules.Lambda, context.Node.GetLocation()));
        }

        private static void ReportLinq(SyntaxNodeAnalysisContext context)
        {
            context.ReportDiagnostic(Diagnostic.Create(Rules.Linq, context.Node.GetLocation()));
        }
    }
}
