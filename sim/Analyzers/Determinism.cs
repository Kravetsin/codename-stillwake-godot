using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Operations;

namespace Stillwake.Analyzers;

/// <summary>
/// What BannedApiAnalyzers cannot see: floating point in any form (a `double` local, a float
/// literal, `Math.Sqrt`'s result), and a foreach over a Dictionary or HashSet, whose order is not
/// guaranteed. The C# side of tools/lint's determinism rule.
/// </summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class Determinism : DiagnosticAnalyzer
{
    private static readonly DiagnosticDescriptor Float = new(
        "SW0001", "No floating point in the sim",
        "'{0}' is floating point: it may round differently between CPUs; use integers, in units and ticks",
        "Determinism", DiagnosticSeverity.Error, isEnabledByDefault: true);

    private static readonly DiagnosticDescriptor Unordered = new(
        "SW0002", "No iteration in unguaranteed order",
        "A {0}'s order is not guaranteed: iterate a list or sorted keys",
        "Determinism", DiagnosticSeverity.Error, isEnabledByDefault: true);

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(Float, Unordered);

    public override void Initialize(AnalysisContext context)
    {
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.EnableConcurrentExecution();
        context.RegisterOperationAction(c =>
        {
            var type = c.Operation.Type;
            if (IsFloat(type) && c.Operation is not IConversionOperation { IsImplicit: true })
                c.ReportDiagnostic(Diagnostic.Create(Float, c.Operation.Syntax.GetLocation(), c.Operation.Syntax.ToString()));
        }, OperationKind.Literal, OperationKind.Invocation, OperationKind.LocalReference,
           OperationKind.FieldReference, OperationKind.PropertyReference, OperationKind.Conversion,
           OperationKind.Binary, OperationKind.ParameterReference);
        context.RegisterOperationAction(c =>
        {
            if (c.Operation is not IForEachLoopOperation loop) return;
            var collection = loop.Collection is IConversionOperation conv ? conv.Operand.Type : loop.Collection.Type;
            var name = collection?.OriginalDefinition.ToDisplayString();
            if (name is "System.Collections.Generic.Dictionary<TKey, TValue>" or "System.Collections.Generic.HashSet<T>")
                c.ReportDiagnostic(Diagnostic.Create(Unordered, loop.Collection.Syntax.GetLocation(), collection!.Name));
        }, OperationKind.Loop);
    }

    private static bool IsFloat(ITypeSymbol? t) =>
        t?.SpecialType is SpecialType.System_Single or SpecialType.System_Double or SpecialType.System_Decimal;
}
