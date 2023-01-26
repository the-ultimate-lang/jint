using Ultimate.Language.Jint.Native.Object;
using Ultimate.Language.Jint.Runtime;
using Ultimate.Language.Jint.Runtime.Descriptors;

namespace Ultimate.Language.Jint.Native.String;

internal class StringInstance : ObjectInstance, IPrimitiveInstance
{
    internal PropertyDescriptor? _length;

    public StringInstance(Engine engine, JsString value)
        : base(engine, ObjectClass.String)
    {
        StringData = value;
        _length = PropertyDescriptor.AllForbiddenDescriptor.ForNumber(value.Length);
    }

    Types IPrimitiveInstance.Type => Types.String;

    JsValue IPrimitiveInstance.PrimitiveValue => StringData;

    public JsString StringData { get; }

    private static bool IsInt32(double d, out int intValue)
    {
        if (d >= int.MinValue && d <= int.MaxValue)
        {
            intValue = (int) d;
            return intValue == d;
        }

        intValue = 0;
        return false;
    }

    public override PropertyDescriptor GetOwnProperty(JsValue property)
    {
        if (property == CommonProperties.Infinity)
        {
            return PropertyDescriptor.Undefined;
        }

        if (property == CommonProperties.Length)
        {
            return _length ?? PropertyDescriptor.Undefined;
        }

        var desc = base.GetOwnProperty(property);
        if (desc != PropertyDescriptor.Undefined)
        {
            return desc;
        }

        if ((property._type & (InternalTypes.Number | InternalTypes.Integer | InternalTypes.String)) == 0)
        {
            return PropertyDescriptor.Undefined;
        }

        var str = StringData.ToString();
        var number = TypeConverter.ToNumber(property);
        if (!IsInt32(number, out var index) || index < 0 || index >= str.Length)
        {
            return PropertyDescriptor.Undefined;
        }

        return new PropertyDescriptor(str[index], PropertyFlag.OnlyEnumerable);
    }

    public override IEnumerable<KeyValuePair<JsValue, PropertyDescriptor>> GetOwnProperties()
    {
        foreach (var entry in base.GetOwnProperties())
        {
            yield return entry;
        }

        if (_length != null)
        {
            yield return new KeyValuePair<JsValue, PropertyDescriptor>(CommonProperties.Length, _length);
        }
    }

    internal override IEnumerable<JsValue> GetInitialOwnStringPropertyKeys()
    {
        return new[] { JsString.LengthString };
    }

    public override List<JsValue> GetOwnPropertyKeys(Types types)
    {
        var keys = new List<JsValue>(StringData.Length + 1);
        if ((types & Types.String) != 0)
        {
            for (uint i = 0; i < StringData.Length; ++i)
            {
                keys.Add(JsString.Create(i));
            }

            keys.AddRange(base.GetOwnPropertyKeys(Types.String));
        }

        if ((types & Types.Symbol) != 0)
        {
            keys.AddRange(base.GetOwnPropertyKeys(Types.Symbol));
        }

        return keys;
    }

    protected internal override void SetOwnProperty(JsValue property, PropertyDescriptor desc)
    {
        if (property == CommonProperties.Length)
        {
            _length = desc;
        }
        else
        {
            base.SetOwnProperty(property, desc);
        }
    }

    public override void RemoveOwnProperty(JsValue property)
    {
        if (property == CommonProperties.Length)
        {
            _length = null;
        }

        base.RemoveOwnProperty(property);
    }
}
