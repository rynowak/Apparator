
#if NETSTANDARD1_6
using System;
using System.IO;
using System.Reflection;
using System.Runtime.Loader;
using System.Threading;
using Microsoft.Build.Framework;
using Microsoft.Extensions.DependencyModel;
using MSBuildTask = Microsoft.Build.Utilities.Task;

namespace Apparator.Core
{
    public partial class Apparate
    {
        public override bool Execute()
        {
            if (Debug)
            {
                while (!System.Diagnostics.Debugger.IsAttached)
                {
                    Log.LogMessage(MessageImportance, "Go for it {0}", System.Diagnostics.Process.GetCurrentProcess().Id);
                    Log.LogMessage(MessageImportance, "Go for it {0}", System.Diagnostics.Process.GetCurrentProcess().Id);
                    Log.LogMessage(MessageImportance, "Go for it {0}", System.Diagnostics.Process.GetCurrentProcess().Id);
                    Log.LogMessage(MessageImportance, "Go for it {0}", System.Diagnostics.Process.GetCurrentProcess().Id);
                    Thread.Sleep(3 * 1000);
                }
            }

            var context = new PluginLoadContext(AssemblyLoadContext.Default, LoadDependencyContext());

            Assembly assembly;
            if (EntryAssemblyPath == null)
            {
                Log.LogMessage(MessageImportance, "Loading Entry Point Assembly by name {0}", EntryAssembly);
                assembly = context.LoadFromAssemblyName(new AssemblyName(EntryAssembly));
            }
            else
            {
                Log.LogMessage(MessageImportance, "Loading Entry Point Assembly by path {0}", EntryAssemblyPath);
                assembly = context.LoadFromAssemblyPath(EntryAssemblyPath);
            }

            Log.LogMessage(MessageImportance, "Loading Entry Point Type {0}", EntryPointType);
            var type = assembly.GetType(EntryPointType);

            Log.LogMessage(MessageImportance, "Finding Entry Point Method {0}", EntryPointMethod);
            var method = type.GetMethod(EntryPointMethod);

            Log.LogMessage(MessageImportance, "Calling Entry Point Method {0}", string.Join(Environment.NewLine, Args));
            method.Invoke(null, new object[] { Args, });

            return true;
        }

        private DependencyContext LoadDependencyContext()
        {
            Log.LogMessage(MessageImportance, "Reading dependency context from {0}", DepsFile);
            using (var stream = File.OpenRead(DepsFile))
            {
                var reader = new DependencyContextJsonReader();
                return reader.Read(stream);
            }
        }

        private class PluginLoadContext : AssemblyLoadContext
        {
            private readonly DependencyContext _dependencyContext;
            private readonly AssemblyLoadContext _parent;

            public PluginLoadContext(AssemblyLoadContext parent, DependencyContext dependencyContext)
            {
                _parent = parent;
                _dependencyContext = dependencyContext;
            }

            protected override Assembly Load(AssemblyName assemblyName)
            {
                for (var i = 0; i < _dependencyContext.RuntimeLibraries.Count; i++)
                {
                    var library = _dependencyContext.RuntimeLibraries[i];
                    if (library.Name == assemblyName.Name)
                    {
                        for (var j = 0; j < library.RuntimeAssemblyGroups.Count; j++)
                        {
                            var group = library.RuntimeAssemblyGroups[j];
                            for (var k = 0; k < group.AssetPaths.Count; k++)
                            {
                                var asset = group.AssetPaths[k];
                                if (asset.EndsWith(".dll"))
                                {
                                    var assembly = LoadFromAssemblyPath(asset);
                                    if (assembly != null)
                                    {
                                        return assembly;
                                    }
                                }
                            }
                        }
                    }
                }

                return _parent.LoadFromAssemblyName(assemblyName);
            }
        }
    }
}
#endif
