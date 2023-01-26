using Esprima.Ast;
using Ultimate.Language.Jint.Native;
using Ultimate.Language.Jint.Runtime.Environments;
using Ultimate.Language.Jint.Runtime.References;

namespace Ultimate.Language.Jint.Runtime.Interpreter.Expressions
{
    /// <summary>
    /// http://www.ecma-international.org/ecma-262/5.1/#sec-11.2.1
    /// </summary>
    internal sealed class JintMemberExpression : JintExpression
    {
        private readonly MemberExpression _memberExpression;
        private JintExpression _objectExpression = null!;
        private JintExpression? _propertyExpression;
        private JsValue? _determinedProperty;

        public JintMemberExpression(MemberExpression expression) : base(expression)
        {
            _initialized = false;
            _memberExpression = (MemberExpression) _expression;
        }

        protected override void Initialize(EvaluationContext context)
        {
            _objectExpression = Build(_memberExpression.Object);

            if (!_memberExpression.Computed)
            {
                _determinedProperty = ((Identifier) _memberExpression.Property).Name;
            }
            else if (_memberExpression.Property.Type == Nodes.Literal)
            {
                _determinedProperty = JintLiteralExpression.ConvertToJsValue((Literal) _memberExpression.Property);
            }

            if (_determinedProperty is null)
            {
                _propertyExpression = Build(_memberExpression.Property);
            }
        }

        protected override object EvaluateInternal(EvaluationContext context)
        {
            JsValue? actualThis = null;
            string? baseReferenceName = null;
            JsValue? baseValue = null;
            var isStrictModeCode = StrictModeScope.IsStrictModeCode;

            var engine = context.Engine;
            if (_objectExpression is JintIdentifierExpression identifierExpression)
            {
                baseReferenceName = identifierExpression.Identifier.Key.Name;
                var strict = isStrictModeCode;
                var env = engine.ExecutionContext.LexicalEnvironment;
                JintEnvironment.TryGetIdentifierEnvironmentWithBindingValue(
                    env,
                    identifierExpression.Identifier,
                    strict,
                    out _,
                    out baseValue);
            }
            else if (_objectExpression is JintThisExpression thisExpression)
            {
                baseValue = (JsValue?) thisExpression.GetValue(context);
            }
            else if (_objectExpression is JintSuperExpression)
            {
                var env = (FunctionEnvironmentRecord) engine.ExecutionContext.GetThisEnvironment();
                actualThis = env.GetThisBinding();
                baseValue = env.GetSuperBase();
            }

            if (baseValue is null)
            {
                // fast checks failed
                var baseReference = _objectExpression.Evaluate(context);
                if (ReferenceEquals(JsValue.Undefined, baseReference))
                {
                    return JsValue.Undefined;
                }
                if (baseReference is Reference reference)
                {
                    baseReferenceName = reference.GetReferencedName().ToString();
                    baseValue = engine.GetValue(reference, false);
                    engine._referencePool.Return(reference);
                }
                else
                {
                    baseValue = engine.GetValue(baseReference, false);
                }
            }

            if (baseValue.IsNullOrUndefined() && (_memberExpression.Optional || _objectExpression._expression.IsOptional()))
            {
                return JsValue.Undefined;
            }

            var property = _determinedProperty ?? _propertyExpression!.GetValue(context);
            if (baseValue.IsNullOrUndefined())
            {
                // we can use base data types securely, object evaluation can mess things up
                var referenceName = property.IsPrimitive()
                    ? TypeConverter.ToString(property)
                    : _determinedProperty?.ToString() ?? baseReferenceName;

                TypeConverter.CheckObjectCoercible(engine, baseValue, _memberExpression.Property, referenceName!);
            }

            // only convert if necessary
            var propertyKey = property.IsInteger() && baseValue.IsIntegerIndexedArray
                ? property
                : TypeConverter.ToPropertyKey(property);

            return context.Engine._referencePool.Rent(baseValue, propertyKey, isStrictModeCode, thisValue: actualThis);
        }
    }
}
