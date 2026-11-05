using System.Collections.Immutable;
using Microsoft.CodeAnalysis;

namespace UnityArchitecture.Analyzers
{
    /// <summary>
    /// The mechanical rules from the checklist in architecture/AGENT_WORKFLOW.md.
    /// Every message says how to fix the problem, because agents act on it directly.
    /// </summary>
    internal static class Rules
    {
        private const string CATEGORY = "Architecture";

        public static readonly DiagnosticDescriptor Knowledge = Create(
            "UA0001",
            "A layer references something it must not know",
            "{0} must not know {1}. Models know only Models, and Views and Controllers know only Models. Read the Model instead.");

        public static readonly DiagnosticDescriptor ViewHandlesInput = Create(
            "UA0002",
            "A View handles input",
            "{0} implements {1}. Views only render: move input handling to a Controller.");

        public static readonly DiagnosticDescriptor ControllerSerializesReference = Create(
            "UA0003",
            "A Controller serializes a reference",
            "{0} serializes the reference {1}. Controllers serialize only config values: get Models through injection.");

        public static readonly DiagnosticDescriptor EventWithParameters = Create(
            "UA0004",
            "An event has parameters",
            "Event {0} has parameters. Use 'public event Action {0};' and let listeners read the Model.");

        public static readonly DiagnosticDescriptor SelectableComponent = Create(
            "UA0005",
            "A Selectable component is used",
            "{0} is a View and a Controller in one component. Use a Controller with IPointerClickHandler or IPointerDownHandler. See architecture/UI_PATTERNS.md.");

        public static readonly DiagnosticDescriptor MethodInjection = Create(
            "UA0006",
            "A MonoBehaviour uses method injection",
            "{0}.Initialize takes parameters. Use injected internal fields instead of method injection.");

        public static readonly DiagnosticDescriptor InjectAttribute = Create(
            "UA0007",
            "An injection attribute is used",
            "Remove [Inject]. Injected fields are internal with no attribute.");

        public static readonly DiagnosticDescriptor FieldVisibility = Create(
            "UA0008",
            "A MonoBehaviour field has the wrong shape",
            "Field {0} on a MonoBehaviour must be private or internal. With Fast IoC, an internal field has no attribute.");

        public static readonly DiagnosticDescriptor Lambda = Create(
            "UA0009",
            "A lambda or anonymous method is used",
            "Replace this lambda or anonymous method with a named method. It allocates, and a named method can be unsubscribed.");

        public static readonly DiagnosticDescriptor Linq = Create(
            "UA0010",
            "LINQ is used",
            "Replace LINQ with a for loop. It allocates.");

        public static readonly DiagnosticDescriptor Var = Create(
            "UA0011",
            "var is used",
            "Replace var with the explicit type.");

        public static readonly DiagnosticDescriptor ConditionalAccessOnUnityObject = Create(
            "UA0012",
            "?. is used on a Unity object",
            "Don't use ?. on {0}. It skips Unity's destroyed-object check: compare with null explicitly.");

        public static readonly ImmutableArray<DiagnosticDescriptor> All = ImmutableArray.Create(
            Knowledge,
            ViewHandlesInput,
            ControllerSerializesReference,
            EventWithParameters,
            SelectableComponent,
            MethodInjection,
            InjectAttribute,
            FieldVisibility,
            Lambda,
            Linq,
            Var,
            ConditionalAccessOnUnityObject);

        private static DiagnosticDescriptor Create(string id, string title, string message)
        {
            return new DiagnosticDescriptor(id, title, message, CATEGORY, DiagnosticSeverity.Error, isEnabledByDefault: true);
        }
    }
}
