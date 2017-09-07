using System;
using System.Reflection;

namespace MonoScript
{
    public class ScriptModule
    {
        public readonly Assembly Assembly;
        
        public Type[] GetTypes() => Assembly.GetTypes();

        internal ScriptModule(Assembly assembly)
        {
            Assembly = assembly;
        }
    }
}
