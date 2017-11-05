using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO.Pipes;
using System.Linq;
using System.Runtime.Serialization.Formatters.Binary;
using Apparator.Messages;
using Microsoft.Build.Framework;
using MSBuildTask = Microsoft.Build.Utilities.Task;

namespace Apparator.Tasks
{
    internal class ApparatorTask : MSBuildTask, IGeneratedTask
    {
        private readonly MessageImportance DefaultLevel = MessageImportance.High;

        private readonly string _assemblyFile;
        private readonly string _assemblyName;
        private readonly string _taskName;
        private readonly string _typeName;
        private readonly string _hostId;

        private readonly Dictionary<TaskPropertyInfo, object> _properties;

        public ApparatorTask(string taskName, TaskPropertyInfo[] properties, string assemblyName, string assemblyFile, string typeName, string hostId)
        {
            _taskName = taskName;
            _assemblyName = assemblyName;
            _assemblyFile = assemblyFile;
            _typeName = typeName;
            _hostId = hostId;
            
            _properties = new Dictionary<TaskPropertyInfo, object>();

            for (var i = 0; i < properties.Length; i++)
            {
                var property = properties[i];

                var value = property.PropertyType.IsValueType ? Activator.CreateInstance(property.PropertyType) : null;
                _properties.Add(property, value);
            }
        }

        public override bool Execute()
        {
            Log.LogMessage(DefaultLevel, "Connecting to host: {0}", _hostId);
            var stream = new NamedPipeClientStream(".", $"apparator.{_hostId}", PipeDirection.InOut, PipeOptions.None);
            stream.Connect(10 * 1000);

            Log.LogMessage(DefaultLevel, "Sending message");
            var formatter = new BinaryFormatter()
            {
                Binder = new ApparatorSerializationBinder(),
            };

            var request = new ExecuteTaskMessage()
            {
                Arguments = _properties.Where(kvp => !kvp.Key.Output).ToDictionary(kvp => kvp.Key.Name, kvp => kvp.Value),
                AssemblyFile = _assemblyFile,
                AssemblyName = _assemblyName,
                OutputParameters = _properties.Where(kvp => kvp.Key.Output).Select(kvp => kvp.Key.Name).ToArray(),
                TaskName = _taskName,
                TypeName = _typeName,
            };
            
            formatter.Serialize(stream, request);

            Log.LogMessage(DefaultLevel, "Waiting for reply");

            while (true)
            {
                var message = formatter.Deserialize(stream);
                if (message is ExecuteTaskResultMessage result)
                {
                    Log.LogMessage(DefaultLevel, "Got reply: Success={0}", result.Success);

                    foreach (var property in _properties.Where(kvp => kvp.Key.Output).Select(kvp => kvp.Key).ToArray())
                    {
                        if (result.Outputs.TryGetValue(property.Name, out var value))
                        {
                            SetPropertyValue(property, value);
                        }
                    }

                    return result.Success;
                }
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
