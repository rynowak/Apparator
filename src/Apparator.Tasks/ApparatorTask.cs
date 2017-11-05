using System;
using System.Collections.Generic;
using System.IO.Pipes;
using System.Linq;
using System.Runtime.Serialization.Formatters.Binary;
using System.Threading;
using Apparator.Messages;
using Microsoft.Build.Framework;
using MSBuildTask = Microsoft.Build.Utilities.Task;

namespace Apparator.Tasks
{
    internal class ApparatorTask : MSBuildTask, IGeneratedTask, ICancelableTask
    {
        private readonly MessageImportance DefaultLevel = MessageImportance.High;

        private readonly string _assemblyFile;
        private readonly string _assemblyName;
        private readonly string _taskName;
        private readonly string _typeName;
        private readonly string _hostId;

        private readonly CancellationTokenSource _cts;
        private readonly BinaryFormatter _formatter;
        private readonly Dictionary<TaskPropertyInfo, object> _properties;


        public ApparatorTask(string taskName, TaskPropertyInfo[] properties, string assemblyName, string assemblyFile, string typeName, string hostId)
        {
            _taskName = taskName;
            _assemblyName = assemblyName;
            _assemblyFile = assemblyFile;
            _typeName = typeName;
            _hostId = hostId;

            _cts = new CancellationTokenSource();
            _formatter = new BinaryFormatter()
            {
                Binder = new ApparatorSerializationBinder()
            };

            _properties = new Dictionary<TaskPropertyInfo, object>();

            for (var i = 0; i < properties.Length; i++)
            {
                var property = properties[i];

                var value = property.PropertyType.IsValueType ? Activator.CreateInstance(property.PropertyType) : null;
                _properties.Add(property, value);
            }
        }

        private IEnumerable<TaskPropertyInfo> InputProperties => _properties.Where(kvp => !kvp.Key.Output).Select(kvp => kvp.Key).ToArray();

        private IEnumerable<TaskPropertyInfo> OutputProperties => _properties.Where(kvp => kvp.Key.Output).Select(kvp => kvp.Key).ToArray();

        public void Cancel()
        {
            _cts.Cancel();
        }

        public override bool Execute()
        {
            try
            {
                using (var stream = Connect(_hostId))
                {
                    StartRemoteTask(stream);

                    var result = WaitForResult(stream);
                    foreach (var property in OutputProperties)
                    {
                        if (result.Outputs.TryGetValue(property.Name, out var value))
                        {
                            SetPropertyValue(property, SerializableTaskItem.Unwrap(value));
                        }
                    }

                    return result.Success;
                }
            }
            catch (OperationCanceledException)
            {
                Log.LogWarning("task cancelled");
                return false;
            }
        }

        private NamedPipeClientStream Connect(string hostId)
        {
            var pipeName = $"apparator.{hostId}";
            var stream = new NamedPipeClientStream(".", pipeName, PipeDirection.InOut, PipeOptions.None);

            while (true)
            {
                _cts.Token.ThrowIfCancellationRequested();

                try
                {
                    stream.Connect(10 * 3000);
                    return stream;
                }
                catch (TimeoutException)
                {
                    Log.LogWarning("waiting for apparator host with pipe name '{0}'", pipeName);
                }
            }
        }

        private BinaryFormatter CreateFormatter()
        {
            return new BinaryFormatter()
            {
                Binder = new ApparatorSerializationBinder(),
            };
        }

        private void StartRemoteTask(NamedPipeClientStream stream)
        {
            var request = new ExecuteTaskMessage()
            {
                Arguments = InputProperties.ToDictionary(p => p.Name, p => SerializableTaskItem.Wrap(GetPropertyValue(p))),
                AssemblyFile = _assemblyFile,
                AssemblyName = _assemblyName,
                OutputParameters =  OutputProperties.Select(p => p.Name).ToArray(),
                TaskName = _taskName,
                TypeName = _typeName,
            };

            _formatter.Serialize(stream, request);
        }

        private ExecuteTaskResultMessage WaitForResult(NamedPipeClientStream stream)
        {
            while (true)
            {
                _cts.Token.ThrowIfCancellationRequested();

                var message = _formatter.Deserialize(stream);
                if (message is ExecuteTaskResultMessage result)
                {
                    return result;
                }

                LogMessage(message);
            }
        }

        private void LogMessage(object obj)
        {
            switch (obj)
            {
                case BuildErrorEventArgsMessage error:
                    BuildEngine5.LogErrorEvent(error.ToEventArgs());
                    break;

                case BuildWarningEventArgsMessage warning:
                    BuildEngine5.LogWarningEvent(warning.ToEventArgs());
                    break;

                case BuildMessageEventArgsMessage message:
                    BuildEngine5.LogMessageEvent(message.ToEventArgs());
                    break;

                case TelemetryMessage telemetry:
                    BuildEngine5.LogTelemetry(telemetry.EventName, telemetry.Properties);
                    break;

                default:
                    Log.LogError("unexpected message type {0}", obj.GetType());
                    break;

            }
        }

        public object GetPropertyValue(TaskPropertyInfo property)
        {
            if (!_properties.TryGetValue(property, out var value))
            {
                throw new InvalidOperationException("That's not a real property. Try again.");
            }

            return value;
        }

        public void SetPropertyValue(TaskPropertyInfo property, object value)
        {
            if (!_properties.ContainsKey(property))
            {
                throw new InvalidOperationException("That's not a real property. Try again.");
            }

            _properties[property] = value;
        }
    }
}
