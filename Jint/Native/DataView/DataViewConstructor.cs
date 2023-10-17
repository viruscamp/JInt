using Jint.Native.ArrayBuffer;
using Jint.Native.Function;
using Jint.Native.Object;
using Jint.Runtime;
using Jint.Runtime.Descriptors;

namespace Jint.Native.DataView
{
    /// <summary>
    /// https://tc39.es/ecma262/#sec-dataview-constructor
    /// </summary>
    internal sealed class DataViewConstructor : Constructor
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
            _length = new DataPropertyDescriptor(1, PropertyFlag.Configurable);
            _prototypeDescriptor = new DataPropertyDescriptor(PrototypeObject, PropertyFlag.AllForbidden);
        }

        private DataViewPrototype PrototypeObject { get; }

        public override ObjectInstance Construct(JsValue[] arguments, JsValue newTarget)
        {
            if (newTarget.IsUndefined())
            {
                ExceptionHelper.ThrowTypeError(_realm);
            }

            var buffer = arguments.At(0) as JsArrayBuffer;
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
                static (Engine engine, Realm _, object? _) => new JsDataView(engine));

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
