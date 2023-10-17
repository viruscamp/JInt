using Jint.Collections;
using Jint.Native.Object;
using Jint.Runtime;
using Jint.Runtime.Descriptors;

namespace Jint.Native.AggregateError;

/// <summary>
/// https://tc39.es/ecma262/#sec-properties-of-the-aggregate-error-prototype-objects
/// </summary>
internal sealed class AggregateErrorPrototype : Prototype
{
    private readonly AggregateErrorConstructor _constructor;

    internal AggregateErrorPrototype(
        Engine engine,
        Realm realm,
        AggregateErrorConstructor constructor,
        ObjectInstance prototype)
        : base(engine, realm)
    {
        _constructor = constructor;
        _prototype = prototype;
    }

    protected override void Initialize()
    {
        var properties = new PropertyDictionary(3, checkExistingKeys: false)
        {
            ["constructor"] = new DataPropertyDescriptor(_constructor, PropertyFlag.NonEnumerable),
            ["message"] = new DataPropertyDescriptor(JsString.Empty, PropertyFlag.Configurable | PropertyFlag.Writable),
            ["name"] = new DataPropertyDescriptor("AggregateError", PropertyFlag.Configurable | PropertyFlag.Writable),
        };
        SetProperties(properties);
    }
}
