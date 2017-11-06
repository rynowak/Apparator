using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc.Razor.Extensions;
using Microsoft.AspNetCore.Mvc.Razor.Internal;
using Microsoft.AspNetCore.Mvc.Razor.ViewCompilation.Internal;
using Microsoft.AspNetCore.Razor.Language;
using Microsoft.Build.Framework;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Emit;
using Microsoft.CodeAnalysis.Text;
using Microsoft.Extensions.FileProviders;
using MSBuildTask = Microsoft.Build.Utilities.Task;

namespace Apparator.Razor.Tasks
{
    public class Rzc : MSBuildTask
    {
        public string KeyContainer { get; set; }

        public string KeyFile { get; set; }
        
        [Required]
        public ITaskItem[] References { get; set; }

        [Required]
        public string OutputAssembly { get; set; }

        [Required]
        public string ProjectRoot { get; set; }

        [Required]
        public ITaskItem[] Sources { get; set; }

        public bool EmbedSources { get; set; }

        public string DebugType { get; set; }

        public override bool Execute()
        {
            foreach (var reference in References)
            {
                Log.LogMessage(MessageImportance.High, "reference assembly: {0}", reference.ItemSpec);
            }

            foreach (var source in Sources)
            {
                Log.LogMessage(MessageImportance.High, ".cshtml source: {0}", source.ItemSpec);
            }

            Log.LogMessage(MessageImportance.High, "output assembly: {0}", OutputAssembly);
            Log.LogMessage(MessageImportance.High, "project root: {0}", ProjectRoot);

            var engine = RazorEngine.Create(b =>
            {
                RazorExtensions.Register(b);

                // Roslyn + TagHelpers infrastructure
                var metadataReferenceFeature = new LazyMetadataReferenceFeature(References.Select(r => r.GetMetadata("FullPath")));
                b.Features.Add(metadataReferenceFeature);
                b.Features.Add(new Microsoft.CodeAnalysis.Razor.CompilationTagHelperFeature());

                // TagHelperDescriptorProviders (actually do tag helper discovery)
                b.Features.Add(new Microsoft.CodeAnalysis.Razor.DefaultTagHelperDescriptorProvider());
                b.Features.Add(new ViewComponentTagHelperDescriptorProvider());
            });

            var project = new FileProviderRazorProject(new PhysicalFileProvider(ProjectRoot));
            var templateEngine = new MvcRazorTemplateEngine(engine, project);

            var results = GenerateCode(templateEngine);
            var success = true;

            foreach (var result in results)
            {
                if (result.CSharpDocument.Diagnostics.Count > 0)
                {
                    success = false;
                    foreach (var error in result.CSharpDocument.Diagnostics)
                    {
                        Log.LogError(
                            null,
                            error.Id,
                            null,
                            error.Span.FilePath,
                            error.Span.LineIndex,
                            error.Span.CharacterIndex,
                            -1,
                            -1,
                            error.GetMessage());
                    }
                }
            }

            if (!success)
            {
                return false;
            }
            
            var compilation = CreateCompilation(results, Path.GetFileNameWithoutExtension(OutputAssembly));
            var resources = GetResources(results);
            
            var emitResult = EmitAssembly(
                compilation,
                new EmitOptions(),
                OutputAssembly,
                resources);

            if (!emitResult.Success)
            {
                foreach (var diagnostic in emitResult.Diagnostics)
                {
                    Log.LogError(
                        diagnostic.Descriptor.Category,
                        diagnostic.Id,
                        diagnostic.Descriptor.HelpLinkUri,
                        diagnostic.Location.GetLineSpan().Path,
                        diagnostic.Location.GetMappedLineSpan().StartLinePosition.Line,
                        diagnostic.Location.GetMappedLineSpan().StartLinePosition.Character,
                        diagnostic.Location.GetMappedLineSpan().EndLinePosition.Line,
                        diagnostic.Location.GetMappedLineSpan().EndLinePosition.Character,
                        diagnostic.GetMessage());
                }

                return false;
            }

            return true;
        }

        private List<ViewFileInfo> GetRazorFiles()
        {
            var contentRoot = ProjectRoot;
            var viewFiles = Sources.Select(s => Path.Combine(ProjectRoot, s.ItemSpec)).ToArray();
            var viewFileInfo = new List<ViewFileInfo>(Sources.Length);
            var trimLength = contentRoot.EndsWith("/") ? contentRoot.Length - 1 : contentRoot.Length;

            for (var i = 0; i < viewFiles.Length; i++)
            {
                var fullPath = viewFiles[i];
                if (fullPath.StartsWith(contentRoot, StringComparison.OrdinalIgnoreCase))
                {
                    var viewEnginePath = fullPath.Substring(trimLength).Replace('\\', '/');
                    viewFileInfo.Add(new ViewFileInfo(fullPath, viewEnginePath));
                }
            }

            return viewFileInfo;
        }

        private ViewCompilationInfo[] GenerateCode(RazorTemplateEngine templateEngine)
        {
            var files = GetRazorFiles();
            var results = new ViewCompilationInfo[files.Count];
            Parallel.For(0, results.Length, new ParallelOptions() { MaxDegreeOfParallelism = 4 }, i =>
            {
                var fileInfo = files[i];
                ViewCompilationInfo compilationInfo;
                using (var fileStream = fileInfo.CreateReadStream())
                {
                    var csharpDocument = templateEngine.GenerateCode(fileInfo.ViewEnginePath);
                    compilationInfo = new ViewCompilationInfo(fileInfo, csharpDocument);
                }

                results[i] = compilationInfo;
            });

            return results;
        }

        private CSharpCompilation CreateCompilation(ViewCompilationInfo[] results, string assemblyname)
        {
            var options = new CSharpCompilationOptions(
                outputKind: OutputKind.DynamicallyLinkedLibrary,
                cryptoKeyContainer: KeyContainer,
                cryptoKeyFile: KeyFile);

            var compilation = CSharpCompilation.Create(
                assemblyName: assemblyname,
                references: References.Select(r => MetadataReference.CreateFromFile(r.ItemSpec)),
                options: options);
            
            var syntaxTrees = new SyntaxTree[results.Length];

            Parallel.For(0, results.Length, new ParallelOptions() { MaxDegreeOfParallelism = 4 }, i =>
            {
                var result = results[i];

                syntaxTrees[i] = CSharpSyntaxTree.ParseText(
                    text: SourceText.From(result.CSharpDocument.GeneratedCode, Encoding.UTF8),
                    options: new CSharpParseOptions(),
                    path: result.ViewFileInfo.FullPath ?? result.ViewFileInfo.ViewEnginePath);
            });

            compilation = compilation.AddSyntaxTrees(syntaxTrees);
            return compilation;
        }

        private ResourceDescription[] GetResources(ViewCompilationInfo[] results)
        {
            if (!EmbedSources)
            {
                return Array.Empty<ResourceDescription>();
            }

            var resources = new ResourceDescription[results.Length];
            for (var i = 0; i < results.Length; i++)
            {
                var fileInfo = results[i].ViewFileInfo;

                resources[i] = new ResourceDescription(
                    fileInfo.ViewEnginePath,
                    fileInfo.CreateReadStream,
                    isPublic: true);
            }

            return resources;
        }

        public EmitResult EmitAssembly(
            CSharpCompilation compilation,
            EmitOptions emitOptions,
            string assemblyPath,
            ResourceDescription[] resources)
        {
            EmitResult emitResult;
            using (var assemblyStream = new MemoryStream())
            {
                using (var pdbStream = new MemoryStream())
                {
                    emitResult = compilation.Emit(
                        assemblyStream,
                        pdbStream,
                        manifestResources: resources,
                        options: emitOptions);

                    if (emitResult.Success)
                    {
                        Directory.CreateDirectory(Path.GetDirectoryName(assemblyPath));
                        var pdbPath = Path.ChangeExtension(assemblyPath, ".pdb");
                        assemblyStream.Position = 0;
                        pdbStream.Position = 0;

                        // Avoid writing to disk unless the compilation is successful.
                        using (var assemblyFileStream = File.OpenWrite(assemblyPath))
                        {
                            assemblyStream.CopyTo(assemblyFileStream);
                        }

                        using (var pdbFileStream = File.OpenWrite(pdbPath))
                        {
                            pdbStream.CopyTo(pdbFileStream);
                        }
                    }
                }
            }

            return emitResult;
        }
    }
}
