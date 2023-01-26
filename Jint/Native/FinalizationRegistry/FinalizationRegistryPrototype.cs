using Ultimate.Language.Jint.Collections;
using Ultimate.Language.Jint.Native.Object;
using Ultimate.Language.Jint.Native.Symbol;
using Ultimate.Language.Jint.Runtime;
using Ultimate.Language.Jint.Runtime.Descriptors;
using Ultimate.Language.Jint.Runtime.Interop;

namespace Ultimate.Language.Jint.Native.FinalizationRegistry;

/// <summary>
/// https://tc39.es/ecma262/#sec-properties-of-the-finalization-registry-prototype-object
/// </summary>
internal sealed class FinalizationRegistryPrototype : Prototype
{
    private readonly FinalizationRegistryConstructor _constructor;

    public FinalizationRegistryPrototype(
        Engine engine,
        Realm realm,
        FinalizationRegistryConstructor constructor,
        ObjectPrototype objectPrototype) : base(engine, realm)
    {
        _constructor = constructor;
        _prototype = objectPrototype;
    }

    protected override void Initialize()
    {
        const PropertyFlag PropertyFlags = PropertyFlag.NonEnumerable;
        var properties = new PropertyDictionary(4, checkExistingKeys: false)
        {
            [KnownKeys.Constructor] = new(_constructor, PropertyFlag.NonEnumerable),
            ["register"] = new(new ClrFunctionInstance(Engine, "register", Register, 2, PropertyFlag.Configurable), PropertyFlags),
            ["unregister"] = new(new ClrFunctionInstance(Engine, "unregister", Unregister, 1, PropertyFlag.Configurable), PropertyFlags),
            ["cleanupSome"] = new(new ClrFunctionInstance(Engine, "cleanupSome", CleanupSome, 0, PropertyFlag.Configurable), PropertyFlags),
        };
        SetProperties(properties);

        var symbols = new SymbolDictionary(1) { [GlobalSymbolRegistry.ToStringTag] = new("FinalizationRegistry", PropertyFlag.Configurable) };
        SetSymbols(symbols);
    }

    /// <summary>
    /// https://tc39.es/ecma262/#sec-finalization-registry.prototype.register
    /// </summary>
    private JsValue Register(JsValue thisObj, JsValue[] arguments)
    {
        var finalizationRegistry = AssertFinalizationRegistryInstance(thisObj);

        var target = arguments.At(0);
        var heldValue = arguments.At(1);
        var unregisterToken = arguments.At(2);

        if (!target.CanBeHeldWeakly(_engine.GlobalSymbolRegistry))
        {
            ExceptionHelper.ThrowTypeError(_realm, "target must be an object or symbol");
        }

        if (SameValue(target, heldValue))
        {
            ExceptionHelper.ThrowTypeError(_realm, "target and holdings must not be same");
        }

        if (!unregisterToken.CanBeHeldWeakly(_engine.GlobalSymbolRegistry))
        {
            if (!unregisterToken.IsUndefined())
            {
                ExceptionHelper.ThrowTypeError(_realm, unregisterToken + " must be an object");
            }

        }
        var cell = new Cell(target, heldValue, unregisterToken);
        finalizationRegistry.AddCell(cell);
        return Undefined;
    }

    /// <summary>
    /// https://tc39.es/ecma262/#sec-finalization-registry.prototype.unregister
    /// </summary>
    private JsValue Unregister(JsValue thisObj, JsValue[] arguments)
    {
        var finalizationRegistry = AssertFinalizationRegistryInstance(thisObj);

        var unregisterToken = arguments.At(0);

        if (!unregisterToken.CanBeHeldWeakly(_engine.GlobalSymbolRegistry))
        {
            ExceptionHelper.ThrowTypeError(_realm, unregisterToken + " must be an object or symbol");
        }

        return finalizationRegistry.Remove(unregisterToken);
    }

    private JsValue CleanupSome(JsValue thisObj, JsValue[] arguments)
    {
        var finalizationRegistry = AssertFinalizationRegistryInstance(thisObj);
        var callback = arguments.At(0);

        if (!callback.IsUndefined() && callback is not ICallable)
        {
            ExceptionHelper.ThrowTypeError(_realm, callback + " must be callable");
        }

        finalizationRegistry.CleanupFinalizationRegistry(callback as ICallable);

        return Undefined;
    }

    private FinalizationRegistryInstance AssertFinalizationRegistryInstance(JsValue thisObj)
    {
        if (thisObj is not FinalizationRegistryInstance finalizationRegistryInstance)
        {
            ExceptionHelper.ThrowTypeError(_realm, "object must be a FinalizationRegistry");
            return null;
        }

        return finalizationRegistryInstance;
    }
}

