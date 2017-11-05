
using System;
using System.Collections.Generic;

namespace Apparator.Messages
{
    [Serializable]
    internal class ExecuteTaskResultMessage
    {
        public Dictionary<string, object> Outputs { get; set; }

        public bool Success { get; set; }
    }
}
