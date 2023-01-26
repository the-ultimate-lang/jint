using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using Esprima.Ast;
using Ultimate.Language.Jint.Native;
using Ultimate.Language.Jint.Native.Argument;
using Ultimate.Language.Jint.Runtime.Environments;

namespace Ultimate.Language.Jint.Runtime.Interpreter.Expressions
{
    internal sealed class JintIdentifierExpression : JintExpression
    {
        public JintIdentifierExpression(Identifier expression) : base(expression)
        {
        }

        internal EnvironmentRecord.BindingName Identifier
        {
            get => (EnvironmentRecord.BindingName) (_expression.AssociatedData ??= new EnvironmentRecord.BindingName(((Identifier) _expression).Name));
        }

        public bool HasEvalOrArguments => Identifier.HasEvalOrArguments;

        protected override object EvaluateInternal(EvaluationContext context)
        {
            var engine = context.Engine;
            var env = engine.ExecutionContext.LexicalEnvironment;
            var strict = StrictModeScope.IsStrictModeCode;
            var identifierEnvironment = JintEnvironment.TryGetIdentifierEnvironmentWithBinding(env, Identifier, out var temp)
                ? temp
                : JsValue.Undefined;

            return engine._referencePool.Rent(identifierEnvironment, Identifier.StringValue, strict, thisValue: null);
        }

        public override JsValue GetValue(EvaluationContext context)
        {
            // need to notify correct node when taking shortcut
            context.LastSyntaxElement = _expression;

            if (Identifier.CalculatedValue is not null)
            {
                return Identifier.CalculatedValue;
            }

            var strict = StrictModeScope.IsStrictModeCode;
            var engine = context.Engine;
            var env = engine.ExecutionContext.LexicalEnvironment;

            if (JintEnvironment.TryGetIdentifierEnvironmentWithBindingValue(
                env,
                Identifier,
                strict,
                out _,
                out var value))
            {
                if (value is null)
                {
                    ThrowNotInitialized(engine);
                }
            }
            else
            {
                var reference = engine._referencePool.Rent(JsValue.Undefined, Identifier.StringValue, strict, thisValue: null);
                value = engine.GetValue(reference, true);
            }

            // make sure arguments access freezes state
            if (value is ArgumentsInstance argumentsInstance)
            {
                argumentsInstance.Materialize();
            }

            return value;
        }

        [DoesNotReturn]
        [MethodImpl(MethodImplOptions.NoInlining)]
        private void ThrowNotInitialized(Engine engine)
        {
            ExceptionHelper.ThrowReferenceError(engine.Realm, Identifier.Key.Name + " has not been initialized");
        }
    }
}
