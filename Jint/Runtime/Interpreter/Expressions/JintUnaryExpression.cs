using Esprima.Ast;
using Ultimate.Language.Jint.Extensions;
using Ultimate.Language.Jint.Native;
using Ultimate.Language.Jint.Runtime.Environments;
using Ultimate.Language.Jint.Runtime.Interop;
using Ultimate.Language.Jint.Runtime.References;
using System.Collections.Concurrent;
using System.Diagnostics.CodeAnalysis;
using System.Numerics;
using System.Reflection;

namespace Ultimate.Language.Jint.Runtime.Interpreter.Expressions
{
    internal sealed class JintUnaryExpression : JintExpression
    {
        private readonly record struct OperatorKey(string OperatorName, Type Operand);
        private static readonly ConcurrentDictionary<OperatorKey, MethodDescriptor?> _knownOperators = new();

        private readonly JintExpression _argument;
        private readonly UnaryOperator _operator;

        private JintUnaryExpression(UnaryExpression expression) : base(expression)
        {
            _argument = Build(expression.Argument);
            _operator = expression.Operator;
        }

        internal static JintExpression Build(UnaryExpression expression)
        {
            if (expression.AssociatedData is JsValue cached)
            {
                return new JintConstantExpression(expression, cached);
            }

            if (expression.Operator == UnaryOperator.Minus
                && expression.Argument is Literal literal)
            {
                var value = JintLiteralExpression.ConvertToJsValue(literal);
                if (value is not null)
                {
                    // valid for caching
                    var evaluatedValue = EvaluateMinus(value);
                    expression.AssociatedData = evaluatedValue;
                    return new JintConstantExpression(expression, evaluatedValue);
                }
            }

            return new JintUnaryExpression(expression);
        }

        public override JsValue GetValue(EvaluationContext context)
        {
            // need to notify correct node when taking shortcut
            context.LastSyntaxElement = _expression;
            return EvaluateJsValue(context);
        }

        protected override object EvaluateInternal(EvaluationContext context)
        {
            return EvaluateJsValue(context);
        }

        private JsValue EvaluateJsValue(EvaluationContext context)
        {
            var engine = context.Engine;
            switch (_operator)
            {
                case UnaryOperator.Plus:
                {
                    var v = _argument.GetValue(context);
                    if (context.OperatorOverloadingAllowed &&
                        TryOperatorOverloading(context, v, "op_UnaryPlus", out var result))
                    {
                        return result;
                    }

                    return TypeConverter.ToNumber(v);
                }
                case UnaryOperator.Minus:
                {
                    var v = _argument.GetValue(context);
                    if (context.OperatorOverloadingAllowed &&
                        TryOperatorOverloading(context, v, "op_UnaryNegation", out var result))
                    {
                        return result;
                    }

                    return EvaluateMinus(v);
                }
                case UnaryOperator.BitwiseNot:
                {
                    var v = _argument.GetValue(context);
                    if (context.OperatorOverloadingAllowed &&
                        TryOperatorOverloading(context, v, "op_OnesComplement", out var result))
                    {
                        return result;
                    }

                    var value = TypeConverter.ToNumeric(v);
                    if (value.IsNumber())
                    {
                        return JsNumber.Create(~TypeConverter.ToInt32(value));
                    }

                    return JsBigInt.Create(~value.AsBigInt());
                }
                case UnaryOperator.LogicalNot:
                {
                    var v = _argument.GetValue(context);
                    if (context.OperatorOverloadingAllowed &&
                        TryOperatorOverloading(context, v, "op_LogicalNot", out var result))
                    {
                        return result;
                    }

                    return !TypeConverter.ToBoolean(v) ? JsBoolean.True : JsBoolean.False;
                }

                case UnaryOperator.Delete:
                    // https://262.ecma-international.org/5.1/#sec-11.4.1
                    if (_argument.Evaluate(context) is not Reference r)
                    {
                        return JsBoolean.True;
                    }

                    if (r.IsUnresolvableReference())
                    {
                        if (r.IsStrictReference())
                        {
                            ExceptionHelper.ThrowSyntaxError(engine.Realm, "Delete of an unqualified identifier in strict mode.");
                        }

                        engine._referencePool.Return(r);
                        return JsBoolean.True;
                    }

                    var referencedName = r.GetReferencedName();
                    if (r.IsPropertyReference())
                    {
                        if (r.IsSuperReference())
                        {
                            ExceptionHelper.ThrowReferenceError(engine.Realm, r);
                        }

                        var o = TypeConverter.ToObject(engine.Realm, r.GetBase());
                        var deleteStatus = o.Delete(referencedName);
                        if (!deleteStatus)
                        {
                            if (r.IsStrictReference())
                            {
                                ExceptionHelper.ThrowTypeError(engine.Realm, $"Cannot delete property '{referencedName}' of {o}");
                            }

                            if (StrictModeScope.IsStrictModeCode && !r.GetBase().AsObject().GetProperty(referencedName).Configurable)
                            {
                                ExceptionHelper.ThrowTypeError(engine.Realm, $"Cannot delete property '{referencedName}' of {o}");
                            }
                        }

                        engine._referencePool.Return(r);
                        return deleteStatus ? JsBoolean.True : JsBoolean.False;
                    }

                    if (r.IsStrictReference())
                    {
                        ExceptionHelper.ThrowSyntaxError(engine.Realm);
                    }

                    var bindings = (EnvironmentRecord) r.GetBase();
                    var property = referencedName;
                    engine._referencePool.Return(r);

                    return bindings.DeleteBinding(property.ToString()) ? JsBoolean.True : JsBoolean.False;

                case UnaryOperator.Void:
                    _argument.GetValue(context);
                    return JsValue.Undefined;

                case UnaryOperator.TypeOf:
                {
                    var result = _argument.Evaluate(context);
                    JsValue v;

                    if (result is Reference rf)
                    {
                        if (rf.IsUnresolvableReference())
                        {
                            engine._referencePool.Return(rf);
                            return JsString.UndefinedString;
                        }

                        v = engine.GetValue(rf, true);
                    }
                    else
                    {
                        v = (JsValue) result;
                    }

                    if (v.IsUndefined())
                    {
                        return JsString.UndefinedString;
                    }

                    if (v.IsNull())
                    {
                        return JsString.ObjectString;
                    }

                    switch (v.Type)
                    {
                        case Types.Boolean: return JsString.BooleanString;
                        case Types.Number: return JsString.NumberString;
                        case Types.BigInt: return JsString.BigIntString;
                        case Types.String: return JsString.StringString;
                        case Types.Symbol: return JsString.SymbolString;
                    }

                    if (v.IsCallable)
                    {
                        return JsString.FunctionString;
                    }

                    return JsString.ObjectString;
                }
                default:
                    ExceptionHelper.ThrowArgumentException();
                    return null;
            }
        }

        private static JsValue EvaluateMinus(JsValue value)
        {
            if (value.IsInteger())
            {
                var asInteger = value.AsInteger();
                if (asInteger != 0)
                {
                    return JsNumber.Create(asInteger * -1);
                }
            }

            value = TypeConverter.ToNumeric(value);
            if (value.IsNumber())
            {
                var n = ((JsNumber) value)._value;
                return double.IsNaN(n) ? JsNumber.DoubleNaN : JsNumber.Create(n * -1);
            }

            var bigInt = value.AsBigInt();
            return JsBigInt.Create(BigInteger.Negate(bigInt));
        }

        internal static bool TryOperatorOverloading(EvaluationContext context, JsValue value, string clrName, [NotNullWhen(true)] out JsValue? result)
        {
            var operand = value.ToObject();

            if (operand != null)
            {
                var operandType = operand.GetType();
                var arguments = new[] { value };

                var key = new OperatorKey(clrName, operandType);
                var method = _knownOperators.GetOrAdd(key, _ =>
                {
                    MethodInfo? foundMethod = null;
                    foreach (var x in operandType.GetOperatorOverloadMethods())
                    {
                        if (x.Name == clrName && x.GetParameters().Length == 1)
                        {
                            foundMethod = x;
                            break;
                        }
                    }

                    if (foundMethod != null)
                    {
                        return new MethodDescriptor(foundMethod);
                    }
                    return null;
                });

                if (method != null)
                {
                    result = method.Call(context.Engine, null, arguments);
                    return true;
                }
            }
            result = null;
            return false;
        }
    }
}
