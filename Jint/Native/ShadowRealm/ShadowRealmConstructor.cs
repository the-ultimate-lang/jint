using Ultimate.Language.Jint.Native.Function;
using Ultimate.Language.Jint.Native.Object;
using Ultimate.Language.Jint.Runtime;
using Ultimate.Language.Jint.Runtime.Descriptors;
using Ultimate.Language.Jint.Runtime.Environments;

namespace Ultimate.Language.Jint.Native.ShadowRealm;

/// <summary>
/// https://tc39.es/proposal-shadowrealm/#sec-properties-of-the-shadowRealm-constructor
/// </summary>
public sealed class ShadowRealmConstructor : FunctionInstance, IConstructor
{
    private static readonly JsString _functionName = new("ShadowRealm");

    internal ShadowRealmConstructor(
        Engine engine,
        Realm realm,
        FunctionPrototype functionPrototype,
        ObjectPrototype objectPrototype)
        : base(engine, realm, _functionName)
    {
        _prototype = functionPrototype;
        PrototypeObject = new ShadowRealmPrototype(engine, realm, this, objectPrototype);
        _length = new PropertyDescriptor(0, PropertyFlag.Configurable);
        _prototypeDescriptor = new PropertyDescriptor(PrototypeObject, PropertyFlag.AllForbidden);
    }

    private ShadowRealmPrototype PrototypeObject { get; }

    protected internal override JsValue Call(JsValue thisObject, JsValue[] arguments)
    {
        ExceptionHelper.ThrowTypeError(_realm, "Constructor ShadowRealm requires 'new'");
        return null;
    }

    public ShadowRealm Construct()
    {
        return Construct(PrototypeObject);
    }

    private ShadowRealm Construct(JsValue newTarget)
    {
        var realmRec = _engine._host.CreateRealm();

        var o = OrdinaryCreateFromConstructor(
            newTarget,
            static intrinsics => intrinsics.ShadowRealm.PrototypeObject,
            static (engine, _, realmRec) =>
            {
                var context = new ExecutionContext(
                    scriptOrModule: null,
                    lexicalEnvironment: realmRec!.GlobalEnv,
                    variableEnvironment: realmRec.GlobalEnv,
                    privateEnvironment: null,
                    realm: realmRec,
                    function: null);

                return new ShadowRealm(engine, context, realmRec);
            },
            realmRec);

        // this are currently handled as part of realm construction
        // SetRealmGlobalObject(realmRec, Undefined, Undefined);
        // SetDefaultGlobalBindings(o._ShadowRealm);

        _engine._host.InitializeShadowRealm(o._shadowRealm);

        return o;
    }


    ObjectInstance IConstructor.Construct(JsValue[] arguments, JsValue newTarget)
    {
        if (newTarget.IsUndefined())
        {
            ExceptionHelper.ThrowTypeError(_realm);
        }

        return Construct(newTarget);
    }
}
