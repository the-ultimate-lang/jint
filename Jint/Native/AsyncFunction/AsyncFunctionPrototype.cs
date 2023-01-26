using Ultimate.Language.Jint.Collections;
using Ultimate.Language.Jint.Native.Function;
using Ultimate.Language.Jint.Native.Symbol;
using Ultimate.Language.Jint.Runtime;
using Ultimate.Language.Jint.Runtime.Descriptors;

namespace Ultimate.Language.Jint.Native.AsyncFunction;

/// <summary>
/// https://tc39.es/ecma262/#sec-async-function-prototype-properties
/// </summary>
internal sealed class AsyncFunctionPrototype : Prototype
{
    private readonly AsyncFunctionConstructor _constructor;

    public AsyncFunctionPrototype(
        Engine engine,
        Realm realm,
        AsyncFunctionConstructor constructor,
        FunctionPrototype objectPrototype) : base(engine, realm)
    {
        _constructor = constructor;
        _prototype = objectPrototype;
    }

    protected override void Initialize()
    {
        var properties = new PropertyDictionary(1, checkExistingKeys: false)
        {
            [KnownKeys.Constructor] = new(_constructor, PropertyFlag.NonEnumerable),
        };
        SetProperties(properties);

        var symbols = new SymbolDictionary(1)
        {
            [GlobalSymbolRegistry.ToStringTag] = new("AsyncFunction", PropertyFlag.Configurable)
        };
        SetSymbols(symbols);
    }

}
