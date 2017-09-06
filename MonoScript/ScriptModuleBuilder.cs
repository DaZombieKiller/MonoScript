using System;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;

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
        }

        public void ClearReferences()
        {
            _options.ReferencedAssemblies.Clear();
        }

        public void ImportStandardTypes()
        {
            _compiler.ImportTypes(StandardTypes);
        }

        public void ImportNamespace(string name)
        {
            ImportTypes(AppDomain.CurrentDomain.GetAssemblies()
                .SelectMany(t => t.GetTypes())
                .Where(t => t.Namespace == name));
        }

        public void ReferenceAssembly(Assembly asm)
        {
            _options.ReferencedAssemblies.Add(asm.Location);
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
            ClearReferences();
            
            return new ScriptModule(result.CompiledAssembly);
        }
        
        private static readonly Type[] StandardTypes =
        {
            typeof(object),
            typeof(ValueType),
            typeof(Attribute),
            typeof(int),
            typeof(uint),
            typeof(long),
            typeof(ulong),
            typeof(float),
            typeof(double),
            typeof(char),
            typeof(short),
            typeof(decimal),
            typeof(bool),
            typeof(sbyte),
            typeof(byte),
            typeof(ushort),
            typeof(string),
            typeof(Enum),
            typeof(Delegate),
            typeof(MulticastDelegate),
            typeof(void),
            typeof(Array),
            typeof(Type),
            typeof(System.Collections.IEnumerator),
            typeof(System.Collections.IEnumerable),
            typeof(IDisposable),
            typeof(IntPtr),
            typeof(UIntPtr),
            typeof(RuntimeFieldHandle),
            typeof(RuntimeTypeHandle),
            typeof(Exception),
            typeof(ParamArrayAttribute),
            typeof(System.Runtime.InteropServices.OutAttribute),
        };
    }
}
