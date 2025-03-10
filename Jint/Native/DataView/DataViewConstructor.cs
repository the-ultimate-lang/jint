using Ultimate.Language.Jint.Native.ArrayBuffer;
using Ultimate.Language.Jint.Native.Function;
using Ultimate.Language.Jint.Native.Object;
using Ultimate.Language.Jint.Runtime;
using Ultimate.Language.Jint.Runtime.Descriptors;

namespace Ultimate.Language.Jint.Native.DataView
{
    /// <summary>
    /// https://tc39.es/ecma262/#sec-dataview-constructor
    /// </summary>
    internal sealed class DataViewConstructor : FunctionInstance, IConstructor
    {
        private static readonly JsString _functionName = new("DataView");

        internal DataViewConstructor(
            Engine engine,
            Realm realm,
            FunctionPrototype functionPrototype,
            ObjectPrototype objectPrototype)
            : base(engine, realm, _functionName)
        {
            _prototype = functionPrototype;
            PrototypeObject = new DataViewPrototype(engine, this, objectPrototype);
            _length = new PropertyDescriptor(1, PropertyFlag.Configurable);
            _prototypeDescriptor = new PropertyDescriptor(PrototypeObject, PropertyFlag.AllForbidden);
        }

        public DataViewPrototype PrototypeObject { get; }

        protected internal override JsValue Call(JsValue thisObject, JsValue[] arguments)
        {
            ExceptionHelper.ThrowTypeError(_realm, "Constructor DataView requires 'new'");
            return Undefined;
        }

        ObjectInstance IConstructor.Construct(JsValue[] arguments, JsValue newTarget)
        {
            if (newTarget.IsUndefined())
            {
                ExceptionHelper.ThrowTypeError(_realm);
            }

            var buffer = arguments.At(0) as ArrayBufferInstance;
            var byteOffset = arguments.At(1);
            var byteLength = arguments.At(2);

            if (buffer is null)
            {
                ExceptionHelper.ThrowTypeError(_realm, "First argument to DataView constructor must be an ArrayBuffer");
            }

            var offset = TypeConverter.ToIndex(_realm, byteOffset);

            if (buffer.IsDetachedBuffer)
            {
                ExceptionHelper.ThrowTypeError(_realm);
            }

            var bufferByteLength = (uint) buffer.ArrayBufferByteLength;
            if (offset > bufferByteLength)
            {
                ExceptionHelper.ThrowRangeError(_realm, "Start offset " + offset + " is outside the bounds of the buffer");
            }

            uint viewByteLength;
            if (byteLength.IsUndefined())
            {
                viewByteLength = bufferByteLength - offset;
            }
            else
            {
                viewByteLength = TypeConverter.ToIndex(_realm, byteLength);
                if (offset + viewByteLength > bufferByteLength)
                {
                    ExceptionHelper.ThrowRangeError(_realm, "Invalid DataView length");
                }
            }

            var o = OrdinaryCreateFromConstructor(
                newTarget,
                static intrinsics => intrinsics.DataView.PrototypeObject,
                static (Engine engine, Realm _, object? _) => new DataViewInstance(engine));

            if (buffer.IsDetachedBuffer)
            {
                ExceptionHelper.ThrowTypeError(_realm);
            }

            o._viewedArrayBuffer = buffer;
            o._byteLength = viewByteLength;
            o._byteOffset = offset;

            return o;
        }
    }
}
