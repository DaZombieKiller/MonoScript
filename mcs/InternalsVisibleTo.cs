// This will make all of the Mono.CSharp internals visible to the MonoScript assembly.
// It's a necessary action in order to reduce the amount of changes to Mono.CSharp's source.
// Less changes = less work required to update the Mono.CSharp version in the future.

using System.Runtime.CompilerServices;

[assembly:InternalsVisibleTo("MonoScript")]
