using System;

namespace MonoScript.Sample
{
    public struct MyStruct
    {
        public string Message;

        public override string ToString()
        {
            return Message;
        }
    }
    
    internal class Program
    {
        public static void Main(string[] args)
        {
            var builder = new ScriptBuilder();
            builder.Start();

            builder.ImportType(typeof(Console));
            builder.ImportType<MyStruct>();
            
            builder.AddFromString("Example.cs", @"
                using System;
                using MonoScript.Sample;
                
                public class MyClass
                {
                    public MyClass()
                    {
                        var myStruct = new MyStruct
                        {
                            Message = ""Hello, world!""
                        };
                
                        Console.WriteLine(myStruct);
                    }
                }");

            if (!builder.Build(out var module))
                Console.WriteLine("Failed. {0} error(s). {1} warning(s).", module.ErrorCount, module.WarningCount);
            else
            {
                Console.WriteLine("Succeeded. {0} warning(s).", module.WarningCount);

                foreach (var type in module.GetTypes())
                    Activator.CreateInstance(type);
            }
            
            Console.WriteLine("Press any key to exit . . .");
            Console.ReadKey();
        }
    }
}
