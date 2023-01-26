using Ultimate.Language.Jint.Native.Object;

namespace Ultimate.Language.Jint.Native;

internal interface IConstructor
{
    ObjectInstance Construct(JsValue[] arguments, JsValue newTarget);
}
