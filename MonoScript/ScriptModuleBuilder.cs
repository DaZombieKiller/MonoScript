using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Collections;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.Reflection.Emit;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace MonoScript
{
    public class ScriptModuleBuilder
    {
        private readonly CSharpCodeCompiler _compiler;

        private readonly List<string> _sources;

        private readonly CompilerParameters _options;

        public void AddFromFile(string fileName)
        {
            _sources.Add(File.ReadAllText(fileName));
        }

        public void AddSource(string code)
        {
            _sources.Add(code);
        }

        public void AddFromStream(Stream code)
        {
            using (var reader = new StreamReader(code))
            {
                _sources.Add(reader.ReadToEnd());
            }
        }

        public void ImportType<T>()
        {
            ImportType(typeof(T));
        }

        public void ImportType(Type type)
        {
            _compiler.ImportType(type);
        }

        public void ImportTypes(IEnumerable<Type> types)
        {
            _compiler.ImportTypes(types);
        }

        public void ClearImports()
        {
            _compiler.ClearImports();
            _options.ReferencedAssemblies.Clear();
        }

        public void ImportBuiltinTypes()
        {
            _compiler.ImportTypes(BuiltinTypes);
        }

        public void ImportNamespace(string name)
        {
            ImportTypes(AppDomain.CurrentDomain.GetAssemblies()
                .SelectMany(t => t.GetTypes())
                .Where(t => t.Namespace == name));
        }

        public void ImportAssembly(Assembly asm)
        {
            if (asm is AssemblyBuilder) ImportTypes(asm.GetTypes());
            else _options.ReferencedAssemblies.Add(asm.Location);
        }

        public ScriptModuleBuilder()
        {
            _compiler = new CSharpCodeCompiler();
            _sources = new List<string>();
            _options = new CompilerParameters
            {
                GenerateExecutable = false,
                GenerateInMemory = true,
            };
        }
        
        public ScriptModule Build()
        {
            var result = _compiler.CompileAssemblyFromSourceBatch(_options, _sources.ToArray());

            foreach (var error in result.Errors)
                Console.Error.WriteLine(error);

            _sources.Clear();
            ClearImports();
            
            return new ScriptModule(result.CompiledAssembly);
        }
        
        private static readonly Type[] BuiltinTypes =
        {
            typeof(object),
            typeof(ValueType),
            typeof(Attribute),
            
            typeof(InAttribute),
            typeof(OutAttribute),
            typeof(ExtensionAttribute),
            typeof(ParamArrayAttribute),

            typeof(int),
            typeof(long),
            typeof(uint),
            typeof(ulong),
            typeof(byte),
            typeof(sbyte),
            typeof(short),
            typeof(ushort),

            typeof(IEnumerator),
            typeof(IEnumerable),
            typeof(IDisposable),

            typeof(char),
            typeof(string),
            typeof(float),
            typeof(double),
            typeof(decimal),
            typeof(bool),
            typeof(IntPtr),
            typeof(UIntPtr),

            typeof(MulticastDelegate),
            typeof(Delegate),
            typeof(Enum),
            typeof(Array),
            typeof(void),
            typeof(Type),
            typeof(Exception),
            typeof(RuntimeFieldHandle),
            typeof(RuntimeTypeHandle),
        };
    }
}
