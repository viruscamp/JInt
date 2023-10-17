using Jint.Native.Function;
using Jint.Native.Object;
using Jint.Runtime;
using Jint.Runtime.Descriptors;

namespace Jint.Native.Intl;

/// <summary>
/// https://tc39.es/ecma402/#sec-the-intl-collator-constructor
/// </summary>
internal sealed class CollatorConstructor : Constructor
{
    private static readonly JsString _functionName = new("Collator");

    public CollatorConstructor(
        Engine engine,
        Realm realm,
        FunctionPrototype functionPrototype,
        ObjectPrototype objectPrototype) : base(engine, realm, _functionName)
    {
        _prototype = functionPrototype;
        PrototypeObject = new CollatorPrototype(engine, realm, this, objectPrototype);
        _length = new DataPropertyDescriptor(JsNumber.PositiveZero, PropertyFlag.Configurable);
        _prototypeDescriptor = new DataPropertyDescriptor(PrototypeObject, PropertyFlag.AllForbidden);
    }

    public CollatorPrototype PrototypeObject { get; }

    public override ObjectInstance Construct(JsValue[] arguments, JsValue newTarget)
    {
        throw new NotImplementedException();
    }
}
