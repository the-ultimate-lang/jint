using Esprima;
using Esprima.Ast;
using Ultimate.Language.Jint.Runtime;
using Ultimate.Language.Jint.Runtime.Descriptors;
using Ultimate.Language.Jint.Runtime.Environments;
using Ultimate.Language.Jint.Runtime.Interpreter.Statements;

namespace Ultimate.Language.Jint.Native.Function
{
    internal sealed class EvalFunctionInstance : FunctionInstance
    {
        private static readonly JsString _functionName = new("eval");

        private readonly JavaScriptParser _parser = new(new ParserOptions { Tolerant = false });

        public EvalFunctionInstance(
            Engine engine,
            Realm realm,
            FunctionPrototype functionPrototype)
            : base(
                engine,
                realm,
                _functionName,
                StrictModeScope.IsStrictModeCode ? FunctionThisMode.Strict : FunctionThisMode.Global)
        {
            _prototype = functionPrototype;
            _length = new PropertyDescriptor(JsNumber.PositiveOne, PropertyFlag.Configurable);
        }

        protected internal override JsValue Call(JsValue thisObject, JsValue[] arguments)
        {
            var callerRealm = _engine.ExecutionContext.Realm;
            var x = arguments.At(0);
            return PerformEval(x, callerRealm, false, false);
        }

        /// <summary>
        /// https://tc39.es/ecma262/#sec-performeval
        /// </summary>
        public JsValue PerformEval(JsValue x, Realm callerRealm, bool strictCaller, bool direct)
        {
            if (!x.IsString())
            {
                return x;
            }

            var evalRealm = _realm;
            _engine._host.EnsureCanCompileStrings(callerRealm, evalRealm);

            var inFunction = false;
            var inMethod = false;
            var inDerivedConstructor = false;

            if (direct)
            {
                var thisEnvRec = _engine.ExecutionContext.GetThisEnvironment();
                if (thisEnvRec is FunctionEnvironmentRecord functionEnvironmentRecord)
                {
                    var F = functionEnvironmentRecord._functionObject;
                    inFunction = true;
                    inMethod = thisEnvRec.HasSuperBinding();

                    if (F._constructorKind == ConstructorKind.Derived)
                    {
                        inDerivedConstructor = true;
                    }
                }
            }

            Script? script = null;
            try
            {
                script = _parser.ParseScript(x.ToString(), strict: strictCaller);
            }
            catch (ParserException e)
            {
                if (e.Description == Messages.InvalidLHSInAssignment)
                {
                    ExceptionHelper.ThrowReferenceError(callerRealm, (string?) null);
                }
                else
                {
                    ExceptionHelper.ThrowSyntaxError(callerRealm, e.Message);
                }
            }

            var body = script.Body;
            if (body.Count == 0)
            {
                return Undefined;
            }

            if (!inFunction)
            {
                // if body Contains NewTarget, throw a SyntaxError exception.
            }
            if (!inMethod)
            {
                // if body Contains SuperProperty, throw a SyntaxError exception.
            }
            if (!inDerivedConstructor)
            {
                // if body Contains SuperCall, throw a SyntaxError exception.
            }

            var strictEval = script.Strict || _engine._isStrict;
            var ctx = _engine.ExecutionContext;

            using (new StrictModeScope(strictEval))
            {
                EnvironmentRecord lexEnv;
                EnvironmentRecord varEnv;
                PrivateEnvironmentRecord? privateEnv;
                if (direct)
                {
                    lexEnv = JintEnvironment.NewDeclarativeEnvironment(_engine, ctx.LexicalEnvironment);
                    varEnv = ctx.VariableEnvironment;
                    privateEnv = ctx.PrivateEnvironment;
                }
                else
                {
                    lexEnv = JintEnvironment.NewDeclarativeEnvironment(_engine, evalRealm.GlobalEnv);
                    varEnv = evalRealm.GlobalEnv;
                    privateEnv = null;
                }

                if (strictEval)
                {
                    varEnv = lexEnv;
                }

                // If ctx is not already suspended, suspend ctx.

                Engine.EnterExecutionContext(lexEnv, varEnv, evalRealm, privateEnv);

                try
                {
                    Engine.EvalDeclarationInstantiation(script, varEnv, lexEnv, privateEnv, strictEval);

                    var statement = new JintScript(script);
                    var result = statement.Execute(_engine._activeEvaluationContext!);
                    var value = result.GetValueOrDefault();

                    if (result.Type == CompletionType.Throw)
                    {
                        ExceptionHelper.ThrowJavaScriptException(_engine, value, result);
                        return null!;
                    }
                    else
                    {
                        return value;
                    }
                }
                finally
                {
                    Engine.LeaveExecutionContext();
                }
            }
        }
    }
}
