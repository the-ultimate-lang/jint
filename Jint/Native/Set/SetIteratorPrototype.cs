using Ultimate.Language.Jint.Collections;
using Ultimate.Language.Jint.Native.Iterator;
using Ultimate.Language.Jint.Native.Object;
using Ultimate.Language.Jint.Native.Symbol;
using Ultimate.Language.Jint.Runtime;
using Ultimate.Language.Jint.Runtime.Descriptors;
using Ultimate.Language.Jint.Runtime.Interop;

namespace Ultimate.Language.Jint.Native.Set;

/// <summary>
/// https://tc39.es/ecma262/#sec-%setiteratorprototype%-object
/// </summary>
internal sealed class SetIteratorPrototype : IteratorPrototype
{
    internal SetIteratorPrototype(
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
            [GlobalSymbolRegistry.ToStringTag] = new("Set Iterator", PropertyFlag.Configurable)
        };
        SetSymbols(symbols);
    }

    internal IteratorInstance ConstructEntryIterator(SetInstance set)
    {
        var instance = new SetEntryIterator(Engine, set);
        return instance;
    }

    internal IteratorInstance ConstructValueIterator(SetInstance set)
    {
        var instance = new SetValueIterator(Engine, set._set._list);
        return instance;
    }

    private sealed class SetEntryIterator : IteratorInstance
    {
        private readonly SetInstance _set;
        private int _position;

        public SetEntryIterator(Engine engine, SetInstance set) : base(engine)
        {
            _prototype = engine.Realm.Intrinsics.SetIteratorPrototype;
            _set = set;
            _position = 0;
        }

        public override bool TryIteratorStep(out ObjectInstance nextItem)
        {
            if (_position < _set._set._list.Count)
            {
                var value = _set._set[_position];
                _position++;
                nextItem = IteratorResult.CreateKeyValueIteratorPosition(_engine, value, value);
                return true;
            }

            nextItem = IteratorResult.CreateKeyValueIteratorPosition(_engine);
            return false;
        }
    }

    private sealed class SetValueIterator : IteratorInstance
    {
        private readonly List<JsValue> _values;
        private int _position;
        private bool _closed;

        public SetValueIterator(Engine engine, List<JsValue> values) : base(engine)
        {
            _prototype = engine.Realm.Intrinsics.SetIteratorPrototype;
            _values = values;
            _position = 0;
        }

        public override bool TryIteratorStep(out ObjectInstance nextItem)
        {
            if (!_closed && _position < _values.Count)
            {
                var value = _values[_position];
                _position++;
                nextItem = IteratorResult.CreateValueIteratorPosition(_engine, value);
                return true;
            }

            _closed = true;
            nextItem = IteratorResult.CreateKeyValueIteratorPosition(_engine);
            return false;
        }
    }
}
