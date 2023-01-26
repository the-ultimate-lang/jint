using Ultimate.Language.Jint.Collections;
using Ultimate.Language.Jint.Native.Iterator;
using Ultimate.Language.Jint.Native.Object;
using Ultimate.Language.Jint.Native.Symbol;
using Ultimate.Language.Jint.Runtime;
using Ultimate.Language.Jint.Runtime.Descriptors;
using Ultimate.Language.Jint.Runtime.Interop;

namespace Ultimate.Language.Jint.Native.RegExp;

/// <summary>
/// https://tc39.es/ecma262/#sec-%regexpstringiteratorprototype%-object
/// </summary>
internal sealed class RegExpStringIteratorPrototype : IteratorPrototype
{
    internal RegExpStringIteratorPrototype(
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
            [GlobalSymbolRegistry.ToStringTag] = new("RegExp String Iterator", PropertyFlag.Configurable)
        };
        SetSymbols(symbols);
    }

    internal IteratorInstance Construct(ObjectInstance iteratingRegExp, string iteratedString, bool global, bool unicode)
    {
        var instance = new IteratorInstance.RegExpStringIterator(Engine, iteratingRegExp, iteratedString, global, unicode)
        {
            _prototype = this
        };

        return instance;
    }
}
