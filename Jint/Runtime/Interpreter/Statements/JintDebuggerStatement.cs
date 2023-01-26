using Esprima.Ast;
using Ultimate.Language.Jint.Runtime.Debugger;

namespace Ultimate.Language.Jint.Runtime.Interpreter.Statements
{
    internal sealed class JintDebuggerStatement : JintStatement<DebuggerStatement>
    {
        public JintDebuggerStatement(DebuggerStatement statement) : base(statement)
        {
        }

        protected override Completion ExecuteInternal(EvaluationContext context)
        {
            var engine = context.Engine;
            switch (engine.Options.Debugger.StatementHandling)
            {
                case DebuggerStatementHandling.Clr:
                    if (!System.Diagnostics.Debugger.IsAttached)
                    {
                        System.Diagnostics.Debugger.Launch();
                    }

                    System.Diagnostics.Debugger.Break();
                    break;
                // DebugHandler handles DebuggerStatementHandling.Script during OnStep
                case DebuggerStatementHandling.Script:
                case DebuggerStatementHandling.Ignore:
                    break;
            }

            return new Completion(CompletionType.Normal, null!, _statement);
        }
    }
}
