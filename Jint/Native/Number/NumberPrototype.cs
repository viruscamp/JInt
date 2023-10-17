using System.Diagnostics;
using System.Globalization;
using System.Text;
using Jint.Collections;
using Jint.Native.Number.Dtoa;
using Jint.Native.Object;
using Jint.Pooling;
using Jint.Runtime;
using Jint.Runtime.Descriptors;
using Jint.Runtime.Interop;

namespace Jint.Native.Number
{
    /// <summary>
    /// https://tc39.es/ecma262/#sec-properties-of-the-number-prototype-object
    /// </summary>
    internal sealed class NumberPrototype : NumberInstance
    {
        private readonly Realm _realm;
        private readonly NumberConstructor _constructor;

        internal NumberPrototype(
            Engine engine,
            Realm realm,
            NumberConstructor constructor,
            ObjectPrototype objectPrototype)
            : base(engine, InternalTypes.Object | InternalTypes.PlainObject)
        {
            _prototype = objectPrototype;
            _realm = realm;
            _constructor = constructor;
        }

        protected override void Initialize()
        {
            var properties = new PropertyDictionary(8, checkExistingKeys: false)
            {
                ["constructor"] = new DataPropertyDescriptor(_constructor, true, false, true),
                ["toString"] = new DataPropertyDescriptor(new ClrFunctionInstance(Engine, "toString", ToNumberString, 1, PropertyFlag.Configurable), true, false, true),
                ["toLocaleString"] = new DataPropertyDescriptor(new ClrFunctionInstance(Engine, "toLocaleString", ToLocaleString, 0, PropertyFlag.Configurable), true, false, true),
                ["valueOf"] = new DataPropertyDescriptor(new ClrFunctionInstance(Engine, "valueOf", ValueOf, 0, PropertyFlag.Configurable), true, false, true),
                ["toFixed"] = new DataPropertyDescriptor(new ClrFunctionInstance(Engine, "toFixed", ToFixed, 1, PropertyFlag.Configurable), true, false, true),
                ["toExponential"] = new DataPropertyDescriptor(new ClrFunctionInstance(Engine, "toExponential", ToExponential, 1, PropertyFlag.Configurable), true, false, true),
                ["toPrecision"] = new DataPropertyDescriptor(new ClrFunctionInstance(Engine, "toPrecision", ToPrecision, 1, PropertyFlag.Configurable), true, false, true)
            };
            SetProperties(properties);
        }

        private JsValue ToLocaleString(JsValue thisObject, JsValue[] arguments)
        {
            if (!thisObject.IsNumber() && ReferenceEquals(thisObject.TryCast<NumberInstance>(), null))
            {
                ExceptionHelper.ThrowTypeError(_realm);
            }

            var m = TypeConverter.ToNumber(thisObject);

            if (double.IsNaN(m))
            {
                return "NaN";
            }

            if (m == 0)
            {
                return JsString.NumberZeroString;
            }

            if (m < 0)
            {
                return "-" + ToLocaleString(-m, arguments);
            }

            if (double.IsPositiveInfinity(m) || m >= double.MaxValue)
            {
                return "Infinity";
            }

            if (double.IsNegativeInfinity(m) || m <= -double.MaxValue)
            {
                return "-Infinity";
            }

            return m.ToString("n", Engine.Options.Culture);
        }

        private JsValue ValueOf(JsValue thisObject, JsValue[] arguments)
        {
            if (thisObject is NumberInstance ni)
            {
                return ni.NumberData;
            }

            if (thisObject is JsNumber)
            {
                return thisObject;
            }

            ExceptionHelper.ThrowTypeError(_realm);
            return null;
        }

        private const double Ten21 = 1e21;

        private JsValue ToFixed(JsValue thisObject, JsValue[] arguments)
        {
            var f = (int) TypeConverter.ToInteger(arguments.At(0, 0));
            if (f < 0 || f > 100)
            {
                ExceptionHelper.ThrowRangeError(_realm, "fractionDigits argument must be between 0 and 100");
            }

            // limitation with .NET, max is 99
            if (f == 100)
            {
                ExceptionHelper.ThrowRangeError(_realm, "100 fraction digits is not supported due to .NET format specifier limitation");
            }

            var x = TypeConverter.ToNumber(thisObject);

            if (double.IsNaN(x))
            {
                return "NaN";
            }

            if (x >= Ten21)
            {
                return ToNumberString(x);
            }

            // handle non-decimal with greater precision
            if (System.Math.Abs(x - (long) x) < JsNumber.DoubleIsIntegerTolerance)
            {
                return ((long) x).ToString("f" + f, CultureInfo.InvariantCulture);
            }

            return x.ToString("f" + f, CultureInfo.InvariantCulture);
        }

        /// <summary>
        /// https://www.ecma-international.org/ecma-262/6.0/#sec-number.prototype.toexponential
        /// </summary>
        private JsValue ToExponential(JsValue thisObject, JsValue[] arguments)
        {
            if (!thisObject.IsNumber() && ReferenceEquals(thisObject.TryCast<NumberInstance>(), null))
            {
                ExceptionHelper.ThrowTypeError(_realm);
            }

            var x = TypeConverter.ToNumber(thisObject);
            var fractionDigits = arguments.At(0);
            if (fractionDigits.IsUndefined())
            {
                fractionDigits = JsNumber.PositiveZero;
            }

            var f = (int) TypeConverter.ToInteger(fractionDigits);

            if (double.IsNaN(x))
            {
                return "NaN";
            }

            if (double.IsInfinity(x))
            {
                return thisObject.ToString();
            }

            if (f < 0 || f > 100)
            {
                ExceptionHelper.ThrowRangeError(_realm, "fractionDigits argument must be between 0 and 100");
            }

            if (arguments.At(0).IsUndefined())
            {
                f = -1;
            }

            bool negative = false;
            if (x < 0)
            {
                x = -x;
                negative = true;
            }

            int decimalPoint;
            DtoaBuilder dtoaBuilder;
            if (f == -1)
            {
                dtoaBuilder = new DtoaBuilder();
                DtoaNumberFormatter.DoubleToAscii(
                    dtoaBuilder,
                    x,
                    DtoaMode.Shortest,
                    requested_digits: 0,
                    out _,
                    out decimalPoint);
                f = dtoaBuilder.Length - 1;
            }
            else
            {
                dtoaBuilder = new DtoaBuilder(101);
                DtoaNumberFormatter.DoubleToAscii(
                    dtoaBuilder,
                    x,
                    DtoaMode.Precision,
                    requested_digits: f + 1,
                    out _,
                    out decimalPoint);
            }

            Debug.Assert(dtoaBuilder.Length > 0);
            Debug.Assert(dtoaBuilder.Length <= f + 1);

            int exponent = decimalPoint - 1;
            var result = CreateExponentialRepresentation(dtoaBuilder, exponent, negative, f+1);
            return result;
        }

        private JsValue ToPrecision(JsValue thisObject, JsValue[] arguments)
        {
            if (!thisObject.IsNumber() && ReferenceEquals(thisObject.TryCast<NumberInstance>(), null))
            {
                ExceptionHelper.ThrowTypeError(_realm);
            }

            var x = TypeConverter.ToNumber(thisObject);
            var precisionArgument = arguments.At(0);

            if (precisionArgument.IsUndefined())
            {
                return TypeConverter.ToString(x);
            }

            var p = (int) TypeConverter.ToInteger(precisionArgument);

            if (double.IsNaN(x))
            {
                return "NaN";
            }

            if (double.IsInfinity(x))
            {
                return thisObject.ToString();
            }

            if (p < 1 || p > 100)
            {
                ExceptionHelper.ThrowRangeError(_realm, "precision must be between 1 and 100");
            }

            var dtoaBuilder = new DtoaBuilder(101);
            DtoaNumberFormatter.DoubleToAscii(
                dtoaBuilder,
                x,
                DtoaMode.Precision,
                p,
                out var negative,
                out var decimalPoint);


            int exponent = decimalPoint - 1;
            if (exponent < -6 || exponent >= p)
            {
                return CreateExponentialRepresentation(dtoaBuilder, exponent, negative, p);
            }

            using (var builder = StringBuilderPool.Rent())
            {
                // Use fixed notation.
                if (negative)
                {
                    builder.Builder.Append('-');
                }

                if (decimalPoint <= 0)
                {
                    builder.Builder.Append("0.");
                    builder.Builder.Append('0', -decimalPoint);
                    builder.Builder.Append(dtoaBuilder._chars, 0, dtoaBuilder.Length);
                    builder.Builder.Append('0', p - dtoaBuilder.Length);
                }
                else
                {
                    int m = System.Math.Min(dtoaBuilder.Length, decimalPoint);
                    builder.Builder.Append(dtoaBuilder._chars, 0, m);
                    builder.Builder.Append('0', System.Math.Max(0, decimalPoint - dtoaBuilder.Length));
                    if (decimalPoint < p)
                    {
                        builder.Builder.Append('.');
                        var extra = negative ? 2 : 1;
                        if (dtoaBuilder.Length > decimalPoint)
                        {
                            int len = dtoaBuilder.Length - decimalPoint;
                            int n = System.Math.Min(len, p - (builder.Builder.Length - extra));
                            builder.Builder.Append(dtoaBuilder._chars, decimalPoint, n);
                        }

                        builder.Builder.Append('0', System.Math.Max(0, extra + (p - builder.Builder.Length)));
                    }
                }

                return builder.ToString();
            }
        }

        private string CreateExponentialRepresentation(
            DtoaBuilder buffer,
            int exponent,
            bool negative,
            int significantDigits)
        {
            bool negativeExponent = false;
            if (exponent < 0)
            {
                negativeExponent = true;
                exponent = -exponent;
            }

            using (var builder = StringBuilderPool.Rent())
            {
                if (negative)
                {
                    builder.Builder.Append('-');
                }
                builder.Builder.Append(buffer._chars[0]);
                if (significantDigits != 1)
                {
                    builder.Builder.Append('.');
                    builder.Builder.Append(buffer._chars, 1, buffer.Length - 1);
                    int length = buffer.Length;
                    builder.Builder.Append('0', significantDigits - length);
                }

                builder.Builder.Append('e');
                builder.Builder.Append(negativeExponent ? '-' : '+');
                builder.Builder.Append(exponent);
                return builder.ToString();
            }
        }

        private JsValue ToNumberString(JsValue thisObject, JsValue[] arguments)
        {
            if (!thisObject.IsNumber() && (ReferenceEquals(thisObject.TryCast<NumberInstance>(), null)))
            {
                ExceptionHelper.ThrowTypeError(_realm);
            }

            var radix = arguments.At(0).IsUndefined()
                ? 10
                : (int) TypeConverter.ToInteger(arguments.At(0));

            if (radix < 2 || radix > 36)
            {
                ExceptionHelper.ThrowRangeError(_realm, "radix must be between 2 and 36");
            }

            var x = TypeConverter.ToNumber(thisObject);

            if (double.IsNaN(x))
            {
                return "NaN";
            }

            if (x == 0)
            {
                return JsString.NumberZeroString;
            }

            if (double.IsPositiveInfinity(x) || x >= double.MaxValue)
            {
                return "Infinity";
            }

            if (x < 0)
            {
                return "-" + ToNumberString(-x, arguments);
            }

            if (radix == 10)
            {
                return ToNumberString(x);
            }

            var integer = (long) x;
            var fraction = x -  integer;

            string result = ToBase(integer, radix);
            if (fraction != 0)
            {
                result += "." + ToFractionBase(fraction, radix);
            }

            return result;
        }

        public string ToBase(long n, int radix)
        {
            const string digits = "0123456789abcdefghijklmnopqrstuvwxyz";
            if (n == 0)
            {
                return "0";
            }

            using (var result = StringBuilderPool.Rent())
            {
                while (n > 0)
                {
                    var digit = (int) (n % radix);
                    n = n / radix;
                    result.Builder.Insert(0, digits[digit]);
                }

                return result.ToString();
            }
        }

        public string ToFractionBase(double n, int radix)
        {
            // based on the repeated multiplication method
            // http://www.mathpath.org/concepts/Num/frac.htm

            const string digits = "0123456789abcdefghijklmnopqrstuvwxyz";
            if (n == 0)
            {
                return "0";
            }

            using (var result = StringBuilderPool.Rent())
            {
                while (n > 0 && result.Length < 50) // arbitrary limit
                {
                    var c = n*radix;
                    var d = (int) c;
                    n = c - d;

                    result.Builder.Append(digits[d]);
                }

                return result.ToString();
            }
        }

        private string ToNumberString(double m)
        {
            using var stringBuilder = StringBuilderPool.Rent();
            NumberToString(m, new DtoaBuilder(), stringBuilder.Builder);
            return stringBuilder.Builder.ToString();
        }

        internal static void NumberToString(
            double m,
            DtoaBuilder builder,
            StringBuilder stringBuilder)
        {
            if (double.IsNaN(m))
            {
                stringBuilder.Append("NaN");
                return;
            }

            if (m == 0)
            {
                stringBuilder.Append('0');
                return;
            }

            if (double.IsInfinity(m))
            {
                stringBuilder.Append(double.IsNegativeInfinity(m) ? "-Infinity" : "Infinity");
                return;
            }

            DtoaNumberFormatter.DoubleToAscii(
                builder,
                m,
                DtoaMode.Shortest,
                0,
                out var negative,
                out var decimal_point);

            if (negative)
            {
                stringBuilder.Append('-');
            }

            if (builder.Length <= decimal_point && decimal_point <= 21)
            {
                // ECMA-262 section 9.8.1 step 6.
                stringBuilder.Append(builder._chars, 0, builder.Length);
                stringBuilder.Append('0', decimal_point - builder.Length);
            }
            else if (0 < decimal_point && decimal_point <= 21)
            {
                // ECMA-262 section 9.8.1 step 7.
                stringBuilder.Append(builder._chars, 0, decimal_point);
                stringBuilder.Append('.');
                stringBuilder.Append(builder._chars, decimal_point, builder.Length - decimal_point);
            }
            else if (decimal_point <= 0 && decimal_point > -6)
            {
                // ECMA-262 section 9.8.1 step 8.
                stringBuilder.Append("0.");
                stringBuilder.Append('0', -decimal_point);
                stringBuilder.Append(builder._chars, 0, builder.Length);
            }
            else
            {
                // ECMA-262 section 9.8.1 step 9 and 10 combined.
                stringBuilder.Append(builder._chars[0]);
                if (builder.Length != 1)
                {
                    stringBuilder.Append('.');
                    stringBuilder.Append(builder._chars, 1, builder.Length - 1);
                }

                stringBuilder.Append('e');
                stringBuilder.Append((decimal_point >= 0) ? '+' : '-');
                int exponent = decimal_point - 1;
                if (exponent < 0)
                {
                    exponent = -exponent;
                }

                stringBuilder.Append(exponent);
            }
        }
    }
}
