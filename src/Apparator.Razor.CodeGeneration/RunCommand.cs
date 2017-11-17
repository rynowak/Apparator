// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection.Metadata;
using System.Reflection.PortableExecutable;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc.Razor.Extensions;
using Microsoft.AspNetCore.Razor.Language;
using Microsoft.VisualStudio.LanguageServices.Razor;
using Newtonsoft.Json;

namespace Apparator.Razor.CodeGeneration
{
    internal class RunCommand
    {
        private CodeGeneratorApplication Application { get; set; }

        public void Configure(CodeGeneratorApplication application)
        {
            Application = application;
            application.OnExecute(() => Execute());
        }

        private int Execute()
        {
            if (!ValidateArguments())
            {
                Application.ShowHelp();
                return 1;
            }

            var tagHelpers = GetTagHelpers();
            
            var engine = RazorEngine.Create(b =>
            {
                RazorExtensions.Register(b);

                b.Features.Add(new Feature() { TagHelpers = tagHelpers,});
            });

            var templateEngine = new MvcRazorTemplateEngine(engine, RazorProject.Create(Application.ProjectRoot.Value()));

            var results = GenerateCode(templateEngine);
            var success = true;

            foreach (var result in results)
            {
                if (result.CSharpDocument.Diagnostics.Count > 0)
                {
                    success = false;
                    foreach (var error in result.CSharpDocument.Diagnostics)
                    {
                        Console.Error.WriteLine(error.GetMessage());
                    }
                }

                File.WriteAllText(
                    Path.Combine(Application.OutputDirectory.Value(), Path.ChangeExtension(result.ViewFileInfo.ViewEnginePath.Substring(1), ".cs")), 
                    result.CSharpDocument.GeneratedCode);
            }

            return success ? 0 : -1;
        }

        private IReadOnlyList<TagHelperDescriptor> GetTagHelpers()
        {
            using (var stream = File.OpenRead(Application.TagHelpers.Value()))
            {
                var reader = new JsonTextReader(new StreamReader(stream));

                var serializer = new JsonSerializer();
                serializer.Converters.Add(new RazorDiagnosticJsonConverter());
                serializer.Converters.Add(new TagHelperDescriptorJsonConverter());

                return serializer.Deserialize<IReadOnlyList<TagHelperDescriptor>>(reader);
            }
        }

        private List<ViewFileInfo> GetRazorFiles()
        {
            var contentRoot = Application.ProjectRoot.Value();
            var viewFiles = Application.Sources.Values.Select(s => Path.Combine(Application.ProjectRoot.Value(), s)).ToArray();
            var viewFileInfo = new List<ViewFileInfo>(Application.Sources.Values.Count);
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

        private bool ValidateArguments()
        {
            if (string.IsNullOrEmpty(Application.OutputDirectory.Value()))
            {
                Application.Error.WriteLine($"{Application.OutputDirectory.ValueName} not specified.");
                return false;
            }

            if (string.IsNullOrEmpty(Application.ProjectRoot.Value()))
            {
                Application.Error.WriteLine($"{Application.ProjectRoot.ValueName} not specified.");
                return false;
            }

            if (string.IsNullOrEmpty(Application.TagHelpers.Value()))
            {
                Application.Error.WriteLine($"{Application.TagHelpers.ValueName} not specified.");
                return false;
            }

            if (Application.Sources.Values.Count == 0)
            {
                Application.Error.WriteLine($"{Application.Sources.Name} should have at least one value.");
                return false;
            }

            return true;
        }

        private struct ViewCompilationInfo
        {
            public ViewCompilationInfo(
                ViewFileInfo viewFileInfo,
                RazorCSharpDocument cSharpDocument)
            {
                ViewFileInfo = viewFileInfo;
                CSharpDocument = cSharpDocument;
            }

            public ViewFileInfo ViewFileInfo { get; }

            public RazorCSharpDocument CSharpDocument { get; }
        }

        private struct ViewFileInfo
        {
            public ViewFileInfo(string fullPath, string viewEnginePath)
            {
                FullPath = fullPath;
                ViewEnginePath = viewEnginePath;
            }

            public string FullPath { get; }

            public string ViewEnginePath { get; }

            public Stream CreateReadStream()
            {
                // We are setting buffer size to 1 to prevent FileStream from allocating it's internal buffer
                // 0 causes constructor to throw
                var bufferSize = 1;
                return new FileStream(
                    FullPath,
                    FileMode.Open,
                    FileAccess.Read,
                    FileShare.ReadWrite,
                    bufferSize,
                    FileOptions.Asynchronous | FileOptions.SequentialScan);
            }
        }

        private class Feature : ITagHelperFeature
        {
            public RazorEngine Engine { get; set; }

            public IReadOnlyList<TagHelperDescriptor> TagHelpers { get; set; }

            public IReadOnlyList<TagHelperDescriptor> GetDescriptors() => TagHelpers;
        }
    }
}