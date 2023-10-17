using Jint.Collections;
using Jint.Native.Function;
using Jint.Native.Object;
using Jint.Runtime;
using Jint.Runtime.Descriptors;
using Jint.Runtime.Interop;

namespace Jint.Native.Symbol
{
    /// <summary>
    /// 19.4
    /// http://www.ecma-international.org/ecma-262/6.0/index.html#sec-symbol-objects
    /// </summary>
    internal sealed class SymbolConstructor : Constructor
    {
        private static readonly JsString _functionName = new JsString("Symbol");

        internal SymbolConstructor(
            Engine engine,
            Realm realm,
            FunctionPrototype functionPrototype,
            ObjectPrototype objectPrototype)
            : base(engine, realm, _functionName)
        {
            _prototype = functionPrototype;
            PrototypeObject = new SymbolPrototype(engine, realm, this, objectPrototype);
            _length = new DataPropertyDescriptor(JsNumber.PositiveZero, PropertyFlag.Configurable);
            _prototypeDescriptor = new DataPropertyDescriptor(PrototypeObject, PropertyFlag.AllForbidden);
        }

        public SymbolPrototype PrototypeObject { get; }

        protected override void Initialize()
        {
            const PropertyFlag lengthFlags = PropertyFlag.Configurable;
            const PropertyFlag propertyFlags = PropertyFlag.AllForbidden;

            var properties = new PropertyDictionary(15, checkExistingKeys: false)
            {
                ["for"] = new DataPropertyDescriptor(new ClrFunctionInstance(Engine, "for", For, 1, lengthFlags), PropertyFlag.Writable | PropertyFlag.Configurable),
                ["keyFor"] = new DataPropertyDescriptor(new ClrFunctionInstance(Engine, "keyFor", KeyFor, 1, lengthFlags), PropertyFlag.Writable | PropertyFlag.Configurable),
                ["hasInstance"] = new DataPropertyDescriptor(GlobalSymbolRegistry.HasInstance, propertyFlags),
                ["isConcatSpreadable"] = new DataPropertyDescriptor(GlobalSymbolRegistry.IsConcatSpreadable, propertyFlags),
                ["iterator"] = new DataPropertyDescriptor(GlobalSymbolRegistry.Iterator, propertyFlags),
                ["match"] = new DataPropertyDescriptor(GlobalSymbolRegistry.Match, propertyFlags),
                ["matchAll"] = new DataPropertyDescriptor(GlobalSymbolRegistry.MatchAll, propertyFlags),
                ["replace"] = new DataPropertyDescriptor(GlobalSymbolRegistry.Replace, propertyFlags),
                ["search"] = new DataPropertyDescriptor(GlobalSymbolRegistry.Search, propertyFlags),
                ["species"] = new DataPropertyDescriptor(GlobalSymbolRegistry.Species, propertyFlags),
                ["split"] = new DataPropertyDescriptor(GlobalSymbolRegistry.Split, propertyFlags),
                ["toPrimitive"] = new DataPropertyDescriptor(GlobalSymbolRegistry.ToPrimitive, propertyFlags),
                ["toStringTag"] = new DataPropertyDescriptor(GlobalSymbolRegistry.ToStringTag, propertyFlags),
                ["unscopables"] = new DataPropertyDescriptor(GlobalSymbolRegistry.Unscopables, propertyFlags),
                ["asyncIterator"] = new DataPropertyDescriptor(GlobalSymbolRegistry.AsyncIterator, propertyFlags)
            };
            SetProperties(properties);
        }

        /// <summary>
        /// http://www.ecma-international.org/ecma-262/6.0/index.html#sec-symbol-description
        /// </summary>
        protected internal override JsValue Call(JsValue thisObject, JsValue[] arguments)
        {
            var description = arguments.At(0);
            var descString = description.IsUndefined()
                ? Undefined
                : TypeConverter.ToJsString(description);

            var value = GlobalSymbolRegistry.CreateSymbol(descString);
            return value;
        }

        /// <summary>
        /// https://tc39.es/ecma262/#sec-symbol.for
        /// </summary>
        private JsValue For(JsValue thisObject, JsValue[] arguments)
        {
            var stringKey = TypeConverter.ToJsString(arguments.At(0));

            // 2. ReturnIfAbrupt(stringKey).

            if (!_engine.GlobalSymbolRegistry.TryGetSymbol(stringKey, out var symbol))
            {
                symbol = GlobalSymbolRegistry.CreateSymbol(stringKey);
                _engine.GlobalSymbolRegistry.Add(symbol);
            }

            return symbol;
        }

        /// <summary>
        /// https://tc39.es/ecma262/#sec-symbol.keyfor
        /// </summary>
        private JsValue KeyFor(JsValue thisObject, JsValue[] arguments)
        {
            var symbol = arguments.At(0) as JsSymbol;
            if (symbol is null)
            {
                ExceptionHelper.ThrowTypeError(_realm);
            }

            if (_engine.GlobalSymbolRegistry.TryGetSymbol(symbol._value, out var e))
            {
                return e._value;
            }

            return Undefined;
        }

        public override ObjectInstance Construct(JsValue[] arguments, JsValue newTarget)
        {
            ExceptionHelper.ThrowTypeError(_realm, "Symbol is not a constructor");
            return null;
        }

        public SymbolInstance Construct(JsSymbol symbol)
        {
            return new SymbolInstance(Engine, PrototypeObject, symbol);
        }
    }
}
