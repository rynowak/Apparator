// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Diagnostics;
using System.Threading;
using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;
using Microsoft.Extensions.CommandLineUtils;

namespace Apparator.Razor.Tasks2
{
    public abstract class DotnetToolTask : ToolTask
    {
        public bool Debug { get; set; }

        public bool DebugTool { get; set; }

        [Required]
        public string ToolAssembly { get; set; }

        protected override string ToolName => "dotnet";

        protected override MessageImportance StandardOutputLoggingImportance => DebugTool ? MessageImportance.High : base.StandardOutputLoggingImportance;

        protected override string GenerateFullPathToTool() => DotNetMuxer.MuxerPathOrDefault();

        protected override string GenerateCommandLineCommands()
        {
            return
                $"exec \"{ToolAssembly}\"" +
                (DebugTool ? " --debug" : "");
        }

        protected override string GetResponseFileSwitch(string responseFilePath)
        {
            return "@\"" + responseFilePath + "\"";
        }

        protected abstract override string GenerateResponseFileCommands();

        public override bool Execute()
        {
            if (Debug)
            {
                while (!Debugger.IsAttached)
                {
                    Log.LogMessage(MessageImportance.High, "Waiting for debugger in pid: {0}", Process.GetCurrentProcess().Id);
                    Thread.Sleep(TimeSpan.FromSeconds(3));
                }
            }

            return base.Execute();
        }

        protected override void LogToolCommand(string message)
        {
            if (Debug)
            {
                Log.LogMessage(MessageImportance.High, message);
            }
            else
            {
                base.LogToolCommand(message);
            }
        }
    }
}
