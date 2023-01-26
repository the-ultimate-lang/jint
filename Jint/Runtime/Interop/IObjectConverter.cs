using System.Diagnostics.CodeAnalysis;
using Ultimate.Language.Jint.Native;

namespace Ultimate.Language.Jint.Runtime.Interop
{
    /// <summary>
    /// When implemented, converts a CLR value to a <see cref="JsValue"/> instance
    /// </summary>
    public interface IObjectConverter
    {
        bool TryConvert(Engine engine, object value, [NotNullWhen(true)] out JsValue? result);
    }
}
