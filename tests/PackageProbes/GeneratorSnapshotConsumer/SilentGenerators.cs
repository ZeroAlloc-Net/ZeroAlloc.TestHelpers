using Microsoft.CodeAnalysis;

namespace GeneratorSnapshotConsumer;

/// <summary>Emits nothing and reports nothing.</summary>
[Generator]
public sealed class SilentGenerator : IIncrementalGenerator
{
    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
    }
}

/// <summary>Bails out with an error diagnostic and emits no source, like a generator rejecting its input.</summary>
[Generator]
public sealed class FailingGenerator : IIncrementalGenerator
{
    private static readonly DiagnosticDescriptor Rule = new(
        "PROBE001", "Probe failure", "Probe generator rejected the input", "Probe",
        DiagnosticSeverity.Error, isEnabledByDefault: true);

    public void Initialize(IncrementalGeneratorInitializationContext context)
        => context.RegisterSourceOutput(
            context.CompilationProvider,
            static (ctx, _) => ctx.ReportDiagnostic(Diagnostic.Create(Rule, Location.None)));
}
