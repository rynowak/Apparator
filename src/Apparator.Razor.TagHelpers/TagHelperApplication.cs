// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using Microsoft.Extensions.CommandLineUtils;

namespace Apparator.Razor.TagHelpers
{
    internal class TagHelperApplication : CommandLineApplication
    {
        public TagHelperApplication()
        {
            Name = "razor-taghelpers";
            FullName = "Microsoft ASP.NET Core Razor Tag Helper tool";
            Description = "Discovers Tag Helpers in an assembly.";
            ShortVersionGetter = GetInformationalVersion;

            HelpOption("-?|-h|--help");

            OutputDirectory = Option("-o", "output directory", CommandOptionType.SingleValue);
            Assemblies = Argument("assemblies", "assemblies to search for tag helpers", multipleValues: true);

            new RunCommand().Configure(this);
        }

        public CommandArgument Assemblies { get; }

        public CommandOption OutputDirectory { get; }

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
            var assembly = typeof(TagHelperApplication).GetTypeInfo().Assembly;
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