using System.Diagnostics;
using System.Runtime.CompilerServices;
using Jint.Collections;
using Jint.Native;
using Jint.Native.Object;

namespace Jint.Runtime.Descriptors
{
    [DebuggerDisplay("Value: {Value}, Flags: {Flags}")]
    public abstract class PropertyDescriptor
    {
        public static readonly PropertyDescriptor Undefined = new UndefinedPropertyDescriptor();

        internal PropertyFlag _flags;
        internal JsValue? _value;

        protected PropertyDescriptor() : this(PropertyFlag.None)
        {
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        protected PropertyDescriptor(PropertyFlag flags)
        {
            _flags = flags;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        protected PropertyDescriptor(bool? writable, bool? enumerable, bool? configurable)
        {
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

        protected PropertyDescriptor(PropertyDescriptor descriptor)
        {
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

        // TODO change Type to JsValue? to allow null
        // the old commits use PropertyFlag.CustomJsValue and CustomValue to avoid virtual method and force inline
        public virtual JsValue Value
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => _value!;
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

            PropertyDescriptor desc = hasGet || hasSet
                ? new GetSetPropertyDescriptor(null, null, PropertyFlag.None)
                : new DataPropertyDescriptor(PropertyFlag.None);

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
                properties["value"] = new DataPropertyDescriptor(desc.Value ?? JsValue.Undefined, PropertyFlag.ConfigurableEnumerableWritable);
                if (desc._flags != PropertyFlag.None || desc.WritableSet)
                {
                    properties["writable"] = new DataPropertyDescriptor(desc.Writable, PropertyFlag.ConfigurableEnumerableWritable);
                }
            }
            else if (desc is INonData)
            {
                properties["get"] = new DataPropertyDescriptor(desc.Get ?? JsValue.Undefined, PropertyFlag.ConfigurableEnumerableWritable);
                properties["set"] = new DataPropertyDescriptor(desc.Set ?? JsValue.Undefined, PropertyFlag.ConfigurableEnumerableWritable);
            }

            if (!strictUndefined || desc.EnumerableSet)
            {
                properties["enumerable"] = new DataPropertyDescriptor(desc.Enumerable, PropertyFlag.ConfigurableEnumerableWritable);
            }

            if (!strictUndefined || desc.ConfigurableSet)
            {
                properties["configurable"] = new DataPropertyDescriptor(desc.Configurable, PropertyFlag.ConfigurableEnumerableWritable);
            }

            obj.SetProperties(properties);
            return obj;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool IsAccessorDescriptor()
        {
            if (this is INonData)
            {
                return !ReferenceEquals(Get, null) || !ReferenceEquals(Set, null);
            }
            return false;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool IsDataDescriptor()
        {
            if (this is INonData)
            {
                return false;
            }
            return (_flags & (PropertyFlag.WritableSet | PropertyFlag.Writable)) != 0
                   || !ReferenceEquals(Value, null);
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
                var val = Value;

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

        private sealed class UndefinedPropertyDescriptor : PropertyDescriptor, INonData
        {
            public UndefinedPropertyDescriptor() : base(PropertyFlag.None)
            {
            }

            public override JsValue Value
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

            public override JsValue Value
            {
                [MethodImpl(MethodImplOptions.AggressiveInlining)]
                get => _value!;
                set => ExceptionHelper.ThrowNotImplementedException();
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
                    : new DataPropertyDescriptor(number, PropertyFlag.AllForbidden);
            }
        }
    }

    public sealed class DataPropertyDescriptor : PropertyDescriptor
    {
        public DataPropertyDescriptor() : this(PropertyFlag.None)
        {
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal DataPropertyDescriptor(PropertyFlag flags) : base(flags)
        {
            _flags = flags;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal DataPropertyDescriptor(JsValue? value, PropertyFlag flags) : base(flags)
        {
            _value = value!;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public DataPropertyDescriptor(JsValue? value, bool? writable, bool? enumerable, bool? configurable)
            : base(writable, enumerable, configurable)
        {
            _value = value!;
        }

        public DataPropertyDescriptor(PropertyDescriptor descriptor)
            : base(descriptor)
        {
            _value = descriptor.Value;
        }

        public override JsValue Value
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => _value!;
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            set => _value = value;
        }
    }
}
