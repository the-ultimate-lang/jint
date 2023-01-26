using Ultimate.Language.Jint.Native.Object;
using Ultimate.Language.Jint.Runtime;

namespace Ultimate.Language.Jint.Native;

public abstract class Prototype : ObjectInstance
{
    internal readonly Realm _realm;

    private protected Prototype(Engine engine, Realm realm) : base(engine, type: InternalTypes.Object | InternalTypes.PlainObject)
    {
        _realm = realm;
    }
}
