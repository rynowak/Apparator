using System;
using System.Collections.Generic;

namespace Apparator.Messages
{
    [Serializable]
    internal class TelemetryMessage
    {
        public string EventName { get; set; }

        public IDictionary<string, string> Properties { get; set; }
    }
}
