using Esprima;
using Ultimate.Language.Jint.Native;
using Ultimate.Language.Jint.Native.Error;
using Ultimate.Language.Jint.Native.Object;
using Ultimate.Language.Jint.Pooling;
using Ultimate.Language.Jint.Runtime.Descriptors;

namespace Ultimate.Language.Jint.Runtime;

public class JavaScriptException : JintException
{
    private static string? GetMessage(JsValue? error)
    {
        string? ret = null;
        if (error is ObjectInstance oi)
        {
            ret = oi.Get(CommonProperties.Message).ToString();
        }
        else if (error is not null)
        {
            ret = error.IsSymbol() ? error.ToString() : TypeConverter.ToString(error);
        }

        return ret;
    }

    private readonly JavaScriptErrorWrapperException _jsErrorException;

    public string? JavaScriptStackTrace => _jsErrorException.StackTrace;
    public ref readonly Location Location => ref _jsErrorException.Location;
    public JsValue Error => _jsErrorException.Error;

    internal JavaScriptException(ErrorConstructor errorConstructor)
        : base("", new JavaScriptErrorWrapperException(errorConstructor.Construct(), ""))
    {
        _jsErrorException = (JavaScriptErrorWrapperException) InnerException!;
    }

    public JavaScriptException(ErrorConstructor errorConstructor, string? message = null)
        : base(message, new JavaScriptErrorWrapperException(errorConstructor.Construct(message), message))
    {
        _jsErrorException = (JavaScriptErrorWrapperException) InnerException!;
    }

    public JavaScriptException(JsValue error)
        : base(GetMessage(error), new JavaScriptErrorWrapperException(error, GetMessage(error)))
    {
        _jsErrorException = (JavaScriptErrorWrapperException) InnerException!;
    }

    public string GetJavaScriptErrorString() => _jsErrorException.ToString();

    public JavaScriptException SetJavaScriptCallstack(Engine engine, in Location location, bool overwriteExisting = false)
    {
        _jsErrorException.SetCallstack(engine, location, overwriteExisting);
        return this;
    }

    public JavaScriptException SetJavaScriptLocation(in Location location)
    {
        _jsErrorException.SetLocation(location);
        return this;
    }

    private sealed class JavaScriptErrorWrapperException : JintException
    {
        private string? _callStack;
        private Location _location;

        internal JavaScriptErrorWrapperException(JsValue error, string? message = null)
            : base(message ?? GetMessage(error))
        {
            Error = error;
        }

        public JsValue Error { get; }

        public ref readonly Location Location => ref _location;

        internal void SetLocation(Location location)
        {
            _location = location;
        }

        internal void SetCallstack(Engine engine, Location location, bool overwriteExisting)
        {
            _location = location;

            var errObj = Error.IsObject() ? Error.AsObject() : null;
            if (errObj is null)
            {
                _callStack = engine.CallStack.BuildCallStackString(location);
                return;
            }

            // Does the Error object already have a stack property?
            if (errObj.HasProperty(CommonProperties.Stack) && !overwriteExisting)
            {
                _callStack = errObj.Get(CommonProperties.Stack).AsString();
            }
            else
            {
                _callStack = engine.CallStack.BuildCallStackString(location);
                errObj.FastSetProperty(CommonProperties.Stack._value, new PropertyDescriptor(_callStack, false, false, false));
            }
        }

        /// <summary>
        /// Returns the call stack of the JavaScript exception.
        /// </summary>
        public override string? StackTrace
        {
            get
            {
                if (_callStack is not null)
                {
                    return _callStack;
                }

                if (Error is not ObjectInstance oi)
                {
                    return null;
                }

                var callstack = oi.Get(CommonProperties.Stack, Error);

                return callstack.IsUndefined()
                    ? null
                    : callstack.AsString();
            }
        }

        public override string ToString()
        {
            using var rent = StringBuilderPool.Rent();
            var sb = rent.Builder;

            sb.Append("Error");
            var message = Message;
            if (!string.IsNullOrEmpty(message))
            {
                sb.Append(": ");
                sb.Append(message);
            }

            var stackTrace = StackTrace;
            if (stackTrace != null)
            {
                sb.Append(Environment.NewLine);
                sb.Append(stackTrace);
            }

            return rent.ToString();
        }
    }
}
