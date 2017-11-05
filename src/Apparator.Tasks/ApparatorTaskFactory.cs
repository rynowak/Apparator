using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Xml.Linq;
using Microsoft.Build.Framework;

namespace Apparator.Tasks
{
    public class ApparatorTaskFactory : ITaskFactory2
    {
        private string _assemblyFile;
        private string _assemblyName;
        private string _typeName;
        private string _taskName;
        private string _hostId;
        private TaskPropertyInfo[] _parameters;

        public string FactoryName => GetType().FullName;

        public Type TaskType => typeof(ApparatorTask);

        public void CleanupTask(ITask task)
        {
        }

        public ITask CreateTask(IBuildEngine taskFactoryLoggingHost, IDictionary<string, string> taskIdentityParameters)
        {
            // We don't use taskIdentityParameters
            return CreateTask(taskFactoryLoggingHost);
        }

        public ITask CreateTask(IBuildEngine taskFactoryLoggingHost)
        {
            return new ApparatorTask(_taskName, _parameters, _assemblyName, _assemblyFile, _typeName, _hostId);
        }

        public TaskPropertyInfo[] GetTaskParameters()
        {
            return _parameters;
        }

        public bool Initialize(
            string taskName,
            IDictionary<string, string> factoryIdentityParameters,
            IDictionary<string, TaskPropertyInfo> parameterGroup,
            string taskBody,
            IBuildEngine taskFactoryLoggingHost)
        {
            // We don't use factoryIdentityParameters
            return Initialize(taskName, parameterGroup, taskBody, taskFactoryLoggingHost);
        }

        public bool Initialize(
            string taskName,
            IDictionary<string, TaskPropertyInfo> parameterGroup,
            string taskBody,
            IBuildEngine taskFactoryLoggingHost)
        {
            _taskName = taskName;

            _parameters = parameterGroup.Values.ToArray();

            var document = XDocument.Parse(taskBody);

            _assemblyFile = document.Root.Element(XName.Get("AssemblyFile"))?.Value;
            _assemblyName = document.Root.Element(XName.Get("AssemblyName"))?.Value;
            _typeName = document.Root.Element(XName.Get("TypeName"))?.Value;
            _hostId = document.Root.Element(XName.Get("HostId"))?.Value;

            return true;
        }
    }
}
