using System.Diagnostics;
using System.Runtime.CompilerServices;
using Ultimate.Language.Jint.Collections;
using Ultimate.Language.Jint.Native;
using Ultimate.Language.Jint.Native.Object;

namespace Ultimate.Language.Jint.Runtime.Descriptors
{
    [DebuggerDisplay("Value: {Value}, Flags: {Flags}")]
    public class PropertyDescriptor
    {
        public static readonly PropertyDescriptor Undefined = new UndefinedPropertyDescriptor();

        internal PropertyFlag _flags;
        internal JsValue? _value;

        public PropertyDescriptor() : this(PropertyFlag.None)
        {
        }

        protected PropertyDescriptor(PropertyFlag flags)
        {
            _flags = flags;
        }

        protected internal PropertyDescriptor(JsValue? value, PropertyFlag flags) : this(flags)
        {
            if ((_flags & PropertyFlag.CustomJsValue) != 0)
            {
                CustomValue = value;
            }
            _value = value;
        }

        public PropertyDescriptor(JsValue? value, bool? writable, bool? enumerable, bool? configurable)
        {
            if ((_flags & PropertyFlag.CustomJsValue) != 0)
            {
                CustomValue = value;
            }
            _value = value;

            if (writable != null)
            {
                Writable = writable.Value;
                WritableSet = true;
            }

            if (enumerable != null)
            {
                Enumerable = enumerable.Value;
                EnumerableSet = true;
            }

            if (configurable != null)
            {
                Configurable = configurable.Value;
                ConfigurableSet = true;
            }
        }

        public PropertyDescriptor(PropertyDescriptor descriptor)
        {
            Value = descriptor.Value;

            Enumerable = descriptor.Enumerable;
            EnumerableSet = descriptor.EnumerableSet;

            Configurable = descriptor.Configurable;
            ConfigurableSet = descriptor.ConfigurableSet;

            Writable = descriptor.Writable;
            WritableSet = descriptor.WritableSet;
        }

        public virtual JsValue? Get => null;
        public virtual JsValue? Set => null;

        public bool Enumerable
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => (_flags & PropertyFlag.Enumerable) != 0;
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            set
            {
                _flags |= PropertyFlag.EnumerableSet;
                if (value)
                {
                    _flags |= PropertyFlag.Enumerable;
                }
                else
                {
                    _flags &= ~(PropertyFlag.Enumerable);
                }
            }
        }

        public bool EnumerableSet
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => (_flags & (PropertyFlag.EnumerableSet | PropertyFlag.Enumerable)) != 0;
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            private set
            {
                if (value)
                {
                    _flags |= PropertyFlag.EnumerableSet;
                }
                else
                {
                    _flags &= ~(PropertyFlag.EnumerableSet);
                }
            }
        }

        public bool Writable
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => (_flags & PropertyFlag.Writable) != 0;
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            set
            {
                _flags |= PropertyFlag.WritableSet;
                if (value)
                {
                    _flags |= PropertyFlag.Writable;
                }
                else
                {
                    _flags &= ~(PropertyFlag.Writable);
                }
            }
        }

        public bool WritableSet
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => (_flags & (PropertyFlag.WritableSet | PropertyFlag.Writable)) != 0;
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            private set
            {
                if (value)
                {
                    _flags |= PropertyFlag.WritableSet;
                }
                else
                {
                    _flags &= ~(PropertyFlag.WritableSet);
                }
            }
        }

        public bool Configurable
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => (_flags & PropertyFlag.Configurable) != 0;
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            set
            {
                _flags |= PropertyFlag.ConfigurableSet;
                if (value)
                {
                    _flags |= PropertyFlag.Configurable;
                }
                else
                {
                    _flags &= ~(PropertyFlag.Configurable);
                }
            }
        }

        public bool ConfigurableSet
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => (_flags & (PropertyFlag.ConfigurableSet | PropertyFlag.Configurable)) != 0;
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            private set
            {
                if (value)
                {
                    _flags |= PropertyFlag.ConfigurableSet;
                }
                else
                {
                    _flags &= ~(PropertyFlag.ConfigurableSet);
                }
            }
        }

        public JsValue Value
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get
            {
                if ((_flags & PropertyFlag.CustomJsValue) != 0)
                {
                    return CustomValue!;
                }

                return _value!;
            }
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            set
            {
                if ((_flags & PropertyFlag.CustomJsValue) != 0)
                {
                    CustomValue = value;
                }
                _value = value;
            }
        }

        protected internal virtual JsValue? CustomValue
        {
            get => null;
            set => ExceptionHelper.ThrowNotImplementedException();
        }

        internal PropertyFlag Flags
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => _flags;
        }

        /// <summary>
        /// https://tc39.es/ecma262/#sec-topropertydescriptor
        /// </summary>
        public static PropertyDescriptor ToPropertyDescriptor(Realm realm, JsValue o)
        {
            if (o is not ObjectInstance obj)
            {
                ExceptionHelper.ThrowTypeError(realm);
                return null;
            }

            bool? enumerable = null;
            var hasEnumerable = obj.HasProperty(CommonProperties.Enumerable);
            if (hasEnumerable)
            {
                enumerable = TypeConverter.ToBoolean(obj.Get(CommonProperties.Enumerable));
            }

            bool? configurable = null;
            var hasConfigurable = obj.HasProperty(CommonProperties.Configurable);
            if (hasConfigurable)
            {
                configurable = TypeConverter.ToBoolean(obj.Get(CommonProperties.Configurable));
            }

            JsValue? value = null;
            var hasValue = obj.HasProperty(CommonProperties.Value);
            if (hasValue)
            {
                value = obj.Get(CommonProperties.Value);
            }

            bool? writable = null;
            var hasWritable = obj.HasProperty(CommonProperties.Writable);
            if (hasWritable)
            {
                writable = TypeConverter.ToBoolean(obj.Get(CommonProperties.Writable));
            }

            JsValue? get = null;
            var hasGet = obj.HasProperty(CommonProperties.Get);
            if (hasGet)
            {
                get = obj.Get(CommonProperties.Get);
            }

            JsValue? set = null;
            var hasSet = obj.HasProperty(CommonProperties.Set);
            if (hasSet)
            {
                set = obj.Get(CommonProperties.Set);
            }

            if ((hasValue || hasWritable) && (hasGet || hasSet))
            {
                ExceptionHelper.ThrowTypeError(realm, "Invalid property descriptor. Cannot both specify accessors and a value or writable attribute");
            }

            var desc = hasGet || hasSet
                ? new GetSetPropertyDescriptor(null, null, PropertyFlag.None)
                : new PropertyDescriptor(PropertyFlag.None);

            if (hasEnumerable)
            {
                desc.Enumerable = enumerable!.Value;
                desc.EnumerableSet = true;
            }

            if (hasConfigurable)
            {
                desc.Configurable = configurable!.Value;
                desc.ConfigurableSet = true;
            }

            if (hasValue)
            {
                desc.Value = value!;
            }

            if (hasWritable)
            {
                desc.Writable = TypeConverter.ToBoolean(writable!.Value);
                desc.WritableSet = true;
            }

            if (hasGet)
            {
                if (!get!.IsUndefined() && get!.TryCast<ICallable>() == null)
                {
                    ExceptionHelper.ThrowTypeError(realm);
                }

                ((GetSetPropertyDescriptor) desc).SetGet(get!);
            }

            if (hasSet)
            {
                if (!set!.IsUndefined() && set!.TryCast<ICallable>() is null)
                {
                    ExceptionHelper.ThrowTypeError(realm);
                }

                ((GetSetPropertyDescriptor) desc).SetSet(set!);
            }

            if ((hasSet || hasGet) && (hasValue || hasWritable))
            {
                ExceptionHelper.ThrowTypeError(realm);
            }

            return desc;
        }

        /// <summary>
        /// https://tc39.es/ecma262/#sec-frompropertydescriptor
        /// </summary>
        public static JsValue FromPropertyDescriptor(Engine engine, PropertyDescriptor desc, bool strictUndefined = false)
        {
            if (ReferenceEquals(desc, Undefined))
            {
                return JsValue.Undefined;
            }

            var obj = engine.Realm.Intrinsics.Object.Construct(Arguments.Empty);
            var properties = new PropertyDictionary(4, checkExistingKeys: false);

            // TODO should not check for strictUndefined, but needs a bigger cleanup
            // we should have possibility to leave out the properties in property descriptors as newer tests
            // also assert properties to be undefined

            if (desc.IsDataDescriptor())
            {
                properties["value"] =  new PropertyDescriptor(desc.Value ?? JsValue.Undefined, PropertyFlag.ConfigurableEnumerableWritable);
                if (desc._flags != PropertyFlag.None || desc.WritableSet)
                {
                    properties["writable"] = new PropertyDescriptor(desc.Writable, PropertyFlag.ConfigurableEnumerableWritable);
                }
            }
            else
            {
                properties["get"] = new PropertyDescriptor(desc.Get ?? JsValue.Undefined, PropertyFlag.ConfigurableEnumerableWritable);
                properties["set"] = new PropertyDescriptor(desc.Set ?? JsValue.Undefined, PropertyFlag.ConfigurableEnumerableWritable);
            }

            if (!strictUndefined || desc.EnumerableSet)
            {
                properties["enumerable"] = new PropertyDescriptor(desc.Enumerable, PropertyFlag.ConfigurableEnumerableWritable);
            }

            if (!strictUndefined || desc.ConfigurableSet)
            {
                properties["configurable"] = new PropertyDescriptor(desc.Configurable, PropertyFlag.ConfigurableEnumerableWritable);
            }

            obj.SetProperties(properties);
            return obj;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool IsAccessorDescriptor()
        {
            return !ReferenceEquals(Get, null) || !ReferenceEquals(Set, null);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool IsDataDescriptor()
        {
            return (_flags & (PropertyFlag.WritableSet | PropertyFlag.Writable)) != 0
                   || (_flags & PropertyFlag.CustomJsValue) != 0 && !ReferenceEquals(CustomValue, null)
                   || !ReferenceEquals(_value, null);
        }

        /// <summary>
        /// http://www.ecma-international.org/ecma-262/5.1/#sec-8.10.3
        /// </summary>
        /// <returns></returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool IsGenericDescriptor()
        {
            return !IsDataDescriptor() && !IsAccessorDescriptor();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal bool TryGetValue(ObjectInstance thisArg, out JsValue value)
        {
            value = JsValue.Undefined;

            // IsDataDescriptor logic inlined
            if ((_flags & (PropertyFlag.WritableSet | PropertyFlag.Writable)) != 0)
            {
                var val = (_flags & PropertyFlag.CustomJsValue) != 0
                    ? CustomValue
                    : _value;

                if (!ReferenceEquals(val, null))
                {
                    value = val;
                    return true;
                }
            }

            if (this == Undefined)
            {
                return false;
            }

            var getter = Get;
            if (!ReferenceEquals(getter, null) && !getter.IsUndefined())
            {
                // if getter is not undefined it must be ICallable
                var callable = (ICallable) getter;
                value = callable.Call(thisArg, Arguments.Empty);
            }

            return true;
        }

        private sealed class UndefinedPropertyDescriptor : PropertyDescriptor
        {
            public UndefinedPropertyDescriptor() : base(PropertyFlag.None | PropertyFlag.CustomJsValue)
            {
            }

            protected internal override JsValue? CustomValue
            {
                set => ExceptionHelper.ThrowInvalidOperationException("making changes to undefined property's descriptor is not allowed");
            }
        }

        internal sealed class AllForbiddenDescriptor : PropertyDescriptor
        {
            private static readonly PropertyDescriptor[] _cache;

            public static readonly AllForbiddenDescriptor NumberZero = new AllForbiddenDescriptor(JsNumber.Create(0));
            public static readonly AllForbiddenDescriptor NumberOne = new AllForbiddenDescriptor(JsNumber.Create(1));

            public static readonly AllForbiddenDescriptor BooleanFalse = new AllForbiddenDescriptor(JsBoolean.False);
            public static readonly AllForbiddenDescriptor BooleanTrue = new AllForbiddenDescriptor(JsBoolean.True);

            static AllForbiddenDescriptor()
            {
                _cache = new PropertyDescriptor[10];
                for (int i = 0; i < _cache.Length; ++i)
                {
                    _cache[i] = new AllForbiddenDescriptor(JsNumber.Create(i));
                }
            }

            private AllForbiddenDescriptor(JsValue value)
                : base(PropertyFlag.AllForbidden)
            {
                _value = value;
            }

            public static PropertyDescriptor ForNumber(int number)
            {
                var temp = _cache;
                return (uint) number < temp.Length
                    ? temp[number]
                    : new PropertyDescriptor(number, PropertyFlag.AllForbidden);
            }
        }
    }
}
