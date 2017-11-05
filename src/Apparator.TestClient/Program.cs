using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Pipes;
using System.Reflection;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters.Binary;
using System.Threading;
using System.Threading.Tasks;
using Apparator.Messages;
using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;

namespace Apparator.TestClient
{
    internal static class Program
    {
        private static readonly CancellationTokenSource _cts = new CancellationTokenSource();

        public static async Task<int> Main(string[] args)
        {
            Console.CancelKeyPress += Console_CancelKeyPress;

            try
            {
                Console.WriteLine($"Connecting to: apparator.{args[0]}");
                var stream = new NamedPipeClientStream(".", $"apparator.{args[0]}", PipeDirection.InOut, PipeOptions.Asynchronous);
                await stream.ConnectAsync(_cts.Token);
                Console.WriteLine("Connected. Ctrl+C to quit");

                var formatter = new BinaryFormatter()
                {
                    Binder = new ApparatorSerializationBinder(),
                };

                while (!_cts.Token.IsCancellationRequested)
                {
                    Console.WriteLine("Enter your message:");
                    var message = Console.ReadLine();
                    if (message == null)
                    {
                        // Happens when user does Ctrl+C
                        continue;
                    }

                    Console.WriteLine("Sending message");
                    var envelope = new ExecuteTaskMessage()
                    {
                        AssemblyFile = Path.Combine(Path.GetDirectoryName(Assembly.GetEntryAssembly().Location), "Apparator.Tasks.dll"),
                        TaskName = "SayHello",
                        TypeName = "Apparator.Tasks.SayHello",
                        Arguments = new Dictionary<string, object>()
                        {
                            { "Message", message },
                            {
                                "Individuals",
                                new SerializableTaskItem[]
                                {
                                    new SerializableTaskItem(new TaskItem("Ryan Nowak")),
                                    new SerializableTaskItem(new TaskItem("Pranav")),
                                }
                            }

                        },
                        OutputParameters = new string[]
                        {
                            "Responses",
                        },
                    };

                    formatter.Serialize(stream, envelope);

                    Console.WriteLine("Reading reply");

                    while (!_cts.Token.IsCancellationRequested)
                    {
                        var reply = formatter.Deserialize(stream);
                        if (reply is ExecuteTaskResultMessage result)
                        {
                            Console.WriteLine("Got reply: " + SerializableTaskItem.Unwrap(result.Outputs["Responses"]));
                            break;
                        }
                        else
                        {
                            Console.WriteLine("Got message: " + reply);
                        }
                    }
                }
            }
            catch (OperationCanceledException)
            {
            }

            return 0;
        }

        private static void Console_CancelKeyPress(object sender, ConsoleCancelEventArgs e)
        {
            _cts.Cancel();
        }
    }
}
