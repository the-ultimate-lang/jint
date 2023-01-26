using Ultimate.Language.Jint.Native;

namespace Ultimate.Language.Jint.Runtime;

public sealed class PromiseRejectedException : JintException
{
    public PromiseRejectedException(JsValue value) : base($"Promise was rejected with value {value}")
    {
        RejectedValue = value;
    }

    public JsValue RejectedValue { get; }
}
