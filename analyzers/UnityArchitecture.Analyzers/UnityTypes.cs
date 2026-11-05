using System;
using Microsoft.CodeAnalysis;

namespace UnityArchitecture.Analyzers
{
    /// <summary>Layer and Unity type checks. Layers come from the naming rules in FEATURE_STRUCTURE.md.</summary>
    internal static class UnityTypes
    {
        private const string BASE_CLICK_CONTROLLER = "UIFoundations.BaseClickController";

        public static bool DerivesFrom(ITypeSymbol type, string baseTypeName)
        {
            for (ITypeSymbol current = type; current != null; current = current.BaseType)
            {
                if (current.ToDisplayString() == baseTypeName)
                    return true;
            }

            return false;
        }

        public static bool IsMonoBehaviour(ITypeSymbol type)
        {
            return DerivesFrom(type, "UnityEngine.MonoBehaviour");
        }

        public static bool IsUnityObject(ITypeSymbol type)
        {
            return DerivesFrom(type, "UnityEngine.Object");
        }

        public static bool IsModel(ITypeSymbol type)
        {
            return EndsWith(type, "Model") || EndsWith(type, "Module");
        }

        public static bool IsView(ITypeSymbol type)
        {
            return EndsWith(type, "View");
        }

        public static bool IsController(ITypeSymbol type)
        {
            return EndsWith(type, "Controller") || DerivesFrom(type, BASE_CLICK_CONTROLLER);
        }

        private static bool EndsWith(ITypeSymbol type, string suffix)
        {
            return type.TypeKind == TypeKind.Class
                   && type.Name.EndsWith(suffix, StringComparison.Ordinal)
                   && IsInSource(type);
        }

        private static bool IsInSource(ITypeSymbol type)
        {
            foreach (Location location in type.Locations)
            {
                if (location.IsInSource)
                    return true;
            }

            return false;
        }
    }
}
