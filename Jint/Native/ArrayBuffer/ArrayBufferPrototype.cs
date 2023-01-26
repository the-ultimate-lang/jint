using Ultimate.Language.Jint.Collections;
using Ultimate.Language.Jint.Native.Object;
using Ultimate.Language.Jint.Native.Symbol;
using Ultimate.Language.Jint.Runtime;
using Ultimate.Language.Jint.Runtime.Descriptors;
using Ultimate.Language.Jint.Runtime.Interop;

namespace Ultimate.Language.Jint.Native.ArrayBuffer
{
    /// <summary>
    /// https://tc39.es/ecma262/#sec-properties-of-the-arraybuffer-prototype-object
    /// </summary>
    internal sealed class ArrayBufferPrototype : Prototype
    {
        private readonly ArrayBufferConstructor _constructor;

        internal ArrayBufferPrototype(
            Engine engine,
            ArrayBufferConstructor constructor,
            ObjectPrototype objectPrototype) : base(engine, engine.Realm)
        {
            _prototype = objectPrototype;
            _constructor = constructor;
        }

        protected override void Initialize()
        {
            const PropertyFlag lengthFlags = PropertyFlag.Configurable;
            var properties = new PropertyDictionary(3, checkExistingKeys: false)
            {
                ["byteLength"] = new GetSetPropertyDescriptor(new ClrFunctionInstance(_engine, "get byteLength", ByteLength, 0, lengthFlags), Undefined, PropertyFlag.Configurable),
                ["constructor"] = new PropertyDescriptor(_constructor, PropertyFlag.NonEnumerable),
                ["slice"] = new PropertyDescriptor(new ClrFunctionInstance(Engine, "slice", Slice, 2, lengthFlags), PropertyFlag.Configurable | PropertyFlag.Writable)
            };
            SetProperties(properties);

            var symbols = new SymbolDictionary(1)
            {
                [GlobalSymbolRegistry.ToStringTag] = new PropertyDescriptor("ArrayBuffer", PropertyFlag.Configurable)
            };
            SetSymbols(symbols);
        }

        /// <summary>
        /// https://tc39.es/ecma262/#sec-get-arraybuffer.prototype.bytelength
        /// </summary>
        private JsValue ByteLength(JsValue thisObj, JsValue[] arguments)
        {
            var o = thisObj as ArrayBufferInstance;
            if (o is null)
            {
                ExceptionHelper.ThrowTypeError(_realm, "Method ArrayBuffer.prototype.byteLength called on incompatible receiver " + thisObj);
            }

            if (o.IsSharedArrayBuffer)
            {
                ExceptionHelper.ThrowTypeError(_realm);
            }

            if (o.IsDetachedBuffer)
            {
                return JsNumber.PositiveZero;
            }

            return JsNumber.Create(o.ArrayBufferByteLength);
        }

        /// <summary>
        /// https://tc39.es/ecma262/#sec-arraybuffer.prototype.slice
        /// </summary>
        private JsValue Slice(JsValue thisObj, JsValue[] arguments)
        {
            var o = thisObj as ArrayBufferInstance;
            if (o is null)
            {
                ExceptionHelper.ThrowTypeError(_realm, "Method ArrayBuffer.prototype.slice called on incompatible receiver " + thisObj);
            }

            if (o.IsSharedArrayBuffer)
            {
                ExceptionHelper.ThrowTypeError(_realm);
            }

            o.AssertNotDetached();

            var start = arguments.At(0);
            var end = arguments.At(1);

            var len = o.ArrayBufferByteLength;
            var relativeStart = TypeConverter.ToIntegerOrInfinity(start);
            var first = relativeStart switch
            {
                double.NegativeInfinity => 0,
                < 0 => (int) System.Math.Max(len + relativeStart, 0),
                _ => (int) System.Math.Min(relativeStart, len)
            };

            double relativeEnd;
            if (end.IsUndefined())
            {
                relativeEnd = len;
            }
            else
            {
                relativeEnd = TypeConverter.ToIntegerOrInfinity(end);
            }

            var final = relativeEnd switch
            {
                double.NegativeInfinity => 0,
                < 0 => (int) System.Math.Max(len + relativeEnd, 0),
                _ => (int) System.Math.Min(relativeEnd, len)
            };

            var newLen = System.Math.Max(final - first, 0);
            var ctor = SpeciesConstructor(o, _realm.Intrinsics.ArrayBuffer);
            var bufferInstance = Construct(ctor, new JsValue[] { JsNumber.Create(newLen) }) as ArrayBufferInstance;

            if (bufferInstance is null)
            {
                ExceptionHelper.ThrowTypeError(_realm);
            }
            if (bufferInstance.IsSharedArrayBuffer)
            {
                ExceptionHelper.ThrowTypeError(_realm);
            }
            if (bufferInstance.IsDetachedBuffer)
            {
                ExceptionHelper.ThrowTypeError(_realm);
            }

            if (ReferenceEquals(bufferInstance, o))
            {
                ExceptionHelper.ThrowTypeError(_realm);
            }

            if (bufferInstance.ArrayBufferByteLength < newLen)
            {
                ExceptionHelper.ThrowTypeError(_realm);
            }

            // NOTE: Side-effects of the above steps may have detached O.

            if (bufferInstance.IsDetachedBuffer)
            {
                ExceptionHelper.ThrowTypeError(_realm);
            }

            var fromBuf = o.ArrayBufferData;
            var toBuf = bufferInstance.ArrayBufferData;
            System.Array.Copy(fromBuf, first, toBuf, 0, newLen);
            return bufferInstance;
        }
    }
}
