using Esprima.Ast;

namespace Ultimate.Language.Jint.Runtime.Interpreter.Statements;

internal sealed class JintEmptyStatement : JintStatement<EmptyStatement>
{
    public JintEmptyStatement(EmptyStatement statement) : base(statement)
    {
    }

    protected override Completion ExecuteInternal(EvaluationContext context)
    {
        return new Completion(CompletionType.Normal, null!, _statement);
    }
}
