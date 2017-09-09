using System;
using System.Reflection;
using System.Reflection.Emit;
using Mono.CSharp;

namespace MonoScript
{
    // The original AssemblyBuilderMonoSpecific class doesn't
    // override GetReferencedAssemblies, thus the function
    // returns null. This causes a null reference exception in
    // AssemblyDefinition.CheckReferencesPublicToken.
    internal class AssemblyBuilderMonoScript : AssemblyBuilderMonoSpecific
    {
        private readonly AssemblyBuilder _builder;

        public AssemblyBuilderMonoScript(AssemblyBuilder ab, CompilerContext ctx)
            : base(ab, ctx)
        {
            _builder = ab;
        }

        public override AssemblyName[] GetReferencedAssemblies()
        {
            return _builder?.GetReferencedAssemblies() ?? new AssemblyName[0];
        }
    }

    internal class AssemblyDefinitionDynamicMonoScript : AssemblyDefinitionDynamic
    {
        public AssemblyDefinitionDynamicMonoScript(ModuleContainer module, string name)
            : base(module, name)
        {
        }

        public AssemblyDefinitionDynamicMonoScript(ModuleContainer module, string name, string fileName)
            : base(module, name, fileName)
        {
        }

        // Replace the Create() function with a clone of it that uses
        // AssemblyBuilderMonoScript instead of AssemblyBuilderMonoSpecific.
        public new bool Create(AppDomain domain, AssemblyBuilderAccess access)
        {
#if STATIC || FULL_AOT_RUNTIME
			throw new NotSupportedException ();
#else
            ResolveAssemblySecurityAttributes();
            var an = CreateAssemblyName();

            Builder = file_name == null
                ? domain.DefineDynamicAssembly(an, access)
                : domain.DefineDynamicAssembly(an, access, Dirname(file_name));

            module.Create(this, CreateModuleBuilder());
            builder_extra = new AssemblyBuilderMonoScript(Builder, Compiler);
            return true;
#endif
        }

        private static string Dirname(string name)
        {
            var pos = name.LastIndexOf('/');

            if (pos != -1)
                return name.Substring(0, pos);

            pos = name.LastIndexOf('\\');

            return pos != -1 ? name.Substring(0, pos) : ".";
        }
    }
}
