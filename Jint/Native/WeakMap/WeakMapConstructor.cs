using Ultimate.Language.Jint.Native.Function;
using Ultimate.Language.Jint.Native.Iterator;
using Ultimate.Language.Jint.Native.Object;
using Ultimate.Language.Jint.Runtime;
using Ultimate.Language.Jint.Runtime.Descriptors;

namespace Ultimate.Language.Jint.Native.WeakMap
{
    internal sealed class WeakMapConstructor : FunctionInstance, IConstructor
    {
        private static readonly JsString _functionName = new JsString("WeakMap");

        internal WeakMapConstructor(
            Engine engine,
            Realm realm,
            FunctionPrototype prototype,
            ObjectPrototype objectPrototype)
            : base(engine, realm, _functionName)
        {
            _prototype = prototype;
            PrototypeObject = new WeakMapPrototype(engine, realm, this, objectPrototype);
            _length = new PropertyDescriptor(0, PropertyFlag.Configurable);
            _prototypeDescriptor = new PropertyDescriptor(PrototypeObject, PropertyFlag.AllForbidden);
        }

        public WeakMapPrototype PrototypeObject { get; }

        protected internal override JsValue Call(JsValue thisObject, JsValue[] arguments)
        {
            ExceptionHelper.ThrowTypeError(_realm, "Constructor WeakMap requires 'new'");
            return null;
        }

        ObjectInstance IConstructor.Construct(JsValue[] arguments, JsValue newTarget)
        {
            if (newTarget.IsUndefined())
            {
                ExceptionHelper.ThrowTypeError(_realm);
            }

            var map = OrdinaryCreateFromConstructor(
                newTarget,
                static intrinsics =>  intrinsics.WeakMap.PrototypeObject,
                static (Engine engine, Realm _, object? _) => new WeakMapInstance(engine));
            if (arguments.Length > 0 && !arguments[0].IsNullOrUndefined())
            {
                var adder = map.Get("set");
                var iterator = arguments.At(0).GetIterator(_realm);

                IteratorProtocol.AddEntriesFromIterable(map, iterator, adder);
            }

            return map;
        }
    }
}
