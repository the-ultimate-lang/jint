using Ultimate.Language.Jint.Native.Object;
using Ultimate.Language.Jint.Runtime;

namespace Ultimate.Language.Jint.Native.Boolean;

internal class BooleanInstance : ObjectInstance, IPrimitiveInstance
{
    public BooleanInstance(Engine engine, JsBoolean value)
        : base(engine, ObjectClass.Boolean)
    {
        BooleanData = value;
    }

    Types IPrimitiveInstance.Type => Types.Boolean;

    JsValue IPrimitiveInstance.PrimitiveValue => BooleanData;

    public JsValue BooleanData { get; }
}
