using Esprima.Ast;
using Ultimate.Language.Jint.Native;
using Ultimate.Language.Jint.Native.Promise;

namespace Ultimate.Language.Jint.Runtime.Interpreter.Expressions;

internal sealed class JintImportExpression : JintExpression
{
    private JintExpression _importExpression;

    public JintImportExpression(Import expression) : base(expression)
    {
        _initialized = false;
        _importExpression = null!;
    }

    protected override void Initialize(EvaluationContext context)
    {
        var expression = ((Import) _expression).Source;
        _importExpression = Build(expression);
    }

    /// <summary>
    /// https://tc39.es/ecma262/#sec-import-calls
    /// </summary>
    protected override object EvaluateInternal(EvaluationContext context)
    {
        var referencingScriptOrModule = context.Engine.GetActiveScriptOrModule();
        var argRef = _importExpression.Evaluate(context);
        var specifier = context.Engine.GetValue(argRef); //.UnwrapIfPromise();
        var promiseCapability = PromiseConstructor.NewPromiseCapability(context.Engine, context.Engine.Realm.Intrinsics.Promise);

        string specifierString;
        try
        {
            specifierString = TypeConverter.ToString(specifier);
        }
        catch (JavaScriptException e)
        {
            promiseCapability.Reject.Call(JsValue.Undefined, new[] { e.Error });
            return JsValue.Undefined;
        }

        context.Engine._host.ImportModuleDynamically(referencingScriptOrModule, specifierString, promiseCapability);
        context.Engine.RunAvailableContinuations();
        return promiseCapability.PromiseInstance;
    }
}
