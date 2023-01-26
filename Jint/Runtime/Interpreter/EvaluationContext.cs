using System.Runtime.CompilerServices;
using Esprima.Ast;

namespace Ultimate.Language.Jint.Runtime.Interpreter;

/// <summary>
/// Per Engine.Evaluate() call context.
/// </summary>
internal sealed class EvaluationContext
{
    private readonly bool _shouldRunBeforeExecuteStatementChecks;

    public EvaluationContext(Engine engine, in Completion? resumedCompletion = null)
    {
        Engine = engine;
        ResumedCompletion = resumedCompletion ?? default; // TODO later
        OperatorOverloadingAllowed = engine.Options.Interop.AllowOperatorOverloading;
        _shouldRunBeforeExecuteStatementChecks = engine._constraints.Length > 0 || engine._isDebugMode;
    }

    // for fast evaluation checks only
    public EvaluationContext()
    {
        Engine = null!;
        ResumedCompletion = default; // TODO later
        OperatorOverloadingAllowed = false;
        _shouldRunBeforeExecuteStatementChecks = false;
    }

    public readonly Engine Engine;
    public readonly Completion ResumedCompletion;
    public bool DebugMode => Engine._isDebugMode;

    public SyntaxElement LastSyntaxElement = null!;
    public readonly bool OperatorOverloadingAllowed;

    // completion record information
    public string? Target;
    public CompletionType Completion;

    public void RunBeforeExecuteStatementChecks(Statement statement)
    {
        if (_shouldRunBeforeExecuteStatementChecks)
        {
            Engine.RunBeforeExecuteStatementChecks(statement);
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void PrepareFor(Node node)
    {
        LastSyntaxElement = node;
        Target = null;
        Completion = CompletionType.Normal;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool IsAbrupt() => Completion != CompletionType.Normal;
}
