using System;
using Microsoft.Build.Framework;

namespace Apparator.Messages
{

    [Serializable]
    internal class BuildErrorEventArgsMessage : BuildEventArgsMessage
    {
        public BuildErrorEventArgsMessage()
        {
        }

        public BuildErrorEventArgsMessage(BuildErrorEventArgs other) 
            : base(other)
        {
            Subcategory = other.Subcategory;
            Code = other.Code;
            File = other.File;
            ProjectFile = other.ProjectFile;
            LineNumber = other.LineNumber;
            ColumnNumber = other.ColumnNumber;
            EndLineNumber = other.EndLineNumber;
            EndColumnNumber = other.EndColumnNumber;
        }

        public string Subcategory { get; set; }

        public string Code { get; set; }

        public string File { get; set; }

        public string ProjectFile { get; set; }

        public int LineNumber { get; set; }

        public int ColumnNumber { get; set; }

        public int EndLineNumber { get; set; }

        public int EndColumnNumber { get; set; }

        public BuildErrorEventArgs ToEventArgs()
        {
            return new BuildErrorEventArgs(Subcategory, Code, File, LineNumber, ColumnNumber, EndLineNumber, EndColumnNumber, Message, HelpKeyword, SenderName, Timestamp)
            {
                BuildEventContext = BuildEventContext?.ToEventArgs(),
                ProjectFile = ProjectFile,
            };
        }
    }
}
