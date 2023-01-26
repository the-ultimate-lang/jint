using Ultimate.Language.Jint.Runtime;

namespace Ultimate.Language.Jint.Native;

public sealed class JsBoolean : JsValue, IEquatable<JsBoolean>
{
    public static readonly JsBoolean False = new JsBoolean(false);
    public static readonly JsBoolean True = new JsBoolean(true);

    internal static readonly object BoxedTrue = true;
    internal static readonly object BoxedFalse = false;

    internal readonly bool _value;

    private JsBoolean(bool value) : base(Types.Boolean)
    {
        _value = value;
    }

    internal static JsBoolean Create(bool value) => value ? True : False;

    public override object ToObject() => _value ? BoxedTrue : BoxedFalse;

    internal override bool ToBoolean() => _value;

    public override string ToString()
    {
        return _value ? "true" : "false";
    }

    public override bool IsLooselyEqual(JsValue value)
    {
        if (value is JsBoolean jsBoolean)
        {
            return Equals(jsBoolean);
        }

        return !value.IsNullOrUndefined() && base.IsLooselyEqual(value);
    }

    public override bool Equals(JsValue? obj)
    {
        return Equals(obj as JsBoolean);
    }

    public bool Equals(JsBoolean? other)
    {
        if (ReferenceEquals(this, other))
        {
            return true;
        }

        if (other is null)
        {
            return false;
        }

        return _value == other._value;
    }

    public override int GetHashCode()
    {
        return _value.GetHashCode();
    }
}
