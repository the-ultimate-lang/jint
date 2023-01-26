using System.Numerics;
using Ultimate.Language.Jint.Collections;
using Ultimate.Language.Jint.Native.Function;
using Ultimate.Language.Jint.Native.Object;
using Ultimate.Language.Jint.Runtime;
using Ultimate.Language.Jint.Runtime.Descriptors;
using Ultimate.Language.Jint.Runtime.Interop;

namespace Ultimate.Language.Jint.Native.BigInt;

/// <summary>
/// https://tc39.es/ecma262/#sec-properties-of-the-bigint-constructor
/// </summary>
internal sealed class BigIntConstructor : FunctionInstance, IConstructor
{
    private static readonly JsString _functionName = new("BigInt");

    public BigIntConstructor(
        Engine engine,
        Realm realm,
        FunctionPrototype functionPrototype,
        ObjectPrototype objectPrototype)
        : base(engine, realm, _functionName)
    {
        _prototype = functionPrototype;
        PrototypeObject = new BigIntPrototype(engine, this, objectPrototype);
        _length = new PropertyDescriptor(JsNumber.PositiveOne, PropertyFlag.Configurable);
        _prototypeDescriptor = new PropertyDescriptor(PrototypeObject, PropertyFlag.AllForbidden);
    }

    protected override void Initialize()
    {
        var properties = new PropertyDictionary(2, checkExistingKeys: false)
        {
            ["asIntN"] = new(new ClrFunctionInstance(Engine, "asIntN", AsIntN, 2, PropertyFlag.Configurable), true, false, true),
            ["asUintN"] = new(new ClrFunctionInstance(Engine, "asUintN", AsUintN, 2, PropertyFlag.Configurable), true, false, true),
        };
        SetProperties(properties);
    }

    /// <summary>
    /// https://tc39.es/ecma262/#sec-bigint.asintn
    /// </summary>
    private JsValue AsIntN(JsValue thisObj, JsValue[] arguments)
    {
        var bits = (int) TypeConverter.ToIndex(_realm, arguments.At(0));
        var bigint = arguments.At(1).ToBigInteger(_engine);

        var mod = TypeConverter.BigIntegerModulo(bigint, BigInteger.Pow(2, bits));
        if (bits > 0 && mod >= BigInteger.Pow(2, bits - 1))
        {
            return (mod - BigInteger.Pow(2, bits));
        }

        return mod;
    }

    /// <summary>
    /// https://tc39.es/ecma262/#sec-bigint.asuintn
    /// </summary>
    private JsValue AsUintN(JsValue thisObj, JsValue[] arguments)
    {
        var bits = (int) TypeConverter.ToIndex(_realm, arguments.At(0));
        var bigint = arguments.At(1).ToBigInteger(_engine);

        var result = TypeConverter.BigIntegerModulo(bigint, BigInteger.Pow(2, bits));

        return result;
    }

    protected internal override JsValue Call(JsValue thisObject, JsValue[] arguments)
    {
        if (arguments.Length == 0)
        {
            return JsBigInt.Zero;
        }

        var prim = TypeConverter.ToPrimitive(arguments.At(0), Types.Number);
        if (prim.IsNumber())
        {
            return NumberToBigInt((JsNumber) prim);
        }

        return prim.ToBigInteger(_engine);
    }

    /// <summary>
    /// https://tc39.es/ecma262/#sec-numbertobigint
    /// </summary>
    private JsBigInt NumberToBigInt(JsNumber value)
    {
        if (TypeConverter.IsIntegralNumber(value._value))
        {
            return JsBigInt.Create((long) value._value);
        }

        ExceptionHelper.ThrowRangeError(_realm, "The number " + value + " cannot be converted to a BigInt because it is not an integer");
        return null;
    }

    /// <summary>
    /// https://tc39.es/ecma262/#sec-bigint-constructor-number-value
    /// </summary>
    ObjectInstance IConstructor.Construct(JsValue[] arguments, JsValue newTarget)
    {
        var value = arguments.Length > 0
            ? JsBigInt.Create(arguments[0].ToBigInteger(_engine))
            : JsBigInt.Zero;

        if (newTarget.IsUndefined())
        {
            return Construct(value);
        }

        var o = OrdinaryCreateFromConstructor(
            newTarget,
            static intrinsics => intrinsics.BigInt.PrototypeObject,
            static (engine, realm, state) => new BigIntInstance(engine, state!),
            value);

        return o;
    }

    public BigIntPrototype PrototypeObject { get; }

    public BigIntInstance Construct(JsBigInt value)
    {
        var instance = new BigIntInstance(Engine, value)
        {
            _prototype = PrototypeObject
        };

        return instance;
    }
}
