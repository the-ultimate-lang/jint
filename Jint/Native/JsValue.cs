using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Diagnostics.Contracts;
using System.Numerics;
using System.Runtime.CompilerServices;
using Ultimate.Language.Jint.Native.Generator;
using Ultimate.Language.Jint.Native.Iterator;
using Ultimate.Language.Jint.Native.Number;
using Ultimate.Language.Jint.Native.Object;
using Ultimate.Language.Jint.Native.Symbol;
using Ultimate.Language.Jint.Runtime;

namespace Ultimate.Language.Jint.Native
{
    [DebuggerTypeProxy(typeof(JsValueDebugView))]
    public abstract class JsValue : IEquatable<JsValue>
    {
        public static readonly JsValue Undefined = new JsUndefined();
        public static readonly JsValue Null = new JsNull();
        internal readonly InternalTypes _type;

        protected JsValue(Types type)
        {
            _type = (InternalTypes) type;
        }

        internal JsValue(InternalTypes type)
        {
            _type = type;
        }

        [Pure]
        public virtual bool IsArray() => false;

        internal virtual bool IsIntegerIndexedArray => false;

        internal virtual bool IsConstructor => false;

        [Pure]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal IteratorInstance GetIterator(Realm realm, GeneratorKind hint = GeneratorKind.Sync, ICallable? method = null)
        {
            if (!TryGetIterator(realm, out var iterator, hint, method))
            {
                ExceptionHelper.ThrowTypeError(realm, "The value is not iterable");
                return null!;
            }

            return iterator;
        }

        [Pure]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal bool TryGetIterator(Realm realm, [NotNullWhen(true)] out IteratorInstance? iterator, GeneratorKind hint = GeneratorKind.Sync, ICallable? method = null)
        {
            var obj = TypeConverter.ToObject(realm, this);

            if (method is null)
            {
                if (hint == GeneratorKind.Async)
                {
                    method = obj.GetMethod(GlobalSymbolRegistry.AsyncIterator);
                    if (method is null)
                    {
                        var syncMethod = obj.GetMethod(GlobalSymbolRegistry.Iterator);
                        var syncIteratorRecord = obj.GetIterator(realm, GeneratorKind.Sync, syncMethod);
                        // TODO async CreateAsyncFromSyncIterator(syncIteratorRecord);
                        ExceptionHelper.ThrowNotImplementedException("async");
                    }
                }
                else
                {
                    method = obj.GetMethod(GlobalSymbolRegistry.Iterator);
                }
            }

            if (method is null)
            {
                iterator = null;
                return false;
            }

            var iteratorResult = method.Call(obj, Arguments.Empty) as ObjectInstance;
            if (iteratorResult is null)
            {
                ExceptionHelper.ThrowTypeError(realm, "Result of the Symbol.iterator method is not an object");
            }

            if (iteratorResult is IteratorInstance i)
            {
                iterator = i;
            }
            else
            {
                iterator = new IteratorInstance.ObjectIterator(iteratorResult);
            }

            return true;
        }

        public Types Type
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => _type == InternalTypes.Integer
                ? Types.Number
                : (Types) (_type & ~InternalTypes.InternalFlags);
        }

        /// <summary>
        /// Creates a valid <see cref="JsValue"/> instance from any <see cref="Object"/> instance
        /// </summary>
        public static JsValue FromObject(Engine engine, object? value)
        {
            if (value is null)
            {
                return Null;
            }

            if (value is JsValue jsValue)
            {
                return jsValue;
            }

            if (engine._objectConverters != null)
            {
                foreach (var converter in engine._objectConverters)
                {
                    if (converter.TryConvert(engine, value, out var result))
                    {
                        return result;
                    }
                }
            }

            if (DefaultObjectConverter.TryConvert(engine, value, out var defaultConversion))
            {
                return defaultConversion;
            }

            return null!;
        }

        /// <summary>
        /// Converts a <see cref="JsValue"/> to its underlying CLR value.
        /// </summary>
        /// <returns>The underlying CLR value of the <see cref="JsValue"/> instance.</returns>
        public abstract object ToObject();

        /// <summary>
        /// Coerces boolean value from <see cref="JsValue"/> instance.
        /// </summary>
        internal virtual bool ToBoolean() => _type > InternalTypes.Null;

        /// <summary>
        /// Invoke the current value as function.
        /// </summary>
        /// <param name="engine">The engine handling the invoke.</param>
        /// <param name="arguments">The arguments of the function call.</param>
        /// <returns>The value returned by the function call.</returns>
        [Obsolete("Should use Engine.Invoke when direct invoking is needed.")]
        public JsValue Invoke(Engine engine, params JsValue[] arguments)
        {
            return engine.Invoke(this, arguments);
        }

        /// <summary>
        /// https://tc39.es/ecma262/#sec-getv
        /// </summary>
        internal JsValue GetV(Realm realm, JsValue property)
        {
            var o = TypeConverter.ToObject(realm, this);
            return o.Get(property, this);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public JsValue Get(JsValue property)
        {
            return Get(property, this);
        }

        /// <summary>
        /// https://tc39.es/ecma262/#sec-get-o-p
        /// </summary>
        public virtual JsValue Get(JsValue property, JsValue receiver)
        {
            return Undefined;
        }

        /// <summary>
        /// https://tc39.es/ecma262/#sec-set-o-p-v-throw
        /// </summary>
        public virtual bool Set(JsValue property, JsValue value, JsValue receiver)
        {
            ExceptionHelper.ThrowNotSupportedException();
            return false;
        }

        /// <summary>
        /// https://tc39.es/ecma262/#sec-instanceofoperator
        /// </summary>
        internal bool InstanceofOperator(JsValue target)
        {
            var oi = target as ObjectInstance;
            if (oi is null)
            {
                ExceptionHelper.ThrowTypeErrorNoEngine("not an object");
            }

            var instOfHandler = oi.GetMethod(GlobalSymbolRegistry.HasInstance);
            if (instOfHandler is not null)
            {
                return TypeConverter.ToBoolean(instOfHandler.Call(target, new[] {this}));
            }

            if (!target.IsCallable)
            {
                ExceptionHelper.ThrowTypeErrorNoEngine("not callable");
            }

            return target.OrdinaryHasInstance(this);
        }

        public override string ToString()
        {
            return "None";
        }

        public static bool operator ==(JsValue? a, JsValue? b)
        {
            if (a is null)
            {
                return b is null;
            }

            return b is not null && a.Equals(b);
        }

        public static bool operator !=(JsValue? a, JsValue? b)
        {
            return !(a == b);
        }

        public static implicit operator JsValue(char value)
        {
            return JsString.Create(value);
        }

        public static implicit operator JsValue(int value)
        {
            return JsNumber.Create(value);
        }

        public static implicit operator JsValue(uint value)
        {
            return JsNumber.Create(value);
        }

        public static implicit operator JsValue(double value)
        {
            return JsNumber.Create(value);
        }

        public static implicit operator JsValue(long value)
        {
            return JsNumber.Create(value);
        }

        public static implicit operator JsValue(ulong value)
        {
            return JsNumber.Create(value);
        }

        public static implicit operator JsValue(BigInteger value)
        {
            return JsBigInt.Create(value);
        }

        public static implicit operator JsValue(bool value)
        {
            return value ? JsBoolean.True : JsBoolean.False;
        }

        [DebuggerStepThrough]
        public static implicit operator JsValue(string? value)
        {
            return value == null ? Null : JsString.Create(value);
        }

        /// <summary>
        /// https://tc39.es/ecma262/#sec-islooselyequal
        /// </summary>
        public virtual bool IsLooselyEqual(JsValue value)
        {
            if (ReferenceEquals(this, value))
            {
                return true;
            }

            // TODO move to type specific IsLooselyEqual

            var x = this;
            var y = value;

            if (x.IsNumber() && y.IsString())
            {
                return x.IsLooselyEqual(TypeConverter.ToNumber(y));
            }

            if (x.IsString() && y.IsNumber())
            {
                return y.IsLooselyEqual(TypeConverter.ToNumber(x));
            }

            if (x.IsBoolean())
            {
                return y.IsLooselyEqual(TypeConverter.ToNumber(x));
            }

            if (y.IsBoolean())
            {
                return x.IsLooselyEqual(TypeConverter.ToNumber(y));
            }

            if (y.IsObject() && (x._type & InternalTypes.Primitive) != 0)
            {
                return x.IsLooselyEqual(TypeConverter.ToPrimitive(y));
            }

            if (x.IsObject() && (y._type & InternalTypes.Primitive) != 0)
            {
                return y.IsLooselyEqual(TypeConverter.ToPrimitive(x));
            }

            return false;
        }

        /// <summary>
        /// Strict equality.
        /// </summary>
        public override bool Equals(object obj)
        {
            return Equals(obj as JsValue);
        }

        /// <summary>
        /// Strict equality.
        /// </summary>
        public virtual bool Equals(JsValue? other)
        {
            return ReferenceEquals(this, other);
        }

        public override int GetHashCode()
        {
            return _type.GetHashCode();
        }

        internal sealed class JsValueDebugView
        {
            public string Value;

            public JsValueDebugView(JsValue value)
            {
                switch (value.Type)
                {
                    case Types.None:
                        Value = "None";
                        break;
                    case Types.Undefined:
                        Value = "undefined";
                        break;
                    case Types.Null:
                        Value = "null";
                        break;
                    case Types.Boolean:
                        Value = ((JsBoolean) value)._value + " (bool)";
                        break;
                    case Types.String:
                        Value = value.ToString() + " (string)";
                        break;
                    case Types.Number:
                        Value = ((JsNumber) value)._value + " (number)";
                        break;
                    case Types.BigInt:
                        Value = ((JsBigInt) value)._value + " (bigint)";
                        break;
                    case Types.Object:
                        Value = value.AsObject().GetType().Name;
                        break;
                    case Types.Symbol:
                        var jsValue = ((JsSymbol) value)._value;
                        Value = (jsValue.IsUndefined() ? "" : jsValue.ToString()) + " (symbol)";
                        break;
                    default:
                        Value = "Unknown";
                        break;
                }
            }
        }

        /// <summary>
        /// Some values need to be cloned in order to be assigned, like ConcatenatedString.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal JsValue Clone()
        {
            // concatenated string and arguments currently may require cloning
            return (_type & InternalTypes.RequiresCloning) == 0
                ? this
                : DoClone();
        }

        internal virtual JsValue DoClone()
        {
            return this;
        }

        internal virtual bool IsCallable => this is ICallable;

        /// <summary>
        /// https://tc39.es/ecma262/#sec-ordinaryhasinstance
        /// </summary>
        internal virtual bool OrdinaryHasInstance(JsValue v)
        {
            if (!IsCallable)
            {
                return false;
            }

            var o = v as ObjectInstance;
            if (o is null)
            {
                return false;
            }

            var p = Get(CommonProperties.Prototype);
            if (p is not ObjectInstance)
            {
                ExceptionHelper.ThrowTypeError(o.Engine.Realm, $"Function has non-object prototype '{TypeConverter.ToString(p)}' in instanceof check");
            }

            while (true)
            {
                o = o.Prototype;

                if (o is null)
                {
                    return false;
                }

                if (SameValue(p, o))
                {
                    return true;
                }
            }
        }

        internal static bool SameValue(JsValue x, JsValue y)
        {
            var typea = x.Type;
            var typeb = y.Type;

            if (typea != typeb)
            {
                return false;
            }

            switch (typea)
            {
                case Types.Number:
                    if (x._type == y._type && x._type == InternalTypes.Integer)
                    {
                        return x.AsInteger() == y.AsInteger();
                    }

                    var nx = TypeConverter.ToNumber(x);
                    var ny = TypeConverter.ToNumber(y);

                    if (double.IsNaN(nx) && double.IsNaN(ny))
                    {
                        return true;
                    }

                    if (nx == ny)
                    {
                        if (nx == 0)
                        {
                            // +0 !== -0
                            return NumberInstance.IsNegativeZero(nx) == NumberInstance.IsNegativeZero(ny);
                        }

                        return true;
                    }

                    return false;
                case Types.String:
                    return TypeConverter.ToString(x) == TypeConverter.ToString(y);
                case Types.Boolean:
                    return TypeConverter.ToBoolean(x) == TypeConverter.ToBoolean(y);
                case Types.Undefined:
                case Types.Null:
                    return true;
                case Types.Symbol:
                    return x == y;
                default:
                    return ReferenceEquals(x, y);
            }
        }

        internal static IConstructor AssertConstructor(Engine engine, JsValue c)
        {
            if (!c.IsConstructor)
            {
                ExceptionHelper.ThrowTypeError(engine.Realm, c + " is not a constructor");
            }

            return (IConstructor) c;
        }
    }
}
