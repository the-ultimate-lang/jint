using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using Ultimate.Language.Jint.Native.Object;
using Ultimate.Language.Jint.Runtime;
using Ultimate.Language.Jint.Runtime.Descriptors;

namespace Ultimate.Language.Jint.Native.Map;

internal sealed class MapInstance : ObjectInstance
{
    private readonly Realm _realm;
    internal readonly OrderedDictionary<JsValue, JsValue> _map;

    public MapInstance(Engine engine, Realm realm) : base(engine)
    {
        _realm = realm;
        _map = new OrderedDictionary<JsValue, JsValue>(SameValueZeroComparer.Instance);
    }

    public override PropertyDescriptor GetOwnProperty(JsValue property)
    {
        if (property == CommonProperties.Size)
        {
            return new PropertyDescriptor(_map.Count, PropertyFlag.AllForbidden);
        }

        return base.GetOwnProperty(property);
    }

    protected override bool TryGetProperty(JsValue property, [NotNullWhen(true)] out PropertyDescriptor? descriptor)
    {
        if (property == CommonProperties.Size)
        {
            descriptor = new PropertyDescriptor(_map.Count, PropertyFlag.AllForbidden);
            return true;
        }

        return base.TryGetProperty(property, out descriptor);
    }

    internal void Clear()
    {
        _map.Clear();
    }

    internal bool Has(JsValue key)
    {
        return _map.ContainsKey(key);
    }

    internal bool MapDelete(JsValue key)
    {
        return _map.Remove(key);
    }

    internal void MapSet(JsValue key, JsValue value)
    {
        if (key is JsNumber number && number.IsNegativeZero())
        {
            key = JsNumber.PositiveZero;
        }
        _map[key] = value;
    }

    internal void ForEach(ICallable callable, JsValue thisArg)
    {
        var args = _engine._jsValueArrayPool.RentArray(3);
        args[2] = this;

        for (var i = 0; i < _map.Count; i++)
        {
            args[0] = _map[i];
            args[1] = _map.GetKey(i);
            callable.Call(thisArg, args);
        }

        _engine._jsValueArrayPool.ReturnArray(args);
    }

    internal JsValue MapGet(JsValue key)
    {
        if (!_map.TryGetValue(key, out var value))
        {
            return Undefined;
        }

        return value;
    }

    internal ObjectInstance Iterator()
    {
        return _realm.Intrinsics.MapIteratorPrototype.ConstructEntryIterator(this);
    }

    internal ObjectInstance Keys()
    {
        return _realm.Intrinsics.MapIteratorPrototype.ConstructKeyIterator(this);
    }

    internal ObjectInstance Values()
    {
        return _realm.Intrinsics.MapIteratorPrototype.ConstructValueIterator(this);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal uint GetSize()
    {
        return (uint) _map.Count;
    }
}
