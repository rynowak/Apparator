using System;
using Microsoft.Build.Framework;

namespace Apparator.Messages
{

    [Serializable]
    internal class BuildWarningEventArgsMessage : BuildEventArgsMessage
    {
        public BuildWarningEventArgsMessage()
        {
        }

        public BuildWarningEventArgsMessage(BuildWarningEventArgs other) 
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

        public BuildWarningEventArgs ToEventArgs()
        {
            return new BuildWarningEventArgs(Subcategory, Code, File, LineNumber, ColumnNumber, EndLineNumber, EndColumnNumber, Message, HelpKeyword, SenderName, Timestamp)
            {
                BuildEventContext = BuildEventContext?.ToEventArgs(),
                ProjectFile = ProjectFile,
            };
        }
    }
}
