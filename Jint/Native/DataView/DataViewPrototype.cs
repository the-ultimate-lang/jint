using Ultimate.Language.Jint.Collections;
using Ultimate.Language.Jint.Native.ArrayBuffer;
using Ultimate.Language.Jint.Native.Object;
using Ultimate.Language.Jint.Native.Symbol;
using Ultimate.Language.Jint.Native.TypedArray;
using Ultimate.Language.Jint.Runtime;
using Ultimate.Language.Jint.Runtime.Descriptors;
using Ultimate.Language.Jint.Runtime.Interop;

namespace Ultimate.Language.Jint.Native.DataView
{
    /// <summary>
    /// https://tc39.es/ecma262/#sec-properties-of-the-dataview-prototype-object
    /// </summary>
    internal sealed class DataViewPrototype : Prototype
    {
        private readonly DataViewConstructor _constructor;

        internal DataViewPrototype(
            Engine engine,
            DataViewConstructor constructor,
            ObjectPrototype objectPrototype) : base(engine, engine.Realm)
        {
            _prototype = objectPrototype;
            _constructor = constructor;
        }

        protected override void Initialize()
        {
            const PropertyFlag lengthFlags = PropertyFlag.Configurable;
            const PropertyFlag propertyFlags = PropertyFlag.Configurable | PropertyFlag.Writable;
            var properties = new PropertyDictionary(24, checkExistingKeys: false)
            {
                ["buffer"] = new GetSetPropertyDescriptor(new ClrFunctionInstance(_engine, "get buffer", Buffer, 0, lengthFlags), Undefined, PropertyFlag.Configurable),
                ["byteLength"] = new GetSetPropertyDescriptor(new ClrFunctionInstance(_engine, "get byteLength", ByteLength, 0, lengthFlags), Undefined, PropertyFlag.Configurable),
                ["byteOffset"] = new GetSetPropertyDescriptor(new ClrFunctionInstance(Engine, "get byteOffset", ByteOffset, 0, lengthFlags), Undefined, PropertyFlag.Configurable),
                ["constructor"] = new PropertyDescriptor(_constructor, PropertyFlag.NonEnumerable),
                ["getBigInt64"] = new PropertyDescriptor(new ClrFunctionInstance(Engine, "getBigInt64", GetBigInt64, length: 1, lengthFlags), propertyFlags),
                ["getBigUint64"] = new PropertyDescriptor(new ClrFunctionInstance(Engine, "getBigUint64", GetBigUint64, length: 1, lengthFlags), propertyFlags),
                ["getFloat32"] = new PropertyDescriptor(new ClrFunctionInstance(Engine, "getFloat32", GetFloat32, length: 1, lengthFlags), propertyFlags),
                ["getFloat64"] = new PropertyDescriptor(new ClrFunctionInstance(Engine, "getFloat64", GetFloat64, length: 1, lengthFlags), propertyFlags),
                ["getInt8"] = new PropertyDescriptor(new ClrFunctionInstance(Engine, "getInt8", GetInt8, length: 1, lengthFlags), propertyFlags),
                ["getInt16"] = new PropertyDescriptor(new ClrFunctionInstance(Engine, "getInt16", GetInt16, length: 1, lengthFlags), propertyFlags),
                ["getInt32"] = new PropertyDescriptor(new ClrFunctionInstance(Engine, "getInt32", GetInt32, length: 1, lengthFlags), propertyFlags),
                ["getUint8"] = new PropertyDescriptor(new ClrFunctionInstance(Engine, "getUint8", GetUint8, length: 1, lengthFlags), propertyFlags),
                ["getUint16"] = new PropertyDescriptor(new ClrFunctionInstance(Engine, "getUint16", GetUint16, length: 1, lengthFlags), propertyFlags),
                ["getUint32"] = new PropertyDescriptor(new ClrFunctionInstance(Engine, "getUint32", GetUint32, length: 1, lengthFlags), propertyFlags),
                ["setBigInt64"] = new PropertyDescriptor(new ClrFunctionInstance(Engine, "setBigInt64", SetBigInt64, length: 2, lengthFlags), propertyFlags),
                ["setBigUint64"] = new PropertyDescriptor(new ClrFunctionInstance(Engine, "setBigUint64", SetBigUint64, length: 2, lengthFlags), propertyFlags),
                ["setFloat32"] = new PropertyDescriptor(new ClrFunctionInstance(Engine, "setFloat32", SetFloat32, length: 2, lengthFlags), propertyFlags),
                ["setFloat64"] = new PropertyDescriptor(new ClrFunctionInstance(Engine, "setFloat64", SetFloat64, length: 2, lengthFlags), propertyFlags),
                ["setInt8"] = new PropertyDescriptor(new ClrFunctionInstance(Engine, "setInt8", SetInt8, length: 2, lengthFlags), propertyFlags),
                ["setInt16"] = new PropertyDescriptor(new ClrFunctionInstance(Engine, "setInt16", SetInt16, length: 2, lengthFlags), propertyFlags),
                ["setInt32"] = new PropertyDescriptor(new ClrFunctionInstance(Engine, "setInt32", SetInt32, length: 2, lengthFlags), propertyFlags),
                ["setUint8"] = new PropertyDescriptor(new ClrFunctionInstance(Engine, "setUint8", SetUint8, length: 2, lengthFlags), propertyFlags),
                ["setUint16"] = new PropertyDescriptor(new ClrFunctionInstance(Engine, "setUint16", SetUint16, length: 2, lengthFlags), propertyFlags),
                ["setUint32"] = new PropertyDescriptor(new ClrFunctionInstance(Engine, "setUint32", SetUint32, length: 2, lengthFlags), propertyFlags)
            };
            SetProperties(properties);

            var symbols = new SymbolDictionary(1)
            {
                [GlobalSymbolRegistry.ToStringTag] = new PropertyDescriptor("DataView", PropertyFlag.Configurable)
            };
            SetSymbols(symbols);
        }

        /// <summary>
        /// https://tc39.es/ecma262/#sec-get-dataview.prototype.buffer
        /// </summary>
        private JsValue Buffer(JsValue thisObj, JsValue[] arguments)
        {
            var o = thisObj as DataViewInstance;
            if (o is null)
            {
                ExceptionHelper.ThrowTypeError(_realm, "Method get DataView.prototype.buffer called on incompatible receiver " + thisObj);
            }

            return o._viewedArrayBuffer!;
        }

        /// <summary>
        /// https://tc39.es/ecma262/#sec-get-dataview.prototype.bytelength
        /// </summary>
        private JsValue ByteLength(JsValue thisObj, JsValue[] arguments)
        {
            var o = thisObj as DataViewInstance;
            if (o is null)
            {
                ExceptionHelper.ThrowTypeError(_realm, "Method get DataView.prototype.byteLength called on incompatible receiver " + thisObj);
            }

            var buffer = o._viewedArrayBuffer!;
            buffer.AssertNotDetached();

            return JsNumber.Create(o._byteLength);
        }

        /// <summary>
        /// https://tc39.es/ecma262/#sec-get-dataview.prototype.byteoffset
        /// </summary>
        private JsValue ByteOffset(JsValue thisObj, JsValue[] arguments)
        {
            var o = thisObj as DataViewInstance;
            if (o is null)
            {
                ExceptionHelper.ThrowTypeError(_realm, "Method get DataView.prototype.byteOffset called on incompatible receiver " + thisObj);
            }

            var buffer = o._viewedArrayBuffer!;
            buffer.AssertNotDetached();

            return JsNumber.Create(o._byteOffset);
        }

        private JsValue GetBigInt64(JsValue thisObj, JsValue[] arguments)
        {
            return GetViewValue(thisObj, arguments.At(0), arguments.At(1), TypedArrayElementType.BigInt64);
        }

        private JsValue GetBigUint64(JsValue thisObj, JsValue[] arguments)
        {
            return GetViewValue(thisObj, arguments.At(0), arguments.At(1), TypedArrayElementType.BigUint64);
        }

        private JsValue GetFloat32(JsValue thisObj, JsValue[] arguments)
        {
            return GetViewValue(thisObj, arguments.At(0), arguments.At(1, JsBoolean.False), TypedArrayElementType.Float32);
        }

        private JsValue GetFloat64(JsValue thisObj, JsValue[] arguments)
        {
            return GetViewValue(thisObj, arguments.At(0), arguments.At(1, JsBoolean.False), TypedArrayElementType.Float64);
        }

        private JsValue GetInt8(JsValue thisObj, JsValue[] arguments)
        {
            return GetViewValue(thisObj, arguments.At(0), JsBoolean.True, TypedArrayElementType.Int8);
        }

        private JsValue GetInt16(JsValue thisObj, JsValue[] arguments)
        {
            return GetViewValue(thisObj, arguments.At(0), arguments.At(1, JsBoolean.False), TypedArrayElementType.Int16);
        }

        private JsValue GetInt32(JsValue thisObj, JsValue[] arguments)
        {
            return GetViewValue(thisObj, arguments.At(0), arguments.At(1, JsBoolean.False), TypedArrayElementType.Int32);
        }

        private JsValue GetUint8(JsValue thisObj, JsValue[] arguments)
        {
            return GetViewValue(thisObj, arguments.At(0), JsBoolean.True, TypedArrayElementType.Uint8);
        }

        private JsValue GetUint16(JsValue thisObj, JsValue[] arguments)
        {
            return GetViewValue(thisObj, arguments.At(0), arguments.At(1, JsBoolean.False), TypedArrayElementType.Uint16);
        }

        private JsValue GetUint32(JsValue thisObj, JsValue[] arguments)
        {
            return GetViewValue(thisObj, arguments.At(0), arguments.At(1, JsBoolean.False), TypedArrayElementType.Uint32);
        }

        private JsValue SetBigInt64(JsValue thisObj, JsValue[] arguments)
        {
            return SetViewValue(thisObj, arguments.At(0), arguments.At(2), TypedArrayElementType.BigInt64, arguments.At(1));
        }

        private JsValue SetBigUint64(JsValue thisObj, JsValue[] arguments)
        {
            return SetViewValue(thisObj, arguments.At(0), arguments.At(2), TypedArrayElementType.BigUint64, arguments.At(1));
        }

        private JsValue SetFloat32 (JsValue thisObj, JsValue[] arguments)
        {
            return SetViewValue(thisObj, arguments.At(0), arguments.At(2, JsBoolean.False), TypedArrayElementType.Float32, arguments.At(1));
        }

        private JsValue SetFloat64(JsValue thisObj, JsValue[] arguments)
        {
            return SetViewValue(thisObj, arguments.At(0), arguments.At(2, JsBoolean.False), TypedArrayElementType.Float64, arguments.At(1));
        }

        private JsValue SetInt8 (JsValue thisObj, JsValue[] arguments)
        {
            return SetViewValue(thisObj, arguments.At(0), JsBoolean.True, TypedArrayElementType.Int8, arguments.At(1));
        }

        private JsValue SetInt16(JsValue thisObj, JsValue[] arguments)
        {
            return SetViewValue(thisObj, arguments.At(0), arguments.At(2, JsBoolean.False), TypedArrayElementType.Int16, arguments.At(1));
        }

        private JsValue SetInt32(JsValue thisObj, JsValue[] arguments)
        {
            return SetViewValue(thisObj, arguments.At(0), arguments.At(2, JsBoolean.False), TypedArrayElementType.Int32, arguments.At(1));
        }

        private JsValue SetUint8(JsValue thisObj, JsValue[] arguments)
        {
            return SetViewValue(thisObj, arguments.At(0), JsBoolean.True, TypedArrayElementType.Uint8, arguments.At(1));
        }

        private JsValue SetUint16(JsValue thisObj, JsValue[] arguments)
        {
            return SetViewValue(thisObj, arguments.At(0), arguments.At(2, JsBoolean.False), TypedArrayElementType.Uint16, arguments.At(1));
        }

        private JsValue SetUint32(JsValue thisObj, JsValue[] arguments)
        {
            return SetViewValue(thisObj, arguments.At(0), arguments.At(2, JsBoolean.False), TypedArrayElementType.Uint32, arguments.At(1));
        }

        /// <summary>
        /// https://tc39.es/ecma262/#sec-getviewvalue
        /// </summary>
        private JsValue GetViewValue(
            JsValue view,
            JsValue requestIndex,
            JsValue isLittleEndian,
            TypedArrayElementType type)
        {
            var dataView = view as DataViewInstance;
            if (dataView is null)
            {
                ExceptionHelper.ThrowTypeError(_realm, "Method called on incompatible receiver " + view);
            }

            var getIndex = (int) TypeConverter.ToIndex(_realm, requestIndex);
            var isLittleEndianBoolean = TypeConverter.ToBoolean(isLittleEndian);
            var buffer = dataView._viewedArrayBuffer!;

            buffer.AssertNotDetached();

            var viewOffset = dataView._byteOffset;
            var viewSize = dataView._byteLength;
            var elementSize = type.GetElementSize();
            if (getIndex + elementSize > viewSize)
            {
                ExceptionHelper.ThrowRangeError(_realm, "Offset is outside the bounds of the DataView");
            }

            var bufferIndex = (int) (getIndex + viewOffset);
            return buffer.GetValueFromBuffer(bufferIndex, type, false, ArrayBufferOrder.Unordered, isLittleEndianBoolean).ToJsValue();
        }

        /// <summary>
        /// https://tc39.es/ecma262/#sec-setviewvalue
        /// </summary>
        private JsValue SetViewValue(
            JsValue view,
            JsValue requestIndex,
            JsValue isLittleEndian,
            TypedArrayElementType type,
            JsValue value)
        {
            var dataView = view as DataViewInstance;
            if (dataView is null)
            {
                ExceptionHelper.ThrowTypeError(_realm, "Method called on incompatible receiver " + view);
            }

            var getIndex = TypeConverter.ToIndex(_realm, requestIndex);

            TypedArrayValue numberValue;
            if (type.IsBigIntElementType())
            {
                numberValue = TypeConverter.ToBigInt(value);
            }
            else
            {
                numberValue = TypeConverter.ToNumber(value);
            }

            var isLittleEndianBoolean = TypeConverter.ToBoolean(isLittleEndian);
            var buffer = dataView._viewedArrayBuffer!;
            buffer.AssertNotDetached();

            var viewOffset = dataView._byteOffset;
            var viewSize = dataView._byteLength;
            var elementSize = type.GetElementSize();
            if (getIndex + elementSize > viewSize)
            {
                ExceptionHelper.ThrowRangeError(_realm, "Offset is outside the bounds of the DataView");
            }

            var bufferIndex = (int) (getIndex + viewOffset);
            buffer.SetValueInBuffer(bufferIndex, type, numberValue, false, ArrayBufferOrder.Unordered, isLittleEndianBoolean);
            return Undefined;
        }
    }
}
