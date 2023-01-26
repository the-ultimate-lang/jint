using System.Collections;
using Ultimate.Language.Jint.Collections;
using Ultimate.Language.Jint.Native.Function;
using Ultimate.Language.Jint.Native.Iterator;
using Ultimate.Language.Jint.Native.Object;
using Ultimate.Language.Jint.Native.Symbol;
using Ultimate.Language.Jint.Runtime;
using Ultimate.Language.Jint.Runtime.Descriptors;
using Ultimate.Language.Jint.Runtime.Interop;

namespace Ultimate.Language.Jint.Native.Array
{
    public sealed class ArrayConstructor : FunctionInstance, IConstructor
    {
        private static readonly JsString _functionName = new JsString("Array");

        internal ArrayConstructor(
            Engine engine,
            Realm realm,
            FunctionPrototype functionPrototype,
            ObjectPrototype objectPrototype)
            : base(engine, realm, _functionName)
        {
            _prototype = functionPrototype;
            PrototypeObject = new ArrayPrototype(engine, realm, this, objectPrototype);
            _length = new PropertyDescriptor(1, PropertyFlag.Configurable);
            _prototypeDescriptor = new PropertyDescriptor(PrototypeObject, PropertyFlag.AllForbidden);
        }

        public ArrayPrototype PrototypeObject { get; }

        protected override void Initialize()
        {
            var properties = new PropertyDictionary(3, checkExistingKeys: false)
            {
                ["from"] = new PropertyDescriptor(new PropertyDescriptor(new ClrFunctionInstance(Engine, "from", From, 1, PropertyFlag.Configurable), PropertyFlag.NonEnumerable)),
                ["isArray"] = new PropertyDescriptor(new PropertyDescriptor(new ClrFunctionInstance(Engine, "isArray", IsArray, 1), PropertyFlag.NonEnumerable)),
                ["of"] = new PropertyDescriptor(new PropertyDescriptor(new ClrFunctionInstance(Engine, "of", Of, 0, PropertyFlag.Configurable), PropertyFlag.NonEnumerable))
            };
            SetProperties(properties);

            var symbols = new SymbolDictionary(1)
            {
                [GlobalSymbolRegistry.Species] = new GetSetPropertyDescriptor(get: new ClrFunctionInstance(Engine, "get [Symbol.species]", Species, 0, PropertyFlag.Configurable), set: Undefined,PropertyFlag.Configurable),
            };
            SetSymbols(symbols);
        }

        /// <summary>
        /// https://tc39.es/ecma262/#sec-array.from
        /// </summary>
        private JsValue From(JsValue thisObj, JsValue[] arguments)
        {
            var source = arguments.At(0);
            var mapFunction = arguments.At(1);
            var callable = !mapFunction.IsUndefined() ? GetCallable(mapFunction) : null;
            var thisArg = arguments.At(2);

            if (source.IsNullOrUndefined())
            {
                ExceptionHelper.ThrowTypeError(_realm, "Cannot convert undefined or null to object");
            }

            if (source is JsString jsString)
            {
                var a = _realm.Intrinsics.Array.ArrayCreate((uint) jsString.Length);
                for (var i = 0; i < jsString._value.Length; i++)
                {
                    a.SetIndexValue((uint) i, JsString.Create(jsString._value[i]), updateLength: false);
                }
                return a;
            }

            if (thisObj.IsNull() || source is not ObjectInstance objectInstance)
            {
                return _realm.Intrinsics.Array.ArrayCreate(0);
            }

            if (objectInstance is IObjectWrapper { Target: IEnumerable enumerable })
            {
                return ConstructArrayFromIEnumerable(enumerable);
            }

            if (objectInstance.IsArrayLike)
            {
                return ConstructArrayFromArrayLike(thisObj, objectInstance, callable, thisArg);
            }

            ObjectInstance instance;
            if (thisObj is IConstructor constructor)
            {
                instance = constructor.Construct(System.Array.Empty<JsValue>(), thisObj);
            }
            else
            {
                instance = _realm.Intrinsics.Array.ArrayCreate(0);
            }

            if (objectInstance.TryGetIterator(_realm, out var iterator))
            {
                var protocol = new ArrayProtocol(_engine, thisArg, instance, iterator, callable);
                protocol.Execute();
            }

            return instance;
        }

        private ObjectInstance ConstructArrayFromArrayLike(
            JsValue thisObj,
            ObjectInstance objectInstance,
            ICallable? callable,
            JsValue thisArg)
        {
            var iterator = objectInstance.Get(GlobalSymbolRegistry.Iterator);
            var source = ArrayOperations.For(objectInstance);
            var length = source.GetLength();

            ObjectInstance a;
            if (thisObj is IConstructor constructor)
            {
                var isNullOrUndefined = iterator.IsNullOrUndefined();
                var argumentsList = isNullOrUndefined
                    ? new JsValue[] { length }
                    : null;

                a = Construct(constructor, argumentsList);
            }
            else
            {
                a = _realm.Intrinsics.Array.ArrayCreate(length);
            }

            var args = !ReferenceEquals(callable, null)
                ? _engine._jsValueArrayPool.RentArray(2)
                : null;

            var target = ArrayOperations.For(a);
            uint n = 0;
            for (uint i = 0; i < length; i++)
            {
                JsValue jsValue;
                var value = source.Get(i);
                if (!ReferenceEquals(callable, null))
                {
                    args![0] = value;
                    args[1] = i;
                    jsValue = callable.Call(thisArg, args);

                    // function can alter data
                    length = source.GetLength();
                }
                else
                {
                    jsValue = value;
                }

                target.CreateDataPropertyOrThrow(i, jsValue);
                n++;
            }

            if (!ReferenceEquals(callable, null))
            {
                _engine._jsValueArrayPool.ReturnArray(args!);
            }

            target.SetLength(length);
            return a;
        }

        private sealed class ArrayProtocol : IteratorProtocol
        {
            private readonly JsValue _thisArg;
            private readonly ArrayOperations _instance;
            private readonly ICallable? _callable;
            private long _index = -1;

            public ArrayProtocol(
                Engine engine,
                JsValue thisArg,
                ObjectInstance instance,
                IteratorInstance iterator,
                ICallable? callable) : base(engine, iterator, 2)
            {
                _thisArg = thisArg;
                _instance = ArrayOperations.For(instance);
                _callable = callable;
            }

            protected override void ProcessItem(JsValue[] args, JsValue currentValue)
            {
                _index++;
                JsValue jsValue;
                if (!ReferenceEquals(_callable, null))
                {
                    args[0] = currentValue;
                    args[1] = _index;
                    jsValue = _callable.Call(_thisArg, args);
                }
                else
                {
                    jsValue = currentValue;
                }

                _instance.CreateDataPropertyOrThrow((ulong) _index, jsValue);
            }

            protected override void IterationEnd()
            {
                _instance.SetLength((ulong) (_index + 1));
            }
        }

        private JsValue Of(JsValue thisObj, JsValue[] arguments)
        {
            var len = arguments.Length;
            ObjectInstance a;
            if (thisObj.IsConstructor)
            {
                a = ((IConstructor) thisObj).Construct(new JsValue[] { len }, thisObj);
            }
            else
            {
                a = _realm.Intrinsics.Array.Construct(len);
            }

            if (a is JsArray ai)
            {
                // faster for real arrays
                for (uint k = 0; k < arguments.Length; k++)
                {
                    var kValue = arguments[k];
                    ai.SetIndexValue(k, kValue, updateLength: k == arguments.Length - 1);
                }
            }
            else
            {
                // slower version
                for (uint k = 0; k < arguments.Length; k++)
                {
                    var kValue = arguments[k];
                    var key = JsString.Create(k);
                    a.CreateDataPropertyOrThrow(key, kValue);
                }

                a.Set(CommonProperties.Length, len, true);
            }

            return a;
        }

        private static JsValue Species(JsValue thisObject, JsValue[] arguments)
        {
            return thisObject;
        }

        private static JsValue IsArray(JsValue thisObj, JsValue[] arguments)
        {
            var o = arguments.At(0);

            return IsArray(o);
        }

        private static JsValue IsArray(JsValue o)
        {
            if (!(o is ObjectInstance oi))
            {
                return JsBoolean.False;
            }

            return oi.IsArray();
        }

        protected internal override JsValue Call(JsValue thisObject, JsValue[] arguments)
        {
            return Construct(arguments, thisObject);
        }

        public JsArray Construct(JsValue[] arguments)
        {
            return Construct(arguments, this);
        }

        ObjectInstance IConstructor.Construct(JsValue[] arguments, JsValue newTarget) => Construct(arguments, newTarget);

        internal JsArray Construct(JsValue[] arguments, JsValue newTarget)
        {
            if (newTarget.IsUndefined())
            {
                newTarget = this;
            }

            var proto = _realm.Intrinsics.Function.GetPrototypeFromConstructor(
                newTarget,
                static intrinsics => intrinsics.Array.PrototypeObject);

            // check if we can figure out good size
            var capacity = arguments.Length > 0 ? (ulong) arguments.Length : 0;
            if (arguments.Length == 1 && arguments[0].IsNumber())
            {
                var number = ((JsNumber) arguments[0])._value;
                ValidateLength(number);
                capacity = (ulong) number;
            }
            return Construct(arguments, capacity, proto);
        }

        public JsArray Construct(int capacity)
        {
            return Construct(System.Array.Empty<JsValue>(), (uint) capacity);
        }

        public JsArray Construct(uint capacity)
        {
            return Construct(System.Array.Empty<JsValue>(), capacity);
        }

        public JsArray Construct(JsValue[] arguments, uint capacity)
        {
            return Construct(arguments, capacity, PrototypeObject);
        }

        private JsArray Construct(JsValue[] arguments, ulong capacity, ObjectInstance prototypeObject)
        {
            JsArray instance;
            if (arguments.Length == 1)
            {
                switch (arguments[0])
                {
                    case JsNumber number:
                        ValidateLength(number._value);
                        instance = ArrayCreate((ulong) number._value, prototypeObject);
                        break;
                    case IObjectWrapper objectWrapper:
                        instance = objectWrapper.Target is IEnumerable enumerable
                            ? ConstructArrayFromIEnumerable(enumerable)
                            : ArrayCreate(0, prototypeObject);
                        break;
                    case JsArray array:
                        // direct copy
                        instance = (JsArray) ConstructArrayFromArrayLike(Undefined, array, null, this);
                        break;
                    default:
                        instance = ArrayCreate(capacity, prototypeObject);
                        instance._length!._value = JsNumber.PositiveZero;
                        instance.Push(arguments);
                        break;
                }
            }
            else
            {
                instance = ArrayCreate((ulong) arguments.Length, prototypeObject);
                instance._length!._value = JsNumber.PositiveZero;
                if (arguments.Length > 0)
                {
                    instance.Push(arguments);
                }
            }

            return instance;
        }

        /// <summary>
        /// https://tc39.es/ecma262/#sec-arraycreate
        /// </summary>
        internal JsArray ArrayCreate(ulong length, ObjectInstance? proto = null)
        {
            if (length > ArrayOperations.MaxArrayLength)
            {
                ExceptionHelper.ThrowRangeError(_realm, "Invalid array length " + length);
            }

            proto ??= PrototypeObject;
            var instance = new JsArray(Engine, (uint) length, (uint) length)
            {
                _prototype = proto
            };
            return instance;
        }

        private JsArray ConstructArrayFromIEnumerable(IEnumerable enumerable)
        {
            var jsArray = Construct(Arguments.Empty);
            var tempArray = _engine._jsValueArrayPool.RentArray(1);
            foreach (var item in enumerable)
            {
                var jsItem = FromObject(Engine, item);
                tempArray[0] = jsItem;
                _realm.Intrinsics.Array.PrototypeObject.Push(jsArray, tempArray);
            }

            _engine._jsValueArrayPool.ReturnArray(tempArray);
            return jsArray;
        }

        public JsArray ConstructFast(JsValue[] contents)
        {
            var instance = ArrayCreate((ulong) contents.Length);
            for (var i = 0; i < contents.Length; i++)
            {
                instance.SetIndexValue((uint) i, contents[i], updateLength: false);
            }
            return instance;
        }

        internal JsArray ConstructFast(List<JsValue> contents)
        {
            var instance = ArrayCreate((ulong) contents.Count);
            for (var i = 0; i < contents.Count; i++)
            {
                instance.SetIndexValue((uint) i, contents[i], updateLength: false);
            }
            return instance;
        }

        /// <summary>
        /// https://tc39.es/ecma262/#sec-arrayspeciescreate
        /// </summary>
        internal ObjectInstance ArraySpeciesCreate(ObjectInstance originalArray, ulong length)
        {
            var isArray = originalArray.IsArray();
            if (!isArray)
            {
                return ArrayCreate(length);
            }

            var c = originalArray.Get(CommonProperties.Constructor);

            if (c.IsConstructor)
            {
                var thisRealm = _engine.ExecutionContext.Realm;
                var realmC = GetFunctionRealm(c);
                if (!ReferenceEquals(thisRealm, realmC))
                {
                    if (ReferenceEquals(c, realmC.Intrinsics.Array))
                    {
                        c = Undefined;
                    }
                }
            }

            if (c.IsObject())
            {
                c = c.Get(GlobalSymbolRegistry.Species);
                if (c.IsNull())
                {
                    c = Undefined;
                }
            }

            if (c.IsUndefined())
            {
                return ArrayCreate(length);
            }

            if (!c.IsConstructor)
            {
                ExceptionHelper.ThrowTypeError(_realm, $"{c} is not a constructor");
            }

            return ((IConstructor) c).Construct(new JsValue[] { JsNumber.Create(length) }, c);
        }

        internal JsArray CreateArrayFromList<T>(List<T> values) where T : JsValue
        {
            var jsArray = ArrayCreate((uint) values.Count);
            var index = 0;
            for (; index < values.Count; index++)
            {
                var item = values[index];
                jsArray.SetIndexValue((uint) index, item, false);
            }

            jsArray.SetLength((uint) index);
            return jsArray;
        }

        internal JsArray CreateArrayFromList<T>(T[] values) where T : JsValue
        {
            var jsArray = ArrayCreate((uint) values.Length);
            var index = 0;
            for (; index < values.Length; index++)
            {
                var item = values[index];
                jsArray.SetIndexValue((uint) index, item, false);
            }

            jsArray.SetLength((uint) index);
            return jsArray;
        }

        private void ValidateLength(double length)
        {
            if (length < 0 || length > ArrayOperations.MaxArrayLikeLength || ((long) length) != length)
            {
                ExceptionHelper.ThrowRangeError(_realm, "Invalid array length");
            }
        }
    }
}
