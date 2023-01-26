using System.Runtime.CompilerServices;

using Ultimate.Language.Jint.Native.Object;
using Ultimate.Language.Jint.Runtime;

namespace Ultimate.Language.Jint.Native.WeakMap;

internal sealed class WeakMapInstance : ObjectInstance
{
    private readonly ConditionalWeakTable<JsValue, JsValue> _table;

    public WeakMapInstance(Engine engine) : base(engine)
    {
        _table = new ConditionalWeakTable<JsValue, JsValue>();
    }

    internal bool WeakMapHas(JsValue key)
    {
        return _table.TryGetValue(key, out _);
    }

    internal bool WeakMapDelete(JsValue key)
    {
        return _table.Remove(key);
    }

    internal void WeakMapSet(JsValue key, JsValue value)
    {
        if (!key.CanBeHeldWeakly(_engine.GlobalSymbolRegistry))
        {
            ExceptionHelper.ThrowTypeError(_engine.Realm, "WeakMap key must be an object, got " + key);
        }

#if NETSTANDARD2_1_OR_GREATER
         _table.AddOrUpdate(key, value);
#else
        _table.Remove(key);
        _table.Add(key, value);
#endif
    }

    internal JsValue WeakMapGet(JsValue key)
    {
        if (!_table.TryGetValue(key, out var value))
        {
            return Undefined;
        }

        return value;
    }

}
