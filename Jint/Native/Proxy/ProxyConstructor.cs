using Ultimate.Language.Jint.Collections;
using Ultimate.Language.Jint.Native.Function;
using Ultimate.Language.Jint.Native.Object;
using Ultimate.Language.Jint.Runtime;
using Ultimate.Language.Jint.Runtime.Descriptors;
using Ultimate.Language.Jint.Runtime.Interop;

namespace Ultimate.Language.Jint.Native.Proxy
{
    /// <summary>
    /// https://tc39.es/ecma262/#sec-proxy-constructor
    /// </summary>
    internal sealed class ProxyConstructor : FunctionInstance, IConstructor
    {
        private static readonly JsString _name = new JsString("Proxy");
        private static readonly JsString PropertyProxy = new JsString("proxy");
        private static readonly JsString PropertyRevoke = new JsString("revoke");

        internal ProxyConstructor(
            Engine engine,
            Realm realm)
            : base(engine, realm, _name)
        {
            _length = new PropertyDescriptor(2, PropertyFlag.Configurable);
        }

        protected override void Initialize()
        {
            var properties = new PropertyDictionary(1, checkExistingKeys: false)
            {
                ["revocable"] = new PropertyDescriptor(new ClrFunctionInstance(_engine, "revocable", Revocable, 2, PropertyFlag.Configurable), true, true, true)
            };
            SetProperties(properties);
        }

        protected internal override JsValue Call(JsValue thisObject, JsValue[] arguments)
        {
            ExceptionHelper.ThrowTypeError(_realm, "Constructor Proxy requires 'new'");
            return null;
        }

        ObjectInstance IConstructor.Construct(JsValue[] arguments, JsValue newTarget)
        {
            if (newTarget.IsUndefined())
            {
                ExceptionHelper.ThrowTypeError(_realm);
            }

            return Construct(arguments.At(0), arguments.At(1));
        }

        protected internal override ObjectInstance? GetPrototypeOf()
        {
            return _realm.Intrinsics.Function.Prototype;
        }

        /// <summary>
        /// https://tc39.es/ecma262/#sec-proxy-target-handler
        /// </summary>
        public ProxyInstance Construct(JsValue target, JsValue handler)
        {
            return ProxyCreate(target, handler);
        }

        /// <summary>
        /// https://tc39.es/ecma262/#sec-proxy.revocable
        /// </summary>
        private JsValue Revocable(JsValue thisObject, JsValue[] arguments)
        {
            var p = ProxyCreate(arguments.At(0), arguments.At(1));

            JsValue Revoke(JsValue thisObject, JsValue[] arguments)
            {
                p._handler = null;
                p._target = null!;
                return Undefined;
            }

            var result = _realm.Intrinsics.Object.Construct(System.Array.Empty<JsValue>());
            result.DefineOwnProperty(PropertyRevoke, new PropertyDescriptor(new ClrFunctionInstance(_engine, name: "", Revoke, 0, PropertyFlag.Configurable), PropertyFlag.ConfigurableEnumerableWritable));
            result.DefineOwnProperty(PropertyProxy, new PropertyDescriptor(p, PropertyFlag.ConfigurableEnumerableWritable));
            return result;
        }

        /// <summary>
        /// https://tc39.es/ecma262/#sec-proxycreate
        /// </summary>
        private ProxyInstance ProxyCreate(JsValue target, JsValue handler)
        {
            if (target is not ObjectInstance targetObject)
            {
                ExceptionHelper.ThrowTypeError(_realm, "Cannot create proxy with a non-object as target");
                return null;
            }

            if (handler is not ObjectInstance targetHandler)
            {
                ExceptionHelper.ThrowTypeError(_realm, "Cannot create proxy with a non-object as handler");
                return null;
            }

            var p = new ProxyInstance(Engine, targetObject, targetHandler);
            return p;
        }
    }
}
