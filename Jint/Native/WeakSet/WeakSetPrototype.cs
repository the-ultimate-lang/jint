using Ultimate.Language.Jint.Collections;
using Ultimate.Language.Jint.Native.Object;
using Ultimate.Language.Jint.Native.Symbol;
using Ultimate.Language.Jint.Runtime;
using Ultimate.Language.Jint.Runtime.Descriptors;
using Ultimate.Language.Jint.Runtime.Interop;

namespace Ultimate.Language.Jint.Native.WeakSet;

/// <summary>
/// https://tc39.es/ecma262/#sec-weakset-objects
/// </summary>
internal sealed class WeakSetPrototype : Prototype
{
    private readonly WeakSetConstructor _constructor;
    internal ClrFunctionInstance _originalAddFunction = null!;

    internal WeakSetPrototype(
        Engine engine,
        Realm realm,
        WeakSetConstructor constructor,
        ObjectPrototype prototype) : base(engine, realm)
    {
        _prototype = prototype;
        _constructor = constructor;
    }

    protected override void Initialize()
    {
        _originalAddFunction = new ClrFunctionInstance(Engine, "add", Add, 1, PropertyFlag.Configurable);

        const PropertyFlag PropertyFlags = PropertyFlag.Configurable | PropertyFlag.Writable;
        var properties = new PropertyDictionary(5, checkExistingKeys: false)
        {
            ["length"] = new(0, PropertyFlag.Configurable),
            ["constructor"] = new(_constructor, PropertyFlag.NonEnumerable),
            ["delete"] = new(new ClrFunctionInstance(Engine, "delete", Delete, 1, PropertyFlag.Configurable), PropertyFlags),
            ["add"] = new(_originalAddFunction, PropertyFlags),
            ["has"] = new(new ClrFunctionInstance(Engine, "has", Has, 1, PropertyFlag.Configurable), PropertyFlags),
        };
        SetProperties(properties);

        var symbols = new SymbolDictionary(1)
        {
            [GlobalSymbolRegistry.ToStringTag] = new("WeakSet", false, false, true)
        };
        SetSymbols(symbols);
    }

    private JsValue Add(JsValue thisObj, JsValue[] arguments)
    {
        var set = AssertWeakSetInstance(thisObj);
        set.WeakSetAdd(arguments.At(0));
        return thisObj;
    }

    private JsValue Delete(JsValue thisObj, JsValue[] arguments)
    {
        var set = AssertWeakSetInstance(thisObj);
        return set.WeakSetDelete(arguments.At(0)) ? JsBoolean.True : JsBoolean.False;
    }

    private JsValue Has(JsValue thisObj, JsValue[] arguments)
    {
        var set = AssertWeakSetInstance(thisObj);
        return set.WeakSetHas(arguments.At(0)) ? JsBoolean.True : JsBoolean.False;
    }

    private WeakSetInstance AssertWeakSetInstance(JsValue thisObj)
    {
        var set = thisObj as WeakSetInstance;
        if (set is null)
        {
            ExceptionHelper.ThrowTypeError(_realm, "object must be a WeakSet");
        }

        return set;
    }
}
