using System.CodeDom.Compiler;
using Mono.CSharp;

namespace MonoScript
{
    internal class CSharpReportPrinter : ReportPrinter
    {
        private readonly CompilerResults _compilerResults;

        public new int ErrorsCount { get; protected set; }

        public new int WarningsCount { get; private set; }

        public CSharpReportPrinter(CompilerResults compilerResults)
        {
            _compilerResults = compilerResults;
        }

        public override void Print(AbstractMessage msg, bool showFullPath)
        {
            if (msg.IsWarning) WarningsCount++;
            else ErrorsCount++;
        
            _compilerResults.Errors.Add(new CompilerError
            {
                IsWarning = msg.IsWarning,
            
                Column = msg.Location.Column,
                Line = msg.Location.Row,
            
                ErrorNumber = msg.Code.ToString(),
                ErrorText = msg.Text,
            
                FileName = showFullPath
                    ? msg.Location.SourceFile.OriginalFullPathName
                    : msg.Location.SourceFile.Name,
            });
        }
    }
}
