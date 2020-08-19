using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime;
using System.Text.RegularExpressions;
using egregore.Controllers;
using egregore.Data;
using egregore.Ontology;
using Microsoft.AspNetCore.Mvc.ApplicationParts;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.Extensions.Logging;

namespace egregore.Configuration
{
    public class DynamicControllerFeatureProvider : IApplicationFeatureProvider<ControllerFeature>
    {
        private readonly IOntologyLog _ontology;
        private readonly ILogger<DynamicControllerFeatureProvider> _logger;
        private IEnumerable<PortableExecutableReference> _references;

        public DynamicControllerFeatureProvider(IOntologyLog ontology, ILogger<DynamicControllerFeatureProvider> logger)
        {
            _ontology = ontology;
            _logger = logger;
        }

        public void PopulateFeature(IEnumerable<ApplicationPart> parts, ControllerFeature feature)
        {
            _references ??= GetReferences();
            var code = _ontology.GenerateModels();

            var syntaxTrees = new[] {CSharpSyntaxTree.ParseText(code)};
            var assemblyName = $"__egregore_V{_ontology.Index}";
            var options = new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary);
            var compilation = CSharpCompilation.Create(assemblyName, syntaxTrees, _references, options);

            using var ms = new MemoryStream();

            var result = compilation.Emit(ms);
 
            if (!result.Success)
            {
                foreach (var diagnostic in result.Diagnostics)
                {
                    if (diagnostic.IsSuppressed)
                        continue;

                    var message = diagnostic.GetMessage();
                    var eventId = new EventId(diagnostic.WarningLevel);

                    switch (diagnostic.Severity)
                    {
                        case DiagnosticSeverity.Hidden:
                            break;
                        case DiagnosticSeverity.Info:
                            _logger.LogInformation(eventId, message);
                            break;
                        case DiagnosticSeverity.Warning when diagnostic.IsWarningAsError:
                            _logger.LogError(eventId, message);
                            break;
                        case DiagnosticSeverity.Warning:
                            _logger.LogWarning(eventId, message);
                            break;
                        case DiagnosticSeverity.Error:
                            _logger.LogError(eventId, message);
                            break;
                        default:
                            throw new ArgumentOutOfRangeException();
                    }
                }
                return;
            }
 
            ms.Seek(0, SeekOrigin.Begin);
            var assembly = Assembly.Load(ms.ToArray());
            var types = assembly.GetExportedTypes();

            var baseType = typeof(DynamicController<>);
            foreach (var type in types)
                feature.Controllers.Add(baseType.MakeGenericType(type).GetTypeInfo());
        }

        private static IEnumerable<PortableExecutableReference> GetReferences()
        {
            return new[] {
                UsingSystem(),
                UsingSystemData(),
                UsingSystemCollections(),
                UsingSystemComponentModel(),
                UsingSystemLinq(),
                UsingSystemRuntime(),
                UsingSystemText(),
                UsingEgregore(),
            }.Distinct();
        }

        private static PortableExecutableReference UsingSystem() => MetadataReference.CreateFromFile(typeof(object).Assembly.Location);
        private static PortableExecutableReference UsingSystemData() => MetadataReference.CreateFromFile(typeof(IDataReader).Assembly.Location);
        private static PortableExecutableReference UsingSystemCollections() => MetadataReference.CreateFromFile(typeof(StructuralComparisons).Assembly.Location);
        private static PortableExecutableReference UsingSystemComponentModel() => MetadataReference.CreateFromFile(typeof(ReadOnlyAttribute).Assembly.Location);
        private static PortableExecutableReference UsingSystemLinq() => MetadataReference.CreateFromFile(typeof(Enumerable).Assembly.Location);
        private static PortableExecutableReference UsingSystemRuntime() => MetadataReference.CreateFromFile(typeof(AssemblyTargetedPatchBandAttribute).Assembly.Location);
        private static PortableExecutableReference UsingSystemText() => MetadataReference.CreateFromFile(typeof(Regex).Assembly.Location);
        private static PortableExecutableReference UsingEgregore() => MetadataReference.CreateFromFile(typeof(DynamicControllerFeatureProvider).Assembly.Location);
    }
}