﻿using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Reflection;
using System.Collections;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Mono.CSharp;

namespace MonoScript
{
    public class ScriptBuilder
    {
        private CompilerContext _context;
        private CompilerDriver _driver;

        public void Start()
        {
            Start(Console.Out, Encoding.Default);
        }

        public void Start(TextWriter error)
        {
            Start(error, Encoding.Default);
        }

        public void Start(TextWriter error, Encoding encoding)
        {
            _context = new CompilerContext(new CompilerSettings
            {
                Encoding = encoding,
                Platform = Platform.AnyCPU,
                StdLibRuntimeVersion = RuntimeVersion.v4,
                Target = Target.Library,
                LoadDefaultReferences = false,
                StdLib = false,
            }, new StreamReportPrinter(error));

            _driver = new CompilerDriver(_context);

            ImportBuiltinTypes();
        }

        public void AddFromFile(string fileName)
        {
            ThrowIfNotStarted();
            _context.Settings.SourceFiles.Add(
                new SourceFile(fileName, fileName, _context.Settings.SourceFiles.Count + 1));
        }

        public void AddFromString(string fileName, string code)
        {
            ThrowIfNotStarted();
            _context.Settings.SourceFiles.Add(
                new SourceFile(fileName, fileName, _context.Settings.SourceFiles.Count + 1,
                    file => new SeekableStreamReader(
                        new MemoryStream(_context.Settings.Encoding.GetBytes(code)), _context.Settings.Encoding)));
        }

        public void AddFromStream(string fileName, Stream code)
        {
            ThrowIfNotStarted();
            _context.Settings.SourceFiles.Add(
                new SourceFile(fileName, fileName, _context.Settings.SourceFiles.Count + 1,
                    file => new SeekableStreamReader(code, _context.Settings.Encoding)));
        }

        public void AddConditionalSymbol(string symbol)
        {
            ThrowIfNotStarted();
            _context.Settings.AddConditionalSymbol(symbol);
        }

        public void ImportType<T>()
        {
            ImportType(typeof(T));
        }

        public void ImportType(Type type)
        {
            ImportTypes(new[] {type});
        }

        private void ImportBuiltinTypes()
        {
            ImportTypes(BuiltinTypes);
        }

        public void ImportTypes(Type[] types)
        {
            ThrowIfNotStarted();
            _driver.Importer.ImportTypes(types, _driver.Module.GlobalRootNamespace, false);
        }

        public void ImportNamespace(string name)
        {
            ThrowIfNotStarted();
            foreach (var type in AppDomain.CurrentDomain.GetAssemblies()
                .SelectMany(t => t.GetTypes())
                .Where(t => t.Namespace == name)
                .Where(t => t.IsPublic))
            {
                ImportType(type);
            }
        }

        public void AddReference(Assembly asm)
        {
            ThrowIfNotStarted();

            if (!asm.IsDynamic)
                _context.Settings.AssemblyReferences.Add(asm.Location);
        }

        public bool Build(out ScriptModule module)
        {
            ThrowIfNotStarted();

            try
            {
                var success = _driver.Compile(out var builder);

                module = new ScriptModule
                {
                    Assembly = builder,
                    ErrorCount = _context.Report.Errors,
                    WarningCount = _context.Report.Warnings
                };

                return success;
            }
            finally
            {
                _context = null;
                _driver = null;
            }
        }

        private void ThrowIfNotStarted()
        {
            if (_driver == null)
                throw new InvalidOperationException("The ScriptBuilder has not been started.");
        }

        private static readonly Type[] BuiltinTypes =
        {
            typeof(object),
            typeof(ValueType),
            typeof(System.Attribute),

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
            typeof(System.Delegate),
            typeof(System.Enum),
            typeof(Array),
            typeof(void),
            typeof(Type),
            typeof(Exception),
            typeof(RuntimeFieldHandle),
            typeof(RuntimeTypeHandle),
        };
    }
}