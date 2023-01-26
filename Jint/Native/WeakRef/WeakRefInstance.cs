using Ultimate.Language.Jint.Native.Object;

namespace Ultimate.Language.Jint.Native.WeakRef;

/// <summary>
/// https://tc39.es/ecma262/#sec-properties-of-weak-ref-instances
/// </summary>
internal sealed class WeakRefInstance : ObjectInstance
{
    private readonly WeakReference<JsValue> _weakRefTarget;

    public WeakRefInstance(Engine engine, JsValue target) : base(engine)
    {
        _weakRefTarget = new WeakReference<JsValue>(target);
    }

    public JsValue WeakRefDeref()
    {
        if (_weakRefTarget.TryGetTarget(out var target))
        {
            _engine.AddToKeptObjects(target);
            return target;
        }

        return Undefined;
    }
}
