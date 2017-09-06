using System;
using System.CodeDom;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.IO;
using System.Reflection.Emit;
using System.Text;
using Mono.CSharp;

namespace MonoScript
{
    internal class CSharpCodeCompiler : ICodeCompiler
    {
        private static int _assemblyCounter;

        private readonly bool _stdLib;

        private readonly List<Type> _imports;

        public CSharpCodeCompiler()
            : this(false)
        {
        }
        
        public CSharpCodeCompiler(bool stdLib)
        {
            _stdLib = stdLib;
            _imports = new List<Type>();
        }

        public void ImportType(Type t)
        {
            if (!_imports.Contains(t))
                _imports.Add(t);
        }

        public void ImportTypes(IEnumerable<Type> types)
        {
            foreach (var type in types)
                ImportType(type);
        }

        public void ClearImports()
        {
            _imports.Clear();
        }
 
        public CompilerResults CompileAssemblyFromDom(CompilerParameters options, CodeCompileUnit compilationUnit)
        {
            return CompileAssemblyFromDomBatch(options, new[] {compilationUnit});
        }
        
        public CompilerResults CompileAssemblyFromDomBatch(CompilerParameters options, CodeCompileUnit[] compilationUnits)
        {
            throw new NotSupportedException();
        }

        public CompilerResults CompileAssemblyFromFile(CompilerParameters options, string fileName)
        {
            return CompileAssemblyFromFileBatch(options, new[] {fileName});
        }
        
        public CompilerResults CompileAssemblyFromFileBatch(CompilerParameters options, string[] fileNames)
        {
            var settings = OptionsToSettings(options);
            
            foreach (var fileName in fileNames)
            {
                var path = Path.GetFullPath(fileName);
                var unit = new SourceFile(fileName, path, settings.SourceFiles.Count);
                settings.SourceFiles.Add(unit);
            }

            return CompileFromCompilerSettings(settings, options.GenerateInMemory);
        }

        public CompilerResults CompileAssemblyFromSource(CompilerParameters options, string source)
        {
            return CompileAssemblyFromSourceBatch(options, new[] {source});
        }

        public CompilerResults CompileAssemblyFromSourceBatch(CompilerParameters options, string[] sources)
        {
            var i = 0;
            var settings = OptionsToSettings(options);

            foreach (var src in sources)
            {
                var source = src;

                var fileName = i++.ToString();
                var unit = new SourceFile(fileName, fileName, settings.SourceFiles.Count,
                    file => new SeekableStreamReader(new MemoryStream(Encoding.UTF8.GetBytes(source ?? "")), Encoding.UTF8));

                settings.SourceFiles.Add(unit);
            }

            return CompileFromCompilerSettings(settings, options.GenerateInMemory);
        }

        private CompilerResults CompileFromCompilerSettings(CompilerSettings settings, bool generateInMemory)
        {
            var compilerResults = new CompilerResults(new TempFileCollection(Path.GetTempPath()));
            var driver = new Driver(new CompilerContext(settings, new CSharpReportPrinter(compilerResults)));

            var assembly = null as AssemblyBuilder;

            try
            {
                driver.ImportTypes(_imports);
                driver.Compile(out assembly, generateInMemory);
            }
            catch (Exception e)
            {
                compilerResults.Errors.Add(new CompilerError
                {
                    IsWarning = false,
                    ErrorText = e.Message,
                });
            }

            compilerResults.CompiledAssembly = assembly;
            
            return compilerResults;
        }

        private CompilerSettings OptionsToSettings(CompilerParameters options)
        {
            var settings = new CompilerSettings
            {
                Encoding = Encoding.UTF8,
                GenerateDebugInfo = options.IncludeDebugInformation,
                MainClass = options.MainClass,
                Platform = Platform.AnyCPU,
                StdLibRuntimeVersion = RuntimeVersion.v4,
                OutputFile = options.OutputAssembly,
                Version = LanguageVersion.Default,
                WarningLevel = options.WarningLevel,
                WarningsAreErrors = options.TreatWarningsAsErrors,
                Target = Target.Library,
                TargetExt = ".dll",
                LoadDefaultReferences = _stdLib,
                StdLib = _stdLib,
            };

            foreach (var assembly in options.ReferencedAssemblies)
                settings.AssemblyReferences.Add(assembly);

            if (options.GenerateExecutable)
            {
                settings.Target = Target.Exe;
                settings.TargetExt = ".exe";
            }
            
            if (options.GenerateInMemory)
                settings.Target = Target.Library;

            if (string.IsNullOrEmpty(options.OutputAssembly))
                options.OutputAssembly = settings.OutputFile =
                    $"cache/_{_assemblyCounter++}{settings.TargetExt}";

            settings.OutputFile = options.OutputAssembly;

            return settings;
        }
    }
}
