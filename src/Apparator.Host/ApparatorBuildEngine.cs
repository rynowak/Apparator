using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.Serialization.Formatters.Binary;
using Apparator.Messages;
using Microsoft.Build.Framework;

namespace Apparator.Host
{
    internal class ApparatorBuildEngine : IBuildEngine5
    {
        private readonly Connection _connection;
        private readonly BinaryFormatter _formatter;

        public ApparatorBuildEngine(Connection connection, BinaryFormatter formatter)
        {
            _connection = connection;
            _formatter = formatter;
        }

        public bool IsRunningMultipleNodes => throw new NotImplementedException();

        public bool ContinueOnError => throw new NotImplementedException();

        public int LineNumberOfTaskNode => throw new NotImplementedException();

        public int ColumnNumberOfTaskNode => throw new NotImplementedException();

        public string ProjectFileOfTaskNode => throw new NotImplementedException();

        public bool BuildProjectFile(string projectFileName, string[] targetNames, IDictionary globalProperties, IDictionary targetOutputs, string toolsVersion)
        {
            throw new NotImplementedException();
        }

        public bool BuildProjectFile(string projectFileName, string[] targetNames, IDictionary globalProperties, IDictionary targetOutputs)
        {
            throw new NotImplementedException();
        }

        public BuildEngineResult BuildProjectFilesInParallel(string[] projectFileNames, string[] targetNames, IDictionary[] globalProperties, IList<string>[] removeGlobalProperties, string[] toolsVersion, bool returnTargetOutputs)
        {
            throw new NotImplementedException();
        }

        public bool BuildProjectFilesInParallel(string[] projectFileNames, string[] targetNames, IDictionary[] globalProperties, IDictionary[] targetOutputsPerProject, string[] toolsVersion, bool useResultsCache, bool unloadProjectsOnCompletion)
        {
            throw new NotImplementedException();
        }

        public object GetRegisteredTaskObject(object key, RegisteredTaskObjectLifetime lifetime)
        {
            throw new NotImplementedException();
        }

        public void LogCustomEvent(CustomBuildEventArgs e)
        {
            throw new NotSupportedException();
        }

        public void LogErrorEvent(BuildErrorEventArgs e)
        {
            _formatter.Serialize(_connection.Stream, new BuildErrorEventArgsMessage(e));
        }

        public void LogMessageEvent(BuildMessageEventArgs e)
        {
            _formatter.Serialize(_connection.Stream, new BuildMessageEventArgsMessage(e));
        }

        public void LogTelemetry(string eventName, IDictionary<string, string> properties)
        {
            _formatter.Serialize(_connection.Stream, new TelemetryMessage()
            {
                EventName = eventName,
                Properties = properties,
            });
        }

        public void LogWarningEvent(BuildWarningEventArgs e)
        {
            _formatter.Serialize(_connection.Stream, new BuildWarningEventArgsMessage(e));
        }

        public void Reacquire()
        {
            throw new NotImplementedException();
        }

        public void RegisterTaskObject(object key, object obj, RegisteredTaskObjectLifetime lifetime, bool allowEarlyCollection)
        {
            throw new NotImplementedException();
        }

        public object UnregisterTaskObject(object key, RegisteredTaskObjectLifetime lifetime)
        {
            throw new NotImplementedException();
        }

        public void Yield()
        {
            throw new NotImplementedException();
        }
    }
}
