using Esprima.Ast;
using Ultimate.Language.Jint.Native;
using Ultimate.Language.Jint.Pooling;

namespace Ultimate.Language.Jint.Runtime.Interpreter.Expressions
{
    internal sealed class JintTemplateLiteralExpression : JintExpression
    {
        internal readonly TemplateLiteral _templateLiteralExpression;
        internal JintExpression[] _expressions = Array.Empty<JintExpression>();

        public JintTemplateLiteralExpression(TemplateLiteral expression) : base(expression)
        {
            _templateLiteralExpression = expression;
            _initialized = false;
        }

        protected override void Initialize(EvaluationContext context)
        {
            DoInitialize();
        }

        internal void DoInitialize()
        {
            _expressions = new JintExpression[_templateLiteralExpression.Expressions.Count];
            for (var i = 0; i < _templateLiteralExpression.Expressions.Count; i++)
            {
                var exp = _templateLiteralExpression.Expressions[i];
                _expressions[i] = Build(exp);
            }

            _initialized = true;
        }

        private JsString BuildString(EvaluationContext context)
        {
            using var sb = StringBuilderPool.Rent();
            for (var i = 0; i < _templateLiteralExpression.Quasis.Count; i++)
            {
                var quasi = _templateLiteralExpression.Quasis[i];
                sb.Builder.Append(quasi.Value.Cooked);
                if (i < _expressions.Length)
                {
                    var completion = _expressions[i].GetValue(context);
                    sb.Builder.Append(completion);
                }
            }

            return JsString.Create(sb.ToString());
        }

        protected override object EvaluateInternal(EvaluationContext context)
        {
            return BuildString(context);
        }
    }
}
