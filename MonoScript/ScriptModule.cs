using System;
using System.Reflection;

namespace MonoScript
{
    public class ScriptModule
    {
        public Assembly Assembly { get; internal set; }
        
        public int ErrorCount { get; internal set; }
        
        public int WarningCount { get; internal set; }
        
        public Type[] GetTypes() => Assembly?.GetTypes();

        internal ScriptModule()
        {
        }
    }
}
