using Ultimate.Language.Jint.Collections;
using Ultimate.Language.Jint.Native.Function;
using Ultimate.Language.Jint.Native.Object;
using Ultimate.Language.Jint.Native.Symbol;
using Ultimate.Language.Jint.Runtime;
using Ultimate.Language.Jint.Runtime.Descriptors;
using Ultimate.Language.Jint.Runtime.Interop;

namespace Ultimate.Language.Jint.Native.Set;

internal sealed class SetConstructor : FunctionInstance, IConstructor
{
    private static readonly JsString _functionName = new("Set");

    internal SetConstructor(
        Engine engine,
        Realm realm,
        FunctionPrototype functionPrototype,
        ObjectPrototype objectPrototype)
        : base(engine, realm, _functionName, FunctionThisMode.Global)
    {
        _prototype = functionPrototype;
        PrototypeObject = new SetPrototype(engine, realm, this, objectPrototype);
        _length = new PropertyDescriptor(0, PropertyFlag.Configurable);
        _prototypeDescriptor = new PropertyDescriptor(PrototypeObject, PropertyFlag.AllForbidden);
    }

    private SetPrototype PrototypeObject { get; }

    protected override void Initialize()
    {
        var symbols = new SymbolDictionary(1)
        {
            [GlobalSymbolRegistry.Species] = new GetSetPropertyDescriptor(get: new ClrFunctionInstance(_engine, "get [Symbol.species]", Species, 0, PropertyFlag.Configurable), set: Undefined, PropertyFlag.Configurable)
        };

        SetSymbols(symbols);
    }

    private static JsValue Species(JsValue thisObject, JsValue[] arguments)
    {
        return thisObject;
    }

    protected internal override JsValue Call(JsValue thisObject, JsValue[] arguments)
    {
        ExceptionHelper.ThrowTypeError(_engine.Realm, "Constructor Set requires 'new'");
        return null;
    }

    /// <summary>
    /// https://tc39.es/ecma262/#sec-set-iterable
    /// </summary>
    ObjectInstance IConstructor.Construct(JsValue[] arguments, JsValue newTarget)
    {
        if (newTarget.IsUndefined())
        {
            ExceptionHelper.ThrowTypeError(_engine.Realm);
        }

        var set = OrdinaryCreateFromConstructor(
            newTarget,
            static intrinsics => intrinsics.Set.PrototypeObject,
            static (Engine engine, Realm _, object? _) => new SetInstance(engine));

        if (arguments.Length > 0 && !arguments[0].IsNullOrUndefined())
        {
            var adderValue = set.Get("add");
            var adder = adderValue as ICallable;
            if (adder is null)
            {
                ExceptionHelper.ThrowTypeError(_engine.Realm, "add must be callable");
            }

            var iterable = arguments.At(0).GetIterator(_realm);

            try
            {
                var args = new JsValue[1];
                do
                {
                    if (!iterable.TryIteratorStep(out var next))
                    {
                        return set;
                    }

                    var nextValue = next.Get(CommonProperties.Value);
                    args[0] = nextValue;
                    adder.Call(set, args);
                } while (true);
            }
            catch
            {
                iterable.Close(CompletionType.Throw);
                throw;
            }
        }

        return set;
    }
}
