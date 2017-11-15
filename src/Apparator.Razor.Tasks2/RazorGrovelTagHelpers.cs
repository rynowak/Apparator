// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.IO;
using System.Text;
using Microsoft.Build.Framework;

namespace Apparator.Razor.Tasks2
{
    public class RazorGrovelTagHelpers : DotnetToolTask
    {
        [Required]
        public string[] Assemblies { get; set; }

        [Required]
        [Output]
        public string OutputPath { get; set; }

        protected override bool SkipTaskExecution()
        {
            if (Assemblies.Length == 0)
            {
                File.WriteAllText(OutputPath, "{ }");
                return true;
            }

            return false;
        }

        protected override string GenerateResponseFileCommands()
        {
            var builder = new StringBuilder();
            for (var i = 0; i < Assemblies.Length; i++)
            {
                builder.AppendLine(Assemblies[i]);
            }

            builder.AppendLine("-o");
            builder.AppendLine(OutputPath);

            return builder.ToString();
        }
    }
}
