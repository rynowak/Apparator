using System;
using Microsoft.Build.Framework;

namespace Apparator.Messages
{

    [Serializable]
    internal class BuildMessageEventArgsMessage : BuildEventArgsMessage
    {
        public BuildMessageEventArgsMessage()
        {
        }

        public BuildMessageEventArgsMessage(BuildMessageEventArgs other) 
            : base(other)
        {
            Importance = other.Importance;
            Subcategory = other.Subcategory;
            Code = other.Code;
            File = other.File;
            ProjectFile = other.ProjectFile;
            LineNumber = other.LineNumber;
            ColumnNumber = other.ColumnNumber;
            EndLineNumber = other.EndLineNumber;
            EndColumnNumber = other.EndColumnNumber;
        }

        public MessageImportance Importance { get; set; }

        public string Subcategory { get; set; }

        public string Code { get; set; }

        public string File { get; set; }

        public string ProjectFile { get; set; }

        public int LineNumber { get; set; }

        public int ColumnNumber { get; set; }

        public int EndLineNumber { get; set; }

        public int EndColumnNumber { get; set; }

        public BuildMessageEventArgs ToEventArgs()
        {
            return new BuildMessageEventArgs(Subcategory, Code, File, LineNumber, ColumnNumber, EndLineNumber, EndColumnNumber, Message, HelpKeyword, SenderName, Importance, Timestamp)
            {
                BuildEventContext = BuildEventContext?.ToEventArgs(),
                ProjectFile = ProjectFile,
            };
        }
    }
}
