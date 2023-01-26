using Ultimate.Language.Jint.Collections;
using Ultimate.Language.Jint.Native.Symbol;
using Ultimate.Language.Jint.Runtime;
using Ultimate.Language.Jint.Runtime.Descriptors;
using Ultimate.Language.Jint.Runtime.Interop;

namespace Ultimate.Language.Jint.Native.Iterator;

/// <summary>
/// https://tc39.es/ecma262/#sec-%iteratorprototype%-object
/// </summary>
internal class IteratorPrototype : Prototype
{
    internal IteratorPrototype(
        Engine engine,
        Realm realm,
        Prototype objectPrototype) : base(engine, realm)
    {
        _prototype = objectPrototype;
    }

    protected override void Initialize()
    {
        var symbols = new SymbolDictionary(1)
        {
            [GlobalSymbolRegistry.Iterator] = new(new ClrFunctionInstance(Engine, "[Symbol.iterator]", ToIterator, 0, PropertyFlag.Configurable), true, false, true),
        };
        SetSymbols(symbols);
    }

    private static JsValue ToIterator(JsValue thisObj, JsValue[] arguments)
    {
        return thisObj;
    }

    internal JsValue Next(JsValue thisObj, JsValue[] arguments)
    {
        var iterator = thisObj as IteratorInstance;
        if (iterator is null)
        {
            ExceptionHelper.ThrowTypeError(_engine.Realm);
        }

        iterator.TryIteratorStep(out var result);
        return result;
    }
}
