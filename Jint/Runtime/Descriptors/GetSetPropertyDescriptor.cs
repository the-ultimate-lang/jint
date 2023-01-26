using Ultimate.Language.Jint.Native;

namespace Ultimate.Language.Jint.Runtime.Descriptors
{
    public sealed class GetSetPropertyDescriptor : PropertyDescriptor
    {
        private JsValue? _get;
        private JsValue? _set;

        public GetSetPropertyDescriptor(JsValue? get, JsValue? set, bool? enumerable = null, bool? configurable = null)
        : base(null, writable: null, enumerable: enumerable, configurable: configurable)
        {
            _get = get;
            _set = set;
        }

        internal GetSetPropertyDescriptor(JsValue? get, JsValue? set, PropertyFlag flags)
            : base(null, flags)
        {
            _get = get;
            _set = set;
        }

        public GetSetPropertyDescriptor(PropertyDescriptor descriptor) : base(descriptor)
        {
            _get = descriptor.Get;
            _set = descriptor.Set;
        }

        public override JsValue? Get => _get;
        public override JsValue? Set => _set;

        internal void SetGet(JsValue getter)
        {
            _get = getter;
        }

        internal void SetSet(JsValue setter)
        {
            _set = setter;
        }

        internal sealed class ThrowerPropertyDescriptor : PropertyDescriptor
        {
            private readonly Engine _engine;
            private JsValue? _thrower;

            public ThrowerPropertyDescriptor(Engine engine, PropertyFlag flags) : base(flags)
            {
                _engine = engine;
            }

            public override JsValue Get => _thrower ??= _engine.Realm.Intrinsics.ThrowTypeError;
            public override JsValue Set => _thrower ??= _engine.Realm.Intrinsics.ThrowTypeError;

            protected internal override JsValue? CustomValue
            {
                set => ExceptionHelper.ThrowInvalidOperationException("making changes to throw type error property's descriptor is not allowed");
            }
        }
    }
}
