using Jint.Collections;
using Jint.Native.Object;
using Jint.Native.Symbol;
using Jint.Runtime;
using Jint.Runtime.Descriptors;
using Jint.Runtime.Interop;

namespace Jint.Native.WeakMap;

/// <summary>
/// https://tc39.es/ecma262/#sec-weakmap-objects
/// </summary>
internal sealed class WeakMapPrototype : Prototype
{
    private readonly WeakMapConstructor _constructor;

    internal WeakMapPrototype(
        Engine engine,
        Realm realm,
        WeakMapConstructor constructor,
        ObjectPrototype prototype) : base(engine, realm)
    {
        _prototype = prototype;
        _constructor = constructor;
    }

    protected override void Initialize()
    {
        const PropertyFlag propertyFlags = PropertyFlag.Configurable | PropertyFlag.Writable;
        var properties = new PropertyDictionary(6, checkExistingKeys: false)
        {
            ["length"] = new DataPropertyDescriptor(0, PropertyFlag.Configurable),
            ["constructor"] = new DataPropertyDescriptor(_constructor, PropertyFlag.NonEnumerable),
            ["delete"] = new DataPropertyDescriptor(new ClrFunctionInstance(Engine, "delete", Delete, 1, PropertyFlag.Configurable), propertyFlags),
            ["get"] = new DataPropertyDescriptor(new ClrFunctionInstance(Engine, "get", Get, 1, PropertyFlag.Configurable), propertyFlags),
            ["has"] = new DataPropertyDescriptor(new ClrFunctionInstance(Engine, "has", Has, 1, PropertyFlag.Configurable), propertyFlags),
            ["set"] = new DataPropertyDescriptor(new ClrFunctionInstance(Engine, "set", Set, 2, PropertyFlag.Configurable), propertyFlags),
        };
        SetProperties(properties);

        var symbols = new SymbolDictionary(1)
        {
            [GlobalSymbolRegistry.ToStringTag] = new DataPropertyDescriptor("WeakMap", false, false, true)
        };
        SetSymbols(symbols);
    }

    private JsValue Get(JsValue thisObject, JsValue[] arguments)
    {
        var map = AssertWeakMapInstance(thisObject);
        return map.WeakMapGet(arguments.At(0));
    }

    private JsValue Delete(JsValue thisObject, JsValue[] arguments)
    {
        var map = AssertWeakMapInstance(thisObject);
        return arguments.Length > 0 && map.WeakMapDelete(arguments.At(0)) ? JsBoolean.True : JsBoolean.False;
    }

    private JsValue Set(JsValue thisObject, JsValue[] arguments)
    {
        var map = AssertWeakMapInstance(thisObject);
        map.WeakMapSet(arguments.At(0), arguments.At(1));
        return thisObject;
    }

    private JsValue Has(JsValue thisObject, JsValue[] arguments)
    {
        var map = AssertWeakMapInstance(thisObject);
        return map.WeakMapHas(arguments.At(0)) ? JsBoolean.True : JsBoolean.False;
    }

    private JsWeakMap AssertWeakMapInstance(JsValue thisObject)
    {
        if (thisObject is JsWeakMap map)
        {
            return map;
        }

        ExceptionHelper.ThrowTypeError(_realm, "object must be a WeakMap");
        return default;
    }
}
