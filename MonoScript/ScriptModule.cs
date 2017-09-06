using System;
using System.Reflection;

namespace MonoScript
{
    public class ScriptModule
    {
        private readonly Assembly _assembly;
        
        public Type[] GetTypes() => _assembly.GetTypes();

        internal ScriptModule(Assembly assembly)
        {
            _assembly = assembly;
        }
    }
}
