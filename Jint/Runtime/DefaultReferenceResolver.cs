using Ultimate.Language.Jint.Native;
using Ultimate.Language.Jint.Runtime.Interop;
using Ultimate.Language.Jint.Runtime.References;

namespace Ultimate.Language.Jint.Runtime
{
    internal sealed class DefaultReferenceResolver : IReferenceResolver
    {
        public static readonly DefaultReferenceResolver Instance = new();

        private DefaultReferenceResolver()
        {
        }

        public bool TryUnresolvableReference(Engine engine, Reference reference, out JsValue value)
        {
            value = JsValue.Undefined;
            return false;
        }

        public bool TryPropertyReference(Engine engine, Reference reference, ref JsValue value)
        {
            return false;
        }

        public bool TryGetCallable(Engine engine, object callee, out JsValue value)
        {
            value = JsValue.Undefined;
            return false;
        }

        public bool CheckCoercible(JsValue value)
        {
            return false;
        }
    }
}
