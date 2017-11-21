using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using Microsoft.AspNetCore.Razor.Language;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Razor;
using Microsoft.VisualStudio.LanguageServices.Razor;
using Newtonsoft.Json;

namespace Apparator.Razor.TagHelpers.Analyzer
{
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public class TagHelperDiscoveryAnalyzer : DiagnosticAnalyzer
    {
        #region LOL

        public const string DiagnosticId = "ApparatorRazorTagHelpersAnalyzer";

        private static readonly LocalizableString Title = "TagHelpers";
        private static readonly LocalizableString MessageFormat = "TagHelpers";
        private static readonly LocalizableString Description = "TagHelpers";
        private const string Category = "Naming";

        private static DiagnosticDescriptor Rule = new DiagnosticDescriptor(DiagnosticId, Title, MessageFormat, Category, DiagnosticSeverity.Warning, isEnabledByDefault: true, description: Description);

        #endregion

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get { return ImmutableArray.Create(Rule); } }

        public override void Initialize(AnalysisContext context)
        {
            Console.WriteLine("Analyzer Initialized");
            context.RegisterCompilationAction(AnalyzeCompilation);
        }

        private void AnalyzeCompilation(CompilationAnalysisContext context)
        {
            Console.WriteLine("Analyzer Running");

            var manifest = context.Options.AdditionalFiles.FirstOrDefault(f => f.Path?.EndsWith(".TagHelperAnalyzer.txt") == true);
            if (manifest == null)
            {
                return;
            }

            var outputPath = manifest.GetText().ToString();

            var compilation = context.Compilation;

            var types = new List<INamedTypeSymbol>();
            var visitor = TagHelperTypeVisitor.Create(compilation, types);

            var results = new List<TagHelperDescriptor>();

            // We always visit the global namespace.
            visitor.Visit(compilation.Assembly.GlobalNamespace);

            foreach (var reference in compilation.References)
            {
                if (compilation.GetAssemblyOrModuleSymbol(reference) is IAssemblySymbol assembly)
                {
                    if (IsTagHelperAssembly(assembly))
                    {
                        visitor.Visit(assembly.GlobalNamespace);
                    }
                }
            }

            var factory = new DefaultTagHelperDescriptorFactory(compilation, designTime: false);
            for (var i = 0; i < types.Count; i++)
            {
                var descriptor = factory.CreateDescriptor(types[i]);

                if (descriptor != null)
                {
                    results.Add(descriptor);
                }
            }

            using (var stream = new MemoryStream())
            {
                Serialize(stream, results);

                stream.Position = 0L;

                var newHash = Hash(stream);
                var existingHash = Hash(outputPath);

                if (!HashesEqual(newHash, existingHash))
                {
                    stream.Position = 0;
                    using (var output = File.OpenWrite(outputPath))
                    {
                        stream.CopyTo(output);
                    }
                }
            }
        }

        private bool IsTagHelperAssembly(IAssemblySymbol assembly)
        {
            return assembly.Name != null && !assembly.Name.StartsWith("System.", StringComparison.Ordinal);
        }

        private static byte[] Hash(string path)
        {
            if (!File.Exists(path))
            {
                return Array.Empty<byte>();
            }

            using (var stream = File.OpenRead(path))
            {
                return Hash(stream);
            }
        }

        private static byte[] Hash(Stream stream)
        {
            using (var sha = SHA256.Create())
            {
                sha.ComputeHash(stream);
                return sha.Hash;
            }
        }

        private bool HashesEqual(byte[] x, byte[] y)
        {
            if (x.Length != y.Length)
            {
                return false;
            }

            for (var i = 0; i < x.Length; i++)
            {
                if (x[i] != y[i])
                {
                    return false;
                }
            }

            return true;
        }
        
        private static void Serialize(Stream stream, IList<TagHelperDescriptor> tagHelpers)
        {
            using (var writer = new StreamWriter(stream))
            {
                var serializer = new JsonSerializer();
                serializer.Converters.Add(new TagHelperDescriptorJsonConverter());
                serializer.Converters.Add(new RazorDiagnosticJsonConverter());

                serializer.Serialize(writer, tagHelpers);
            }
        }
    }
}
