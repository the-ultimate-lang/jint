using Ultimate.Language.Jint.Runtime;

namespace Ultimate.Language.Jint.Native;

public interface IPrimitiveInstance
{
    Types Type { get; }
    JsValue PrimitiveValue { get; }
}
