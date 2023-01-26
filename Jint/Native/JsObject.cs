using Ultimate.Language.Jint.Native.Object;
using Ultimate.Language.Jint.Runtime;

namespace Ultimate.Language.Jint.Native;

/// <summary>
/// Dynamically constructed JavaScript object instance.
/// </summary>
public sealed class JsObject : ObjectInstance
{
    public JsObject(Engine engine) : base(engine, type: InternalTypes.Object | InternalTypes.PlainObject)
    {
    }
}
