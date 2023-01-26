using System.Diagnostics.CodeAnalysis;
using Ultimate.Language.Jint.Native.Object;

namespace Ultimate.Language.Jint.Native.RegExp
{
    internal static class RegExpExtensions
    {
        internal static bool TryGetDefaultRegExpExec(this ObjectInstance? o, [NotNullWhen(true)] out Func<JsValue, JsValue[], JsValue>? exec)
        {
            if (o is RegExpPrototype prototype)
            {
                return prototype.TryGetDefaultExec(prototype, out exec);
            }

            if (o is RegExpInstance instance)
            {
                exec = default;
                return instance.Properties == null
                       && TryGetDefaultRegExpExec(instance.Prototype, out exec);
            }

            exec = default;
            return false;
        }
    }
}
