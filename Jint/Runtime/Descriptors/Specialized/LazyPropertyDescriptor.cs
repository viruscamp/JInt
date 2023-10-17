using System.Runtime.CompilerServices;
using Jint.Native;

namespace Jint.Runtime.Descriptors.Specialized
{
    internal sealed class LazyPropertyDescriptor : PropertyDescriptor
    {
        private readonly object? _state;
        private readonly Func<object?, JsValue> _resolver;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal LazyPropertyDescriptor(object? state, Func<object?, JsValue> resolver, PropertyFlag flags)
            : base(flags)
        {
            _state = state;
            _resolver = resolver;
        }

        public override JsValue Value
        {
            get => _value ??= _resolver(_state);
            set => _value = value;
        }
    }
}
