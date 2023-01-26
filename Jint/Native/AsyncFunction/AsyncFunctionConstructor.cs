using Ultimate.Language.Jint.Native.Function;
using Ultimate.Language.Jint.Native.Object;
using Ultimate.Language.Jint.Runtime;
using Ultimate.Language.Jint.Runtime.Descriptors;

namespace Ultimate.Language.Jint.Native.AsyncFunction;

/// <summary>
/// https://tc39.es/ecma262/#sec-async-function-constructor
/// </summary>
internal sealed class AsyncFunctionConstructor : FunctionInstance, IConstructor
{
    private static readonly JsString _functionName = new("AsyncFunction");

    public AsyncFunctionConstructor(Engine engine, Realm realm, FunctionConstructor functionConstructor) : base(engine, realm, _functionName)
    {
        PrototypeObject = new AsyncFunctionPrototype(engine, realm, this, functionConstructor.PrototypeObject);
        _prototype = functionConstructor;
        _prototypeDescriptor = new PropertyDescriptor(PrototypeObject, PropertyFlag.AllForbidden);
        _length = new PropertyDescriptor(JsNumber.PositiveOne, PropertyFlag.Configurable);
    }

    public AsyncFunctionPrototype PrototypeObject { get; }

    protected internal override JsValue Call(JsValue thisObject, JsValue[] arguments)
    {
        return Construct(arguments, thisObject);
    }

    ObjectInstance IConstructor.Construct(JsValue[] arguments, JsValue newTarget) => Construct(arguments, newTarget);

    private ObjectInstance Construct(JsValue[] arguments, JsValue newTarget)
    {
        var function = CreateDynamicFunction(
            this,
            newTarget,
            FunctionKind.Async,
            arguments);

        return function;
    }
}
