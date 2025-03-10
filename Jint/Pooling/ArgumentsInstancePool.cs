using Ultimate.Language.Jint.Native;
using Ultimate.Language.Jint.Native.Argument;
using Ultimate.Language.Jint.Native.Function;
using Ultimate.Language.Jint.Runtime.Environments;
using Ultimate.Language.Jint.Runtime.References;

namespace Ultimate.Language.Jint.Pooling
{
    /// <summary>
    /// Cache reusable <see cref="Reference" /> instances as we allocate them a lot.
    /// </summary>
    internal sealed class ArgumentsInstancePool
    {
        private const int PoolSize = 10;
        private readonly Engine _engine;
        private readonly ObjectPool<ArgumentsInstance> _pool;

        public ArgumentsInstancePool(Engine engine)
        {
            _engine = engine;
            _pool = new ObjectPool<ArgumentsInstance>(Factory, PoolSize);
        }

        private ArgumentsInstance Factory()
        {
            return new ArgumentsInstance(_engine)
            {
                _prototype = _engine.Realm.Intrinsics.Object.PrototypeObject
            };
        }

        public ArgumentsInstance Rent(JsValue[] argumentsList) => Rent(null, null, argumentsList, null, false);

        public ArgumentsInstance Rent(
            FunctionInstance? func,
            Key[]? formals,
            JsValue[] argumentsList,
            DeclarativeEnvironmentRecord? env,
            bool hasRestParameter)
        {
            var obj = _pool.Allocate();
            obj.Prepare(func!, formals!, argumentsList, env!, hasRestParameter);
            return obj;
        }

        public void Return(ArgumentsInstance instance)
        {
            if (ReferenceEquals(instance, null))
            {
                return;
            }
            _pool.Free(instance);;
        }
    }
}
