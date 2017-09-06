# MonoScript
MonoScript is a library for .NET that allows you to use C# as if it were a scripting language like Lua, Squirrel, AngelScript, etc.

It achieves this by embedding a modified version of the Mono C# compiler (MCS), and providing an API to work with it.

MonoScript is designed with sandboxing in mind, in order to make it more approachable for usage within games (its intended use-case). As a result, you have complete control over what types and namespaces are made accessible to scripts.

## Compiling
In order to compile MonoScript, you will first need to compile `jay`. It should be as simple as building the included Premake project, refer to the Premake documentation for information on how to do so.

Once `jay` has been compiled, simply open the MonoScript solution and build it.

## Usage
Using MonoScript is very simple.

1. Create an instance of the `ScriptModuleBuilder` class
2. Import any types, references and namespaces you wish to make accessible to scripts
3. Build the module with `var module = builder.Build();`
4. Use `module.GetType()` to retrieve the script types.

```CSharp
using System;
using MonoScript;

namespace MonoScriptDemo
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
            const string script = @"
using System;
using MonoScriptDemo;

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
}";
            
            var builder = new ScriptModuleBuilder();
            builder.ImportStandardTypes();
            builder.ImportNamespace("System");
            builder.ImportType(typeof(MyStruct));
            builder.AddSource(script);
            var module = builder.Build();

            foreach (var type in module.GetTypes())
                Activator.CreateInstance(type);

            Console.WriteLine("Press any key to exit . . .");
            Console.ReadKey();
        }
    }
}
```

## Unity
MonoScript has been tested under and confirmed to work with the experimental .NET 4.6 target for Unity projects found in Unity 2017.1 and above.

## License
MonoScript is licensed under the MIT license.
