// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.IO;
using System.Reflection.Metadata;
using System.Reflection.PortableExecutable;

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
                Console.WriteLine($"Reading {assembly}");

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

            Console.WriteLine($"Writing {Path.GetFullPath(Application.OutputDirectory.Value())}");
            File.WriteAllText(Application.OutputDirectory.Value(), "{ }");

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