using System;
using System.IO;
using System.Reflection.Emit;
using Mono.CSharp;

namespace MonoScript
{
    internal class CompilerDriver
    {
        private readonly CompilerContext _ctx;
        
        private Report Report => _ctx.Report;
        
        internal readonly ModuleContainer Module;

        internal readonly ReflectionImporter Importer;

        public CompilerDriver(CompilerContext ctx)
        {
            _ctx = ctx;

            Module = new ModuleContainer(_ctx);
            Importer = new ReflectionImporter(Module, ctx.BuiltinTypes);
        }

        private void TokenizeFile(SourceFile sourceFile, ModuleContainer module, ParserSession session)
        {
            Stream input = null;
            SeekableStreamReader reader = null;

            try
            {
                if (sourceFile.GetInputStream != null)
                {
                    reader = sourceFile.GetInputStream(sourceFile);
                    if (reader == null)
                        throw new FileNotFoundException("Delegate returned null", sourceFile.Name);
                }
                else
                    input = File.OpenRead(sourceFile.Name);
            }
            catch
            {
                Report.Error(2001, "Source file `" + sourceFile.Name + "' could not be found");
                return;
            }

            if (reader == null)
            {
                using (input)
                {
                    reader = new SeekableStreamReader(input, _ctx.Settings.Encoding);
                    DoTokenize(sourceFile, module, session, reader);
                }
            }
            else
                DoTokenize(sourceFile, module, session, reader);
        }

        private void DoTokenize(SourceFile sourceFile, ModuleContainer module, ParserSession session,
            SeekableStreamReader reader)
        {
            var file = new CompilationSourceFile(module, sourceFile);

            var lexer = new Tokenizer(reader, file, session, _ctx.Report);
            int token, tokens = 0, errors = 0;

            while ((token = lexer.token()) != Token.EOF)
            {
                tokens++;
                if (token == Token.ERROR)
                    errors++;
            }
            
            Console.WriteLine("Tokenized: " + tokens + " found " + errors + " errors");
        }

        private void Parse(ModuleContainer module)
        {
            var tokenizeOnly = module.Compiler.Settings.TokenizeOnly;
            var sources = module.Compiler.SourceFiles;

            Location.Initialize(sources);

            var session = new ParserSession
            {
                UseJayGlobalArrays = true,
                LocatedTokens = new LocatedToken[15000]
            };

            foreach (var t in sources)
            {
                if (tokenizeOnly)
                    TokenizeFile(t, module, session);
                else
                    Parse(t, module, session, Report);
            }
        }

        public void Parse(SourceFile file, ModuleContainer module, ParserSession session, Report report)
        {
            Stream input = null;
            SeekableStreamReader reader = null;

            try
            {
                if (file.GetInputStream != null)
                {
                    reader = file.GetInputStream(file);
                    if (reader == null)
                        throw new FileNotFoundException("Delegate returned null", file.Name);
                }
                else
                    input = File.OpenRead(file.Name);
            }
            catch
            {
                report.Error(2001, "Source file `{0}' could not be found", file.Name);
                return;
            }

            if (reader == null)
            {
                using (input)
                {
                    // Check 'MZ' header
                    if (input.ReadByte() == 77 && input.ReadByte() == 90)
                    {
                        report.Error(2015, "Source file `{0}' is a binary file and not a text file", file.Name);
                        return;
                    }

                    input.Position = 0;
                    reader = new SeekableStreamReader(input, _ctx.Settings.Encoding, session.StreamReaderBuffer);

                    DoParse(file, module, session, report, reader);
                }
            }
            else
                DoParse(file, module, session, report, reader);
        }

        private void DoParse(SourceFile file, ModuleContainer module, ParserSession session, Report report,
            SeekableStreamReader reader)
        {
            Parse(reader, file, module, session, report);

            if (!_ctx.Settings.GenerateDebugInfo || report.Errors != 0 || file.HasChecksum) return;
            reader.Stream.Position = 0;
            var checksum = session.GetChecksumAlgorithm();
            file.SetChecksum(checksum.ComputeHash(reader.Stream));
        }

        public static void Parse(SeekableStreamReader reader, SourceFile sourceFile, ModuleContainer module,
            ParserSession session, Report report)
        {
            var file = new CompilationSourceFile(module, sourceFile);
            module.AddTypeContainer(file);

            var parser = new CSharpParser(reader, file, report, session);
            parser.parse();
        }

        public bool Compile(out AssemblyBuilder builder, bool saveAssembly = false)
        {
            var settings = _ctx.Settings;
            builder = null;

            //
            // If we are an exe, require a source file for the entry point or
            // if there is nothing to put in the assembly, and we are not a library
            //
            if (settings.FirstSourceFile == null &&
                (settings.Target == Target.Exe || settings.Target == Target.WinExe ||
                 settings.Target == Target.Module ||
                 settings.Resources == null))
            {
                Report.Error(2008, "No files to compile were specified");
                return false;
            }

            if (settings.Platform == Platform.AnyCPU32Preferred &&
                (settings.Target == Target.Library || settings.Target == Target.Module))
            {
                Report.Error(4023, "Platform option `anycpu32bitpreferred' is valid only for executables");
                return false;
            }

            var tr = new TimeReporter(settings.Timestamps);
            _ctx.TimeReporter = tr;
            tr.StartTotal();

            RootContext.ToplevelTypes = Module;

            tr.Start(TimeReporter.TimerType.ParseTotal);
            Parse(Module);
            tr.Stop(TimeReporter.TimerType.ParseTotal);

            if (Report.Errors > 0)
                return false;

            if (settings.TokenizeOnly || settings.ParseOnly)
            {
                tr.StopTotal();
                tr.ShowStats();
                return true;
            }

            var outputFile = settings.OutputFile;
            string outputFileName;
            if (outputFile == null)
            {
                var sourceFile = settings.FirstSourceFile;

                if (sourceFile == null)
                {
                    Report.Error(1562, "If no source files are specified you must specify the output file with -out:");
                    return false;
                }

                outputFileName = sourceFile.Name;
                var pos = outputFileName.LastIndexOf('.');

                if (pos > 0)
                    outputFileName = outputFileName.Substring(0, pos);

                outputFileName += settings.TargetExt;
                outputFile = outputFileName;
            }
            else
            {
                outputFileName = Path.GetFileName(outputFile);

                if (string.IsNullOrEmpty(Path.GetFileNameWithoutExtension(outputFileName)) ||
                    outputFileName.IndexOfAny(Path.GetInvalidFileNameChars()) >= 0)
                {
                    Report.Error(2021, "Output file name is not valid");
                    return false;
                }
            }

            var assembly = new AssemblyDefinitionDynamicMonoScript(Module, outputFileName, outputFile);
            Module.SetDeclaringAssembly(assembly);

            assembly.Importer = Importer;

            var loader = new DynamicLoader(Importer, _ctx);
            loader.LoadReferences(Module);

            if (!_ctx.BuiltinTypes.CheckDefinitions(Module))
                return false;

            if (!assembly.Create(AppDomain.CurrentDomain, AssemblyBuilderAccess.RunAndSave))
                return false;

            Module.CreateContainer();

            loader.LoadModules(assembly, Module.GlobalRootNamespace);
            Module.InitializePredefinedTypes();

            if (settings.GetResourceStrings != null)
                Module.LoadGetResourceStrings(settings.GetResourceStrings);

            tr.Start(TimeReporter.TimerType.ModuleDefinitionTotal);
            Module.Define();
            tr.Stop(TimeReporter.TimerType.ModuleDefinitionTotal);

            if (Report.Errors > 0)
                return false;

            if (settings.DocumentationFile != null)
            {
                var doc = new DocumentationBuilder(Module);
                doc.OutputDocComment(outputFile, settings.DocumentationFile);
            }

            assembly.Resolve();

            if (Report.Errors > 0)
                return false;

            tr.Start(TimeReporter.TimerType.EmitTotal);
            
            assembly.Emit();
            tr.Stop(TimeReporter.TimerType.EmitTotal);

            if (Report.Errors > 0)
                return false;

            tr.Start(TimeReporter.TimerType.CloseTypes);
            Module.CloseContainer();
            tr.Stop(TimeReporter.TimerType.CloseTypes);

            tr.Start(TimeReporter.TimerType.Resouces);
            if (!settings.WriteMetadataOnly)
                assembly.EmbedResources();
            tr.Stop(TimeReporter.TimerType.Resouces);

            if (Report.Errors > 0)
                return false;

            if (saveAssembly) assembly.Save();
            builder = assembly.Builder;

            tr.StopTotal();
            tr.ShowStats();

            return Report.Errors == 0;
        }
    }
}
