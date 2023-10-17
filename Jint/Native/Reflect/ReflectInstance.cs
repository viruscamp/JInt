using Jint.Collections;
using Jint.Native.Function;
using Jint.Native.Object;
using Jint.Native.Symbol;
using Jint.Runtime;
using Jint.Runtime.Descriptors;
using Jint.Runtime.Interop;

namespace Jint.Native.Reflect
{
    /// <summary>
    /// https://www.ecma-international.org/ecma-262/6.0/index.html#sec-reflect-object
    /// </summary>
    internal sealed class ReflectInstance : ObjectInstance
    {
        private readonly Realm _realm;

        internal ReflectInstance(
            Engine engine,
            Realm realm,
            ObjectPrototype objectPrototype) : base(engine)
        {
            _realm = realm;
            _prototype = objectPrototype;
        }

        protected override void Initialize()
        {
            var properties = new PropertyDictionary(14, checkExistingKeys: false)
            {
                ["apply"] = new DataPropertyDescriptor(new ClrFunctionInstance(Engine, "apply", Apply, 3, PropertyFlag.Configurable), true, false, true),
                ["construct"] = new DataPropertyDescriptor(new ClrFunctionInstance(Engine, "construct", Construct, 2, PropertyFlag.Configurable), true, false, true),
                ["defineProperty"] = new DataPropertyDescriptor(new ClrFunctionInstance(Engine, "defineProperty", DefineProperty, 3, PropertyFlag.Configurable), true, false, true),
                ["deleteProperty"] = new DataPropertyDescriptor(new ClrFunctionInstance(Engine, "deleteProperty", DeleteProperty, 2, PropertyFlag.Configurable), true, false, true),
                ["get"] = new DataPropertyDescriptor(new ClrFunctionInstance(Engine, "get", Get, 2, PropertyFlag.Configurable), true, false, true),
                ["getOwnPropertyDescriptor"] = new DataPropertyDescriptor(new ClrFunctionInstance(Engine, "getOwnPropertyDescriptor", GetOwnPropertyDescriptor, 2, PropertyFlag.Configurable), true, false, true),
                ["getPrototypeOf"] = new DataPropertyDescriptor(new ClrFunctionInstance(Engine, "getPrototypeOf", GetPrototypeOf, 1, PropertyFlag.Configurable), true, false, true),
                ["has"] = new DataPropertyDescriptor(new ClrFunctionInstance(Engine, "has", Has, 2, PropertyFlag.Configurable), true, false, true),
                ["isExtensible"] = new DataPropertyDescriptor(new ClrFunctionInstance(Engine, "isExtensible", IsExtensible, 1, PropertyFlag.Configurable), true, false, true),
                ["ownKeys"] = new DataPropertyDescriptor(new ClrFunctionInstance(Engine, "ownKeys", OwnKeys, 1, PropertyFlag.Configurable), true, false, true),
                ["preventExtensions"] = new DataPropertyDescriptor(new ClrFunctionInstance(Engine, "preventExtensions", PreventExtensions, 1, PropertyFlag.Configurable), true, false, true),
                ["set"] = new DataPropertyDescriptor(new ClrFunctionInstance(Engine, "set", Set, 3, PropertyFlag.Configurable), true, false, true),
                ["setPrototypeOf"] = new DataPropertyDescriptor(new ClrFunctionInstance(Engine, "setPrototypeOf", SetPrototypeOf, 2, PropertyFlag.Configurable), true, false, true),
            };
            SetProperties(properties);

            var symbols = new SymbolDictionary(1)
            {
                [GlobalSymbolRegistry.ToStringTag] = new DataPropertyDescriptor("Reflect", false, false, true)
            };
            SetSymbols(symbols);
        }

        private JsValue Apply(JsValue thisObject, JsValue[] arguments)
        {
            var target = arguments.At(0);
            var thisArgument = arguments.At(1);
            var argumentsList = arguments.At(2);

            if (!target.IsCallable)
            {
                ExceptionHelper.ThrowTypeError(_realm);
            }

            var args = FunctionPrototype.CreateListFromArrayLike(_realm, argumentsList);

            // 3. Perform PrepareForTailCall().

            return ((ICallable) target).Call(thisArgument, args);
        }

        /// <summary>
        /// https://tc39.es/ecma262/#sec-reflect.construct
        /// </summary>
        private JsValue Construct(JsValue thisObject, JsValue[] arguments)
        {
            var target = AssertConstructor(_engine, arguments.At(0));

            var newTargetArgument = arguments.At(2, arguments[0]);
            AssertConstructor(_engine, newTargetArgument);

            var args = FunctionPrototype.CreateListFromArrayLike(_realm, arguments.At(1));

            return target.Construct(args, newTargetArgument);
        }

        /// <summary>
        /// https://tc39.es/ecma262/#sec-reflect.defineproperty
        /// </summary>
        private JsValue DefineProperty(JsValue thisObject, JsValue[] arguments)
        {
            var target = arguments.At(0) as ObjectInstance;
            if (target is null)
            {
                ExceptionHelper.ThrowTypeError(_realm, "Reflect.defineProperty called on non-object");
            }

            var propertyKey = arguments.At(1);
            var attributes = arguments.At(2);

            var key = TypeConverter.ToPropertyKey(propertyKey);
            var desc = PropertyDescriptor.ToPropertyDescriptor(_realm, attributes);

            return target.DefineOwnProperty(key, desc);
        }

        private JsValue DeleteProperty(JsValue thisObject, JsValue[] arguments)
        {
            var o = arguments.At(0) as ObjectInstance;
            if (o is null)
            {
                ExceptionHelper.ThrowTypeError(_realm, "Reflect.deleteProperty called on non-object");
            }

            var property = TypeConverter.ToPropertyKey(arguments.At(1));
            return o.Delete(property) ? JsBoolean.True : JsBoolean.False;
        }

        private JsValue Has(JsValue thisObject, JsValue[] arguments)
        {
            var o = arguments.At(0) as ObjectInstance;
            if (o is null)
            {
                ExceptionHelper.ThrowTypeError(_realm, "Reflect.has called on non-object");
            }

            var property = TypeConverter.ToPropertyKey(arguments.At(1));
            return o.HasProperty(property) ? JsBoolean.True : JsBoolean.False;
        }

        private JsValue Set(JsValue thisObject, JsValue[] arguments)
        {
            var target = arguments.At(0);
            var property = TypeConverter.ToPropertyKey(arguments.At(1));
            var value = arguments.At(2);
            var receiver = arguments.At(3, target);

            var o = target as ObjectInstance;
            if (o is null)
            {
                ExceptionHelper.ThrowTypeError(_realm, "Reflect.set called on non-object");
            }

            return o.Set(property, value, receiver);
        }

        private JsValue Get(JsValue thisObject, JsValue[] arguments)
        {
            var target = arguments.At(0);
            var o = target as ObjectInstance;
            if (o is null)
            {
                ExceptionHelper.ThrowTypeError(_realm, "Reflect.get called on non-object");
            }

            var receiver = arguments.At(2, target);
            var property = TypeConverter.ToPropertyKey(arguments.At(1));
            return o.Get(property, receiver);
        }

        private JsValue GetOwnPropertyDescriptor(JsValue thisObject, JsValue[] arguments)
        {
            if (!arguments.At(0).IsObject())
            {
                ExceptionHelper.ThrowTypeError(_realm, "Reflect.getOwnPropertyDescriptor called on non-object");
            }
            return _realm.Intrinsics.Object.GetOwnPropertyDescriptor(Undefined, arguments);
        }

        private JsValue OwnKeys(JsValue thisObject, JsValue[] arguments)
        {
            var o = arguments.At(0) as ObjectInstance;
            if (o is null)
            {
                ExceptionHelper.ThrowTypeError(_realm, "Reflect.get called on non-object");
            }

            var keys = o.GetOwnPropertyKeys();
            return _realm.Intrinsics.Array.CreateArrayFromList(keys);
        }

        private JsValue IsExtensible(JsValue thisObject, JsValue[] arguments)
        {
            var o = arguments.At(0) as ObjectInstance;
            if (o is null)
            {
                ExceptionHelper.ThrowTypeError(_realm, "Reflect.isExtensible called on non-object");
            }

            return o.Extensible;
        }

        private JsValue PreventExtensions(JsValue thisObject, JsValue[] arguments)
        {
            var o = arguments.At(0) as ObjectInstance;
            if (o is null)
            {
                ExceptionHelper.ThrowTypeError(_realm, "Reflect.preventExtensions called on non-object");
            }

            return o.PreventExtensions();
        }

        private JsValue GetPrototypeOf(JsValue thisObject, JsValue[] arguments)
        {
            var target = arguments.At(0);

            if (!target.IsObject())
            {
                ExceptionHelper.ThrowTypeError(_realm, "Reflect.getPrototypeOf called on non-object");
            }

            return _realm.Intrinsics.Object.GetPrototypeOf(Undefined, arguments);
        }

        private JsValue SetPrototypeOf(JsValue thisObject, JsValue[] arguments)
        {
            var target = arguments.At(0);

            var o = target as ObjectInstance;
            if (o is null)
            {
                ExceptionHelper.ThrowTypeError(_realm, "Reflect.setPrototypeOf called on non-object");
            }

            var prototype = arguments.At(1);
            if (!prototype.IsObject() && !prototype.IsNull())
            {
                ExceptionHelper.ThrowTypeError(_realm, $"Object prototype may only be an Object or null: {prototype}");
            }

            return o.SetPrototypeOf(prototype);
        }
    }
}
