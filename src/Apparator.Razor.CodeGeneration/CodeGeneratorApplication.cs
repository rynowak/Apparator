// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using Microsoft.Extensions.CommandLineUtils;

namespace Apparator.Razor.CodeGeneration
{
    internal class CodeGeneratorApplication : CommandLineApplication
    {
        public CodeGeneratorApplication()
        {
            Name = "razor-codegenerator";
            FullName = "Microsoft ASP.NET Core Razor Code Generation tool";
            Description = "Generates Razor Code.";
            ShortVersionGetter = GetInformationalVersion;

            HelpOption("-?|-h|--help");

            ProjectRoot = Option("-p", "project root directory", CommandOptionType.SingleValue);
            OutputDirectory = Option("-o", "output directory", CommandOptionType.SingleValue);
            Sources = Argument("sources", ".cshtml files to compile", multipleValues: true);

            new RunCommand().Configure(this);
        }

        public CommandArgument Sources { get; }

        public CommandOption OutputDirectory { get; }

        public CommandOption ProjectRoot { get; }

        public new int Execute(params string[] args)
        {
            try
            {
                return base.Execute(ExpandResponseFiles(args));
            }
            catch (AggregateException ex) when (ex.InnerException != null)
            {
                Error.WriteLine(ex.InnerException.Message);
                Error.WriteLine(ex.InnerException.StackTrace);
                return 1;
            }
            catch (Exception ex)
            {
                Error.WriteLine(ex.Message);
                Error.WriteLine(ex.StackTrace);
                return 1;
            }
        }

        private string GetInformationalVersion()
        {
            var assembly = typeof(CodeGeneratorApplication).GetTypeInfo().Assembly;
            var attribute = assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>();
            return attribute.InformationalVersion;
        }

        private static string[] ExpandResponseFiles(string[] args)
        {
            var expandedArgs = new List<string>();
            foreach (var arg in args)
            {
                if (!arg.StartsWith("@", StringComparison.Ordinal))
                {
                    expandedArgs.Add(arg);
                }
                else
                {
                    var fileName = arg.Substring(1);
                    expandedArgs.AddRange(File.ReadLines(fileName));
                }
            }

            return expandedArgs.ToArray();
        }
    }
}