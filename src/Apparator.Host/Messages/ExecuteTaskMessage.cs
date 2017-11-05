
using System;
using System.Collections.Generic;

namespace Apparator.Messages
{
    [Serializable]
    internal class ExecuteTaskMessage
    {
        public string AssemblyName { get; set; }

        public string AssemblyFile { get; set; }

        public string TaskName { get; set; }

        public string TypeName { get; set; }

        public Dictionary<string, object> Arguments { get; set; }

        public string[] OutputParameters { get; set; }
    }
}
