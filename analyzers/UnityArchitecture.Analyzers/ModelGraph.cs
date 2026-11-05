using System.Collections.Concurrent;
using System.Collections.Generic;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;

namespace UnityArchitecture.Analyzers
{
    /// <summary>Collects the Model-to-Model references in a compilation and reports the ones that form a cycle.</summary>
    internal sealed class ModelGraph
    {
        private readonly ConcurrentDictionary<INamedTypeSymbol, HashSet<INamedTypeSymbol>> dependencies =
            new ConcurrentDictionary<INamedTypeSymbol, HashSet<INamedTypeSymbol>>(SymbolEqualityComparer.Default);

        public static void Register(CompilationStartAnalysisContext context)
        {
            ModelGraph graph = new ModelGraph();

            context.RegisterSymbolAction(graph.CollectDependencies, SymbolKind.NamedType);
            context.RegisterCompilationEndAction(graph.ReportCycles);
        }

        private void CollectDependencies(SymbolAnalysisContext context)
        {
            INamedTypeSymbol type = (INamedTypeSymbol)context.Symbol;
            if (type.TypeKind != TypeKind.Class || !UnityTypes.IsModel(type))
                return;

            HashSet<INamedTypeSymbol> referencedModels = new HashSet<INamedTypeSymbol>(SymbolEqualityComparer.Default);

            foreach (ISymbol member in type.GetMembers())
                AddMemberModels(type, member, referencedModels);

            dependencies[type] = referencedModels;
        }

        private static void AddMemberModels(INamedTypeSymbol owner, ISymbol member, HashSet<INamedTypeSymbol> referencedModels)
        {
            switch (member)
            {
                case IFieldSymbol field:
                    AddModels(owner, field.Type, referencedModels);
                    break;
                case IPropertySymbol property:
                    AddModels(owner, property.Type, referencedModels);
                    break;
                case IMethodSymbol constructor when constructor.MethodKind == MethodKind.Constructor:
                    foreach (IParameterSymbol parameter in constructor.Parameters)
                        AddModels(owner, parameter.Type, referencedModels);
                    break;
            }
        }

        private static void AddModels(INamedTypeSymbol owner, ITypeSymbol referenced, HashSet<INamedTypeSymbol> referencedModels)
        {
            switch (referenced)
            {
                case IArrayTypeSymbol array:
                    AddModels(owner, array.ElementType, referencedModels);
                    return;
                case INamedTypeSymbol named when named.IsGenericType:
                    foreach (ITypeSymbol argument in named.TypeArguments)
                        AddModels(owner, argument, referencedModels);
                    break;
            }

            // A singleton's Instance property refers to its own type, which isn't a dependency.
            if (referenced is INamedTypeSymbol model
                && UnityTypes.IsModel(model)
                && !SymbolEqualityComparer.Default.Equals(model, owner))
                referencedModels.Add(model);
        }

        private void ReportCycles(CompilationAnalysisContext context)
        {
            foreach (KeyValuePair<INamedTypeSymbol, HashSet<INamedTypeSymbol>> model in dependencies)
                ReportCycle(context, model.Key, model.Value);
        }

        private void ReportCycle(CompilationAnalysisContext context, INamedTypeSymbol model, HashSet<INamedTypeSymbol> referencedModels)
        {
            foreach (INamedTypeSymbol referenced in referencedModels)
            {
                HashSet<INamedTypeSymbol> visited = new HashSet<INamedTypeSymbol>(SymbolEqualityComparer.Default);
                if (!Reaches(referenced, model, visited))
                    continue;

                context.ReportDiagnostic(Diagnostic.Create(Rules.ModelCycle, model.Locations[0], model.Name, referenced.Name));
                return;
            }
        }

        private bool Reaches(INamedTypeSymbol from, INamedTypeSymbol target, HashSet<INamedTypeSymbol> visited)
        {
            if (SymbolEqualityComparer.Default.Equals(from, target))
                return true;

            if (!visited.Add(from) || !dependencies.TryGetValue(from, out HashSet<INamedTypeSymbol> referencedModels))
                return false;

            foreach (INamedTypeSymbol referenced in referencedModels)
            {
                if (Reaches(referenced, target, visited))
                    return true;
            }

            return false;
        }
    }
}
