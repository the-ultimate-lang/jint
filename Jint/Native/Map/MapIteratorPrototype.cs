using Ultimate.Language.Jint.Collections;
using Ultimate.Language.Jint.Native.Iterator;
using Ultimate.Language.Jint.Native.Object;
using Ultimate.Language.Jint.Native.Symbol;
using Ultimate.Language.Jint.Runtime;
using Ultimate.Language.Jint.Runtime.Descriptors;
using Ultimate.Language.Jint.Runtime.Interop;

namespace Ultimate.Language.Jint.Native.Map;

/// <summary>
/// https://tc39.es/ecma262/#sec-%mapiteratorprototype%-object
/// </summary>
internal sealed class MapIteratorPrototype : IteratorPrototype
{
    internal MapIteratorPrototype(
        Engine engine,
        Realm realm,
        IteratorPrototype iteratorPrototype) : base(engine, realm, iteratorPrototype)
    {
    }

    protected override void Initialize()
    {
        var properties = new PropertyDictionary(1, checkExistingKeys: false)
        {
            [KnownKeys.Next] = new(new ClrFunctionInstance(Engine, "next", Next, 0, PropertyFlag.Configurable), true, false, true)
        };
        SetProperties(properties);

        var symbols = new SymbolDictionary(1)
        {
            [GlobalSymbolRegistry.ToStringTag] = new("Map Iterator", PropertyFlag.Configurable)
        };
        SetSymbols(symbols);
    }

    internal IteratorInstance ConstructEntryIterator(MapInstance map)
    {
        var instance = new MapIterator(Engine, map)
        {
            _prototype = this
        };

        return instance;
    }

    internal IteratorInstance ConstructKeyIterator(MapInstance map)
    {
        var instance = new IteratorInstance(Engine, map._map.Keys)
        {
            _prototype = this
        };

        return instance;
    }

    internal IteratorInstance ConstructValueIterator(MapInstance map)
    {
        var instance = new IteratorInstance(Engine, map._map.Values)
        {
            _prototype = this
        };

        return instance;
    }

    private sealed class MapIterator : IteratorInstance
    {
        private readonly OrderedDictionary<JsValue, JsValue> _map;

        private int _position;

        public MapIterator(Engine engine, MapInstance map) : base(engine)
        {
            _map = map._map;
            _position = 0;
        }

        public override bool TryIteratorStep(out ObjectInstance nextItem)
        {
            if (_position < _map.Count)
            {
                var key = _map.GetKey(_position);
                var value = _map[key];

                _position++;
                nextItem = IteratorResult.CreateKeyValueIteratorPosition(_engine, key, value);
                return true;
            }

            nextItem = IteratorResult.CreateKeyValueIteratorPosition(_engine);
            return false;
        }
    }
}
