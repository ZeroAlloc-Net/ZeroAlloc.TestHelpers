using Microsoft.CodeAnalysis;

namespace GeneratorSnapshotConsumer;

[Generator]
public sealed class HelloGenerator : IIncrementalGenerator
{
    public void Initialize(IncrementalGeneratorInitializationContext context)
        => context.RegisterPostInitializationOutput(static ctx =>
            ctx.AddSource("Hello.g.cs", "namespace Probe;\n\ninternal static class Hello\n{\n}\n"));
}
