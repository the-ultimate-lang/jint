using Ultimate.Language.Jint.Native.ArrayBuffer;
using Ultimate.Language.Jint.Native.Object;

namespace Ultimate.Language.Jint.Native.DataView;

/// <summary>
/// https://tc39.es/ecma262/#sec-properties-of-dataview-instances
/// </summary>
internal sealed class DataViewInstance : ObjectInstance
{
    internal ArrayBufferInstance? _viewedArrayBuffer;
    internal uint _byteLength;
    internal uint _byteOffset;

    internal DataViewInstance(Engine engine) : base(engine)
    {
    }
}
