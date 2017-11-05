using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters.Binary;
using System.Threading;
using System.Threading.Tasks;
using Apparator.Messages;
using Microsoft.Build.Framework;

namespace Apparator.Host
{
    public static class Program
    {
        private static NamedPipeServer _server;
        private static object _lock = new object();

        public static async Task<int> Main(string[] args)
        {
            if (args.Length < 1)
            {
                Console.WriteLine("Usage dotnet Apparator.Host <name>");
                return 0;
            }

            Console.WriteLine($"listening on apparator.{args[0]}");
            _server = new NamedPipeServer($"apparator.{args[0]}", OnConnected);
            _server.Start();
            Console.CancelKeyPress += Console_CancelKeyPress;

            await WhenCanceled(_server.Stopping);

            lock (_lock)
            {
                return 0;
            }
        }

        private static Task OnConnected(Connection connection)
        {
            var stream = connection.Stream;
            var formatter = new BinaryFormatter()
            {
                Binder = new ApparatorSerializationBinder(),
            };

            while (!connection.CancellationToken.IsCancellationRequested)
            {
                ExecuteTaskMessage message;

                try
                {
                    Console.WriteLine("Waiting for message");
                    message = (ExecuteTaskMessage)formatter.Deserialize(stream);
                }
                catch (SerializationException)
                {
                    Console.WriteLine("Client disconnected");
                    return Task.CompletedTask;
                }

                var assembly = Assembly.LoadFrom(message.AssemblyFile);
                var type = assembly.GetType(message.TypeName, throwOnError: true);

                var task = (ITask)Activator.CreateInstance(type);
                task.BuildEngine = new ApparatorBuildEngine();

                foreach (var argument in message.Arguments)
                {
                    type.GetProperty(argument.Key).SetValue(task, argument.Value);
                }

                var success = task.Execute();

                var result = new ExecuteTaskResultMessage()
                {
                    Success = success,
                    Outputs = new Dictionary<string, object>(),
                };

                foreach (var output in message.OutputParameters)
                {
                    result.Outputs[output] = type.GetProperty(output).GetValue(task);
                }

                try
                {
                    Console.WriteLine("Sending reply");
                    formatter.Serialize(stream, result);
                }
                catch (SerializationException)
                {
                    Console.WriteLine("Client disconnected");
                    return Task.CompletedTask;
                }
            }

            return Task.CompletedTask;
        }

        private static void Console_CancelKeyPress(object sender, ConsoleCancelEventArgs e)
        {
            lock (_lock)
            {
                _server.Stop();
            }
        }

        private static Task WhenCanceled(CancellationToken cancellationToken)
        {
            var tcs = new TaskCompletionSource<bool>();
            cancellationToken.Register(s => ((TaskCompletionSource<bool>)s).SetResult(true), tcs);
            return tcs.Task;
        }
    }
}
