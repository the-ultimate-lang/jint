using Ultimate.Language.Jint.Collections;
using Ultimate.Language.Jint.Native.Function;
using Ultimate.Language.Jint.Native.Iterator;
using Ultimate.Language.Jint.Native.Object;
using Ultimate.Language.Jint.Native.Symbol;
using Ultimate.Language.Jint.Runtime;
using Ultimate.Language.Jint.Runtime.Descriptors;
using Ultimate.Language.Jint.Runtime.Interop;

namespace Ultimate.Language.Jint.Native.Map;

internal sealed class MapConstructor : FunctionInstance, IConstructor
{
    private static readonly JsString _functionName = new("Map");

    internal MapConstructor(
        Engine engine,
        Realm realm,
        FunctionPrototype functionPrototype,
        ObjectPrototype objectPrototype)
        : base(engine, realm, _functionName)
    {
        _prototype = functionPrototype;
        PrototypeObject = new MapPrototype(engine, realm, this, objectPrototype);
        _length = new PropertyDescriptor(0, PropertyFlag.Configurable);
        _prototypeDescriptor = new PropertyDescriptor(PrototypeObject, PropertyFlag.AllForbidden);
    }

    private MapPrototype PrototypeObject { get; }

    protected override void Initialize()
    {
        var symbols = new SymbolDictionary(1)
        {
            [GlobalSymbolRegistry.Species] = new GetSetPropertyDescriptor(get: new ClrFunctionInstance(_engine, "get [Symbol.species]", Species, 0, PropertyFlag.Configurable), set: Undefined, PropertyFlag.Configurable)
        };
        SetSymbols(symbols);
    }

    private static JsValue Species(JsValue thisObject, JsValue[] arguments)
    {
        return thisObject;
    }

    protected internal override JsValue Call(JsValue thisObject, JsValue[] arguments)
    {
        ExceptionHelper.ThrowTypeError(_realm, "Constructor Map requires 'new'");
        return null;
    }

    /// <summary>
    /// https://tc39.es/ecma262/#sec-map-iterable
    /// </summary>
    ObjectInstance IConstructor.Construct(JsValue[] arguments, JsValue newTarget)
    {
        if (newTarget.IsUndefined())
        {
            ExceptionHelper.ThrowTypeError(_realm);
        }

        var map = OrdinaryCreateFromConstructor(
            newTarget,
            static intrinsics => intrinsics.Map.PrototypeObject,
            static (Engine engine, Realm realm, object? _) => new MapInstance(engine, realm));

        if (arguments.Length > 0 && !arguments[0].IsNullOrUndefined())
        {
            var adder = map.Get("set");
            var iterator = arguments.At(0).GetIterator(_realm);

            IteratorProtocol.AddEntriesFromIterable(map, iterator, adder);
        }

        return map;
    }
}
