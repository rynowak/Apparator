// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.IO;
using System.Reflection.Metadata;
using System.Reflection.PortableExecutable;
using Newtonsoft.Json;

namespace Apparator.Razor.TagHelpers
{
    internal class RunCommand
    {
        private TagHelperApplication Application { get; set; }

        public void Configure(TagHelperApplication application)
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

            foreach (var assembly in Application.Assemblies.Values)
            {
                using (var stream = File.OpenRead(assembly))
                {
                    using (var peReader = new PEReader(stream, PEStreamOptions.LeaveOpen))
                    {
                        var reader = peReader.GetMetadataReader();

                        foreach (var typeHandle in reader.TypeDefinitions)
                        {
                            var type = reader.GetTypeDefinition(typeHandle);
                            foreach (var interfaceImplementationHandle in type.GetInterfaceImplementations())
                            {
                                var interfaceImplementation = reader.GetInterfaceImplementation(interfaceImplementationHandle);

                                var interfaceHandle = interfaceImplementation.Interface;
                                if (interfaceHandle.Kind == HandleKind.TypeReference)
                                {
                                    var i = reader.GetTypeReference((TypeReferenceHandle)interfaceHandle);
                                }
                            }
                        }
                    }
                }
            }

            var file = Path.GetFullPath(Application.OutputDirectory.Value());
            if (File.Exists(file))
            {
                // Already there
                return 0;
            }

            var input = Path.Combine(Path.GetDirectoryName(typeof(RunCommand).Assembly.Location), "defaultTagHelpers.json");
            Console.WriteLine($"Writing Tag Helpers to: {file}");
            File.WriteAllText(file, File.ReadAllText(input));

            return 0;
        }
        private bool ValidateArguments()
        {
            if (string.IsNullOrEmpty(Application.OutputDirectory.Value()))
            {
                Application.Error.WriteLine($"{Application.OutputDirectory.ValueName} not specified.");
                return false;
            }

            if (Application.Assemblies.Values.Count == 0)
            {
                Application.Error.WriteLine($"{Application.Assemblies.Name} should have at least one value.");
                return false;
            }

            return true;
        }
    }
}