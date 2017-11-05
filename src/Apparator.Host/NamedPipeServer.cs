using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Pipes;
using System.Threading;
using System.Threading.Tasks;

namespace Apparator.Host
{
    internal class NamedPipeServer
    {
        private readonly CancellationTokenSource _cts;
        private readonly List<Connection> _connections;
        private readonly object _lock;
        private readonly Func<Connection, Task> _onConnected;

        private Task _listener;

        public NamedPipeServer(string pipeName, Func<Connection, Task> onConnected)
        {
            PipeName = pipeName;
            _onConnected = onConnected;

            _cts = new CancellationTokenSource();
            _connections = new List<Connection>();
            _lock = new object();
        }

        public string PipeName { get; }

        public CancellationToken Stopping => _cts.Token;

        public void Start()
        {
            _listener = Listen();
        }

        public void Stop()
        {
            _cts.Cancel();

            lock (_lock)
            {
                for (var i = 0; i < _connections.Count; i++)
                {
                    var connection = _connections[i];
                    connection.Close();
                }
            }
        }

        private async Task Listen()
        {
            while (!_cts.IsCancellationRequested)
            {
                try
                {
                    Console.WriteLine("ready for client");
                    var pipe = new NamedPipeServerStream(PipeName, PipeDirection.InOut, -1, PipeTransmissionMode.Byte, PipeOptions.Asynchronous);
                    await pipe.WaitForConnectionAsync(_cts.Token);

                    var connection = new Connection(pipe, _cts.Token);
                    lock (_lock)
                    {
                        _connections.Add(connection);
                    }

                    GC.KeepAlive(Task.Run(() => OnConnected(connection)));
                }
                catch (OperationCanceledException)
                {
                    return;
                }
                catch (Exception ex)
                {
                    Console.WriteLine("Exception in Listen()");
                    Console.WriteLine();
                    Console.WriteLine(ex);
                    Console.WriteLine();
                    Console.WriteLine();
                }
            }
        }

        private async void OnConnected(Connection connection)
        {
            try
            {
                await _onConnected(connection);
            }

            // All of these mean that the client disconnected, or we are shutting down
            catch (OperationCanceledException)
            {
            }
            catch (ObjectDisposedException)
            {
            }
            catch (InvalidOperationException)
            {
            }
            catch (IOException)
            {
            }
            
            // This means it was an actual error
            catch (Exception ex)
            {
                Console.WriteLine("Exception in OnConnected()");
                Console.WriteLine();
                Console.WriteLine(ex);
                Console.WriteLine();
                Console.WriteLine();
            }

            lock (_lock)
            {
                _connections.Remove(connection);
            }
        }
    }
}
