using System.IO.Pipes;
using System.Threading;

namespace Apparator.Host
{
    internal class Connection
    {
        public Connection(NamedPipeServerStream stream, CancellationToken cancellationToken)
        {
            Stream = stream;
            CancellationToken = cancellationToken;
        }

        public CancellationToken CancellationToken { get; }

        public NamedPipeServerStream Stream { get; }

        public void Close()
        {
            Stream.WaitForPipeDrain();
            Stream.Close();
        }
    }
}
