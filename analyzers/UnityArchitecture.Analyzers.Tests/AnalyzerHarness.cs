using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Diagnostics;

namespace UnityArchitecture.Analyzers.Tests
{
    /// <summary>Compiles a snippet against small stand-ins for Unity types and returns the rule ids it breaks.</summary>
    internal static class AnalyzerHarness
    {
        private const string UNITY_STUBS = @"
namespace UnityEngine
{
    public class Object { }
    public class Component : Object { }
    public class Behaviour : Component { }
    public class MonoBehaviour : Behaviour { }
    public class Transform : Component { }
    public class SerializeField : System.Attribute { }
}
namespace UnityEngine.EventSystems
{
    public interface IPointerClickHandler { }
}
namespace UnityEngine.UI
{
    public class Graphic : MonoBehaviour { }
    public class Image : Graphic { }
    public class Selectable : MonoBehaviour { }
    public class Button : Selectable { }
    public class Toggle : Selectable { }
}
namespace TMPro
{
    public class TMP_InputField : UnityEngine.UI.Selectable { }
}
namespace UIFoundations
{
    public abstract class BaseClickController : UnityEngine.UI.Graphic { }
}
";

        public static async Task<string[]> AnalyzeAsync(string source)
        {
            CSharpCompilation compilation = CSharpCompilation.Create(
                "Client",
                new[] { CSharpSyntaxTree.ParseText(UNITY_STUBS), CSharpSyntaxTree.ParseText(source) },
                GetReferences(),
                new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));

            Diagnostic[] compileErrors = compilation.GetDiagnostics().Where(d => d.Severity == DiagnosticSeverity.Error).ToArray();
            if (compileErrors.Length > 0)
                throw new InvalidOperationException("The test snippet doesn't compile: " + compileErrors[0]);

            ImmutableArray<Diagnostic> diagnostics = await compilation
                .WithAnalyzers(ImmutableArray.Create<DiagnosticAnalyzer>(new ArchitectureAnalyzer()))
                .GetAnalyzerDiagnosticsAsync();

            return diagnostics
                .Where(d => d.Location.SourceTree?.ToString().Contains("namespace UnityEngine") != true)
                .Select(d => d.Id)
                .Distinct()
                .OrderBy(id => id)
                .ToArray();
        }

        private static IEnumerable<MetadataReference> GetReferences()
        {
            string assemblies = (string)AppContext.GetData("TRUSTED_PLATFORM_ASSEMBLIES");
            return assemblies.Split(Path.PathSeparator).Select(path => MetadataReference.CreateFromFile(path));
        }
    }
}
