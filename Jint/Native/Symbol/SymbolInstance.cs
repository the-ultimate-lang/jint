using Ultimate.Language.Jint.Native.Object;
using Ultimate.Language.Jint.Runtime;

namespace Ultimate.Language.Jint.Native.Symbol;

internal sealed class SymbolInstance : ObjectInstance, IPrimitiveInstance
{
    internal SymbolInstance(
        Engine engine,
        SymbolPrototype prototype,
        JsSymbol symbol) : base(engine)
    {
        _prototype = prototype;
        SymbolData = symbol;
    }

    Types IPrimitiveInstance.Type => Types.Symbol;

    JsValue IPrimitiveInstance.PrimitiveValue => SymbolData;

    public JsSymbol SymbolData { get; }
}
