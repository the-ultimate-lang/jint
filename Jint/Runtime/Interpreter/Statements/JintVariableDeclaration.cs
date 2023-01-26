using Esprima.Ast;
using Ultimate.Language.Jint.Native;
using Ultimate.Language.Jint.Native.Function;
using Ultimate.Language.Jint.Runtime.Interpreter.Expressions;
using Ultimate.Language.Jint.Runtime.References;

namespace Ultimate.Language.Jint.Runtime.Interpreter.Statements
{
    internal sealed class JintVariableDeclaration : JintStatement<VariableDeclaration>
    {
        private ResolvedDeclaration[] _declarations = Array.Empty<ResolvedDeclaration>();

        private sealed class ResolvedDeclaration
        {
            internal JintExpression? Left;
            internal BindingPattern? LeftPattern;
            internal JintExpression? Init;
            internal JintIdentifierExpression? LeftIdentifierExpression;
            internal bool EvalOrArguments;
        }

        public JintVariableDeclaration(VariableDeclaration statement) : base(statement)
        {
        }

        protected override void Initialize(EvaluationContext context)
        {
            var engine = context.Engine;
            _declarations = new ResolvedDeclaration[_statement.Declarations.Count];
            for (var i = 0; i < _declarations.Length; i++)
            {
                var declaration = _statement.Declarations[i];

                JintExpression? left = null;
                JintExpression? init = null;
                BindingPattern? bindingPattern = null;

                if (declaration.Id is BindingPattern bp)
                {
                    bindingPattern = bp;
                }
                else
                {
                    left = JintExpression.Build((Identifier) declaration.Id);
                }

                if (declaration.Init != null)
                {
                    init = JintExpression.Build(declaration.Init);
                }

                var leftIdentifier = left as JintIdentifierExpression;
                _declarations[i] = new ResolvedDeclaration
                {
                    Left = left,
                    LeftPattern = bindingPattern,
                    LeftIdentifierExpression = leftIdentifier,
                    EvalOrArguments = leftIdentifier?.HasEvalOrArguments == true,
                    Init = init
                };
            }
        }

        protected override Completion ExecuteInternal(EvaluationContext context)
        {
            var engine = context.Engine;
            foreach (var declaration in _declarations)
            {
                if (_statement.Kind != VariableDeclarationKind.Var && declaration.Left != null)
                {
                    var lhs = (Reference) declaration.Left.Evaluate(context);
                    var value = JsValue.Undefined;
                    if (declaration.Init != null)
                    {
                        value = declaration.Init.GetValue(context).Clone();
                        if (declaration.Init._expression.IsFunctionDefinition())
                        {
                            ((FunctionInstance) value).SetFunctionName(lhs.GetReferencedName());
                        }
                    }

                    lhs.InitializeReferencedBinding(value);
                    engine._referencePool.Return(lhs);
                }
                else if (declaration.Init != null)
                {
                    if (declaration.LeftPattern != null)
                    {
                        var environment = _statement.Kind != VariableDeclarationKind.Var
                            ? engine.ExecutionContext.LexicalEnvironment
                            : null;

                        var value = declaration.Init.GetValue(context);

                        BindingPatternAssignmentExpression.ProcessPatterns(
                            context,
                            declaration.LeftPattern,
                            value,
                            environment,
                            checkPatternPropertyReference: _statement.Kind != VariableDeclarationKind.Var);
                    }
                    else if (declaration.LeftIdentifierExpression == null
                             || JintAssignmentExpression.SimpleAssignmentExpression.AssignToIdentifier(
                                 context,
                                 declaration.LeftIdentifierExpression,
                                 declaration.Init,
                                 declaration.EvalOrArguments) is null)
                    {
                        // slow path
                        var lhs = (Reference) declaration.Left!.Evaluate(context);
                        lhs.AssertValid(engine.Realm);

                        var value = declaration.Init.GetValue(context).Clone();

                        if (declaration.Init._expression.IsFunctionDefinition())
                        {
                            ((FunctionInstance) value).SetFunctionName(lhs.GetReferencedName());
                        }

                        engine.PutValue(lhs, value);
                        engine._referencePool.Return(lhs);
                    }
                }
            }

            return Completion.Empty();
        }
    }
}
