using Ultimate.Language.Jint.Collections;
using Ultimate.Language.Jint.Native.Iterator;
using Ultimate.Language.Jint.Native.Object;
using Ultimate.Language.Jint.Native.Symbol;
using Ultimate.Language.Jint.Runtime;
using Ultimate.Language.Jint.Runtime.Descriptors;
using Ultimate.Language.Jint.Runtime.Interop;

namespace Ultimate.Language.Jint.Native.String;

/// <summary>
/// https://tc39.es/ecma262/#sec-%stringiteratorprototype%-object
/// </summary>
internal sealed class StringIteratorPrototype : IteratorPrototype
{
    internal StringIteratorPrototype(
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
            [GlobalSymbolRegistry.ToStringTag] = new("String Iterator", PropertyFlag.Configurable)
        };
        SetSymbols(symbols);
    }

    public ObjectInstance Construct(string str)
    {
        var instance = new IteratorInstance.StringIterator(Engine, str)
        {
            _prototype = this
        };

        return instance;
    }
}
