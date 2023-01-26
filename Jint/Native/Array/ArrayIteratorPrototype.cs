using Ultimate.Language.Jint.Collections;
using Ultimate.Language.Jint.Native.Iterator;
using Ultimate.Language.Jint.Native.Object;
using Ultimate.Language.Jint.Native.Symbol;
using Ultimate.Language.Jint.Native.TypedArray;
using Ultimate.Language.Jint.Runtime;
using Ultimate.Language.Jint.Runtime.Descriptors;
using Ultimate.Language.Jint.Runtime.Interop;

namespace Ultimate.Language.Jint.Native.Array;

/// <summary>
/// https://tc39.es/ecma262/#sec-%arrayiteratorprototype%-object
/// </summary>
internal sealed class ArrayIteratorPrototype : IteratorPrototype
{
    internal ArrayIteratorPrototype(
        Engine engine,
        Realm realm,
        IteratorPrototype objectPrototype) : base(engine, realm, objectPrototype)
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
            [GlobalSymbolRegistry.ToStringTag] = new("Array Iterator", PropertyFlag.Configurable)
        };
        SetSymbols(symbols);
    }

    internal IteratorInstance Construct(ObjectInstance array, ArrayIteratorType kind)
    {
        var instance = new ArrayLikeIterator(Engine, array, kind)
        {
            _prototype = this
        };

        return instance;
    }

    private sealed class ArrayLikeIterator : IteratorInstance
    {
        private readonly ArrayIteratorType _kind;
        private readonly TypedArrayInstance? _typedArray;
        private readonly ArrayOperations? _operations;
        private uint _position;
        private bool _closed;

        public ArrayLikeIterator(Engine engine, ObjectInstance objectInstance, ArrayIteratorType kind) : base(engine)
        {
            _kind = kind;
            _typedArray = objectInstance as TypedArrayInstance;
            if (_typedArray is null)
            {
                _operations = ArrayOperations.For(objectInstance);
            }

            _position = 0;
        }

        public override bool TryIteratorStep(out ObjectInstance nextItem)
        {
            uint len;
            if (_typedArray is not null)
            {
                _typedArray._viewedArrayBuffer.AssertNotDetached();
                len = _typedArray.Length;
            }
            else
            {
                len = _operations!.GetLength();
            }

            if (!_closed && _position < len)
            {
                if (_typedArray is not null)
                {
                    nextItem = _kind switch
                    {
                        ArrayIteratorType.Key => IteratorResult.CreateValueIteratorPosition(_engine, JsNumber.Create(_position)),
                        ArrayIteratorType.Value => IteratorResult.CreateValueIteratorPosition(_engine, _typedArray[(int) _position]),
                        _ => IteratorResult.CreateKeyValueIteratorPosition(_engine, JsNumber.Create(_position), _typedArray[(int) _position])
                    };
                }
                else
                {
                    _operations!.TryGetValue(_position, out var value);
                    if (_kind == ArrayIteratorType.Key)
                    {
                        nextItem = IteratorResult.CreateValueIteratorPosition(_engine, JsNumber.Create(_position));
                    }
                    else if (_kind == ArrayIteratorType.Value)
                    {
                        nextItem = IteratorResult.CreateValueIteratorPosition(_engine, value);
                    }
                    else
                    {
                        nextItem = IteratorResult.CreateKeyValueIteratorPosition(_engine, JsNumber.Create(_position), value);
                    }
                }

                _position++;
                return true;
            }

            _closed = true;
            nextItem = IteratorResult.CreateKeyValueIteratorPosition(_engine);
            return false;
        }
    }
}
