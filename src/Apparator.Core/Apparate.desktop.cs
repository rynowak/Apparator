#if NET462

using System;
using Microsoft.Build.Framework;
using MSBuildTask = Microsoft.Build.Utilities.Task;

namespace Apparator.Core
{
    public partial class Apparate
    {
        public override bool Execute()
        {
            throw new NotImplementedException("TODO Net46 implementation with appdomains. LOL");
        }
    }
}
#endif