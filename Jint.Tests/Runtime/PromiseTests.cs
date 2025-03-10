using Jint.Native;
using Jint.Native.Promise;
using Jint.Runtime;

// obsolete GetCompletionValue
#pragma warning disable 618

namespace Jint.Tests.Runtime
{
    public class PromiseTests
    {
        #region Manual Promise

        [Fact]
        public void RegisterPromise_CalledWithinExecute_ResolvesCorrectly()
        {
            Action<JsValue> resolveFunc = null;

            var engine = new Engine();
            engine.SetValue("f", new Func<JsValue>(() =>
            {
                var (promise, resolve, _) = engine.RegisterPromise();
                resolveFunc = resolve;
                return promise;
            }));

            var promise = engine.Evaluate("f();");

            resolveFunc(66);
            Assert.Equal(66, promise.UnwrapIfPromise());
        }

        [Fact]
        public void RegisterPromise_CalledWithinExecute_RejectsCorrectly()
        {
            Action<JsValue> rejectFunc = null;

            var engine = new Engine();
            engine.SetValue("f", new Func<JsValue>(() =>
            {
                var (promise, _, reject) = engine.RegisterPromise();
                rejectFunc = reject;
                return promise;
            }));

            engine.Execute("f();");

            var completion = engine.Evaluate("f();");

            rejectFunc("oops!");

            var ex = Assert.Throws<PromiseRejectedException>(() => { completion.UnwrapIfPromise(); });

            Assert.Equal("oops!", ex.RejectedValue.AsString());
        }

        [Fact]
        public void RegisterPromise_UsedWithRace_WorksFlawlessly()
        {
            var engine = new Engine();

            Action<JsValue> resolve1 = null;
            engine.SetValue("f1", new Func<JsValue>(() =>
            {
                var (promise, resolve, _) = engine.RegisterPromise();
                resolve1 = resolve;
                return promise;
            }));

            Action<JsValue> resolve2 = null;
            engine.SetValue("f2", new Func<JsValue>(() =>
            {
                var (promise, resolve, _) = engine.RegisterPromise();
                resolve2 = resolve;
                return promise;
            }));

            var completion = engine.Evaluate("Promise.race([f1(), f2()]);");

            resolve1("first");

            // still not finished but the promise is fulfilled
            Assert.Equal("first", completion.UnwrapIfPromise());

            resolve2("second");

            // completion value hasn't changed
            Assert.Equal("first", completion.UnwrapIfPromise());
        }

        #endregion

        #region Execute

        [Fact]
        public void Execute_ConcurrentNormalExecuteCall_WorksFine()
        {
            var engine = new Engine();
            engine.SetValue("f", new Func<JsValue>(() => engine.RegisterPromise().Promise));

            engine.Execute("f();");

            Assert.Equal(true, engine.Evaluate(" 1 + 1 === 2"));
        }

        #endregion

        #region Ctor

        [Fact(Timeout = 5000)]
        public void PromiseCtorWithNoResolver_Throws()
        {
            var engine = new Engine();

            Assert.Throws<JavaScriptException>(() => { engine.Execute("new Promise();"); });
        }

        [Fact(Timeout = 5000)]
        public void PromiseCtorWithInvalidResolver_Throws()
        {
            var engine = new Engine();

            Assert.Throws<JavaScriptException>(() => { engine.Execute("new Promise({});"); });
        }

        [Fact(Timeout = 5000)]
        public void PromiseCtorWithValidResolver_DoesNotThrow()
        {
            var engine = new Engine();

            engine.Execute("new Promise((resolve, reject)=>{});");
        }

        [Fact(Timeout = 5000)]
        public void PromiseCtor_ReturnsPromiseJsValue()
        {
            var engine = new Engine();
            var promise = engine.Evaluate("new Promise((resolve, reject)=>{});");

            Assert.IsType<PromiseInstance>(promise);
        }

        #endregion

        #region Resolve

        [Fact(Timeout = 5000)]
        public void PromiseResolveViaResolver_ReturnsCorrectValue()
        {
            var engine = new Engine();
            var res = engine.Evaluate("new Promise((resolve, reject)=>{resolve(66);});").UnwrapIfPromise();
            Assert.Equal(66, res);
        }

        [Fact(Timeout = 5000)]
        public void PromiseResolveViaStatic_ReturnsCorrectValue()
        {
            var engine = new Engine();
            Assert.Equal(66, engine.Evaluate("Promise.resolve(66);").UnwrapIfPromise());
        }

        #endregion

        #region Reject

        [Fact(Timeout = 5000)]
        public void PromiseRejectViaResolver_ThrowsPromiseRejectedException()
        {
            var engine = new Engine();

            var ex = Assert.Throws<PromiseRejectedException>(() =>
            {
                engine.Evaluate("new Promise((resolve, reject)=>{reject('Could not connect');});").UnwrapIfPromise();
            });

            Assert.Equal("Could not connect", ex.RejectedValue.AsString());
        }

        [Fact(Timeout = 5000)]
        public void PromiseRejectViaStatic_ThrowsPromiseRejectedException()
        {
            var engine = new Engine();

            var ex = Assert.Throws<PromiseRejectedException>(() =>
            {
                engine.Evaluate("Promise.reject('Could not connect');").UnwrapIfPromise();
            });

            Assert.Equal("Could not connect", ex.RejectedValue.AsString());
        }

        #endregion

        #region Then

        [Fact(Timeout = 5000)]
        public void PromiseChainedThen_HandlerCalledWithCorrectValue()
        {
            var engine = new Engine();

            var res = engine.Evaluate(
                "new Promise((resolve, reject) => { new Promise((innerResolve, innerReject) => {innerResolve(66)}).then(() => 44).then(result => resolve(result)); });").UnwrapIfPromise();

            Assert.Equal(44, res);
        }

        [Fact(Timeout = 5000)]
        public void PromiseThen_ReturnsNewPromiseInstance()
        {
            var engine = new Engine();
            var res = engine.Evaluate(
                "var promise1 = new Promise((resolve, reject) => { resolve(1); }); var promise2 = promise1.then();  promise1 === promise2").UnwrapIfPromise();

            Assert.Equal(false, res);
        }

        [Fact(Timeout = 5000)]
        public void PromiseThen_CalledCorrectlyOnResolve()
        {
            var engine = new Engine();
            var res = engine.Evaluate(
                "new Promise((resolve, reject) => { new Promise((innerResolve, innerReject) => {innerResolve(66)}).then(result => resolve(result)); });").UnwrapIfPromise();

            Assert.Equal(66, res);
        }

        [Fact(Timeout = 5000)]
        public void PromiseResolveChainedWithHandler_ResolvedAsUndefined()
        {
            var engine = new Engine();

            Assert.Equal(JsValue.Undefined, engine.Evaluate("Promise.resolve(33).then(() => {});").UnwrapIfPromise());
        }

        [Fact(Timeout = 5000)]
        public void PromiseChainedThenWithUndefinedCallback_PassesThroughValueCorrectly()
        {
            var engine = new Engine();
            var res = engine.Evaluate(
                "new Promise((resolve, reject) => { new Promise((innerResolve, innerReject) => {innerResolve(66)}).then().then(result => resolve(result)); });").UnwrapIfPromise();

            Assert.Equal(66, res);
        }

        [Fact(Timeout = 5000)]
        public void PromiseChainedThenWithCallbackReturningUndefined_PassesThroughUndefinedCorrectly()
        {
            var engine = new Engine();
            var res = engine.Evaluate(
                "new Promise((resolve, reject) => { new Promise((innerResolve, innerReject) => {innerResolve(66)}).then(() => {}).then(result => resolve(result)); });").UnwrapIfPromise();

            Assert.Equal(JsValue.Undefined, res);
        }

        [Fact(Timeout = 5000)]
        public void PromiseChainedThenThrowsError_ChainedCallsCatchWithThrownError()
        {
            var engine = new Engine();
            var res = engine.Evaluate(
                "new Promise((resolve, reject) => { new Promise((innerResolve, innerReject) => {innerResolve(66)}).then(() => { throw 'Thrown Error'; }).catch(result => resolve(result)); });").UnwrapIfPromise();

            Assert.Equal("Thrown Error", res);
        }

        [Fact(Timeout = 5000)]
        public void PromiseChainedThenReturnsResolvedPromise_ChainedCallsThenWithPromiseValue()
        {
            var engine = new Engine();
            var res = engine.Evaluate(
                "new Promise((resolve, reject) => { new Promise((innerResolve, innerReject) => {innerResolve(66)}).then(() => Promise.resolve(55)).then(result => resolve(result)); });").UnwrapIfPromise();

            Assert.Equal(55, res);
        }

        [Fact(Timeout = 5000)]
        public void PromiseChainedThenReturnsRejectedPromise_ChainedCallsCatchWithPromiseValue()
        {
            var engine = new Engine();
            var res = engine.Evaluate(
                "new Promise((resolve, reject) => { new Promise((innerResolve, innerReject) => {innerResolve(66)}).then(() => Promise.reject('Error Message')).catch(result => resolve(result)); });").UnwrapIfPromise();

            Assert.Equal("Error Message", res);
        }

        #endregion

        #region Catch

        [Fact(Timeout = 5000)]
        public void PromiseCatch_CalledCorrectlyOnReject()
        {
            var engine = new Engine();
            var res = engine.Evaluate(
                "new Promise((resolve, reject) => { new Promise((innerResolve, innerReject) => {innerReject('Could not connect')}).catch(result => resolve(result)); });").UnwrapIfPromise();

            Assert.Equal("Could not connect", res);
        }

        [Fact(Timeout = 5000)]
        public void PromiseThenWithCatch_CalledCorrectlyOnReject()
        {
            var engine = new Engine();
            var res = engine.Evaluate(
                "new Promise((resolve, reject) => { new Promise((innerResolve, innerReject) => {innerReject('Could not connect')}).then(undefined, result => resolve(result)); });").UnwrapIfPromise();

            Assert.Equal("Could not connect", res);
        }

        [Fact(Timeout = 5000)]
        public void PromiseChainedWithHandler_ResolvedAsUndefined()
        {
            var engine = new Engine();
            Assert.Equal(JsValue.Undefined, engine.Evaluate("Promise.reject('error').catch(() => {});").UnwrapIfPromise());
        }

        [Fact(Timeout = 5000)]
        public void PromiseChainedCatchThen_ThenCallWithUndefined()
        {
            var engine = new Engine();
            var res = engine.Evaluate(
                "new Promise((resolve, reject) => { new Promise((innerResolve, innerReject) => {innerReject('Could not connect')}).catch(ex => {}).then(result => resolve(result)); });").UnwrapIfPromise();

            Assert.Equal(JsValue.Undefined, res);
        }

        [Fact(Timeout = 5000)]
        public void PromiseChainedCatchWithUndefinedHandler_CatchChainedCorrectly()
        {
            var engine = new Engine();
            var res = engine.Evaluate(
                "new Promise((resolve, reject) => { new Promise((innerResolve, innerReject) => {innerReject('Could not connect')}).catch().catch(result => resolve(result)); });").UnwrapIfPromise();

            Assert.Equal("Could not connect", res);
        }

        #endregion

        #region Finally

        [Fact(Timeout = 5000)]
        public void PromiseChainedFinally_HandlerCalled()
        {
            var engine = new Engine();
            var res = engine.Evaluate(
                "new Promise((resolve, reject) => { new Promise((innerResolve, innerReject) => {innerResolve(66)}).finally(() => resolve(16)); });").UnwrapIfPromise();

            Assert.Equal(16, res);
        }

        [Fact(Timeout = 5000)]
        public void PromiseFinally_ReturnsNewPromiseInstance()
        {
            var engine = new Engine();
            var res = engine.Evaluate(
                "var promise1 = new Promise((resolve, reject) => { resolve(1); }); var promise2 = promise1.finally();  promise1 === promise2");

            Assert.Equal(false, res);
        }

        [Fact(Timeout = 5000)]
        public void PromiseFinally_ResolvesWithCorrectValue()
        {
            var engine = new Engine();
            Assert.Equal(2, engine.Evaluate("Promise.resolve(2).finally(() => {})").UnwrapIfPromise());
        }

        [Fact(Timeout = 5000)]
        public void PromiseFinallyWithNoCallback_ResolvesWithCorrectValue()
        {
            var engine = new Engine();
            Assert.Equal(2, engine.Evaluate("Promise.resolve(2).finally()").UnwrapIfPromise());
        }

        [Fact(Timeout = 5000)]
        public void PromiseFinallyChained_ResolvesWithCorrectValue()
        {
            var engine = new Engine();

            Assert.Equal(2, engine.Evaluate("Promise.resolve(2).finally(() => 6).finally(() => 9);").UnwrapIfPromise());
        }

        [Fact(Timeout = 5000)]
        public void PromiseFinallyWhichThrows_ResolvesWithError()
        {
            var engine = new Engine();
            var res = engine.Evaluate(
                "new Promise((resolve, reject) => { new Promise((innerResolve, innerReject) => {innerResolve(5)}).finally(() => {throw 'Could not connect';}).catch(result => resolve(result)); });").UnwrapIfPromise();

            Assert.Equal("Could not connect", res);
        }

        #endregion

        #region All

        [Fact(Timeout = 5000)]
        public void PromiseAll_BadIterable_Rejects()
        {
            var engine = new Engine();
            Assert.Throws<PromiseRejectedException>(() => { engine.Evaluate("Promise.all();").UnwrapIfPromise(); });
        }


        [Fact(Timeout = 5000)]
        public void PromiseAll_ArgsAreNotPromises_ResolvesCorrectly()
        {
            var engine = new Engine();

            Assert.Equal(new object[] {1d, 2d, 3d}, engine.Evaluate("Promise.all([1,2,3]);").UnwrapIfPromise().ToObject());
        }

        [Fact(Timeout = 5000)]
        public void PromiseAll_MixturePromisesNoPromises_ResolvesCorrectly()
        {
            var engine = new Engine();
            Assert.Equal(new object[] {1d, 2d, 3d},
                engine.Evaluate("Promise.all([1,Promise.resolve(2),3]);").UnwrapIfPromise().ToObject());
        }

        [Fact(Timeout = 5000)]
        public void PromiseAll_MixturePromisesNoPromisesOneRejects_ResolvesCorrectly()
        {
            var engine = new Engine();

            Assert.Throws<PromiseRejectedException>(() =>
            {
                engine.Evaluate("Promise.all([1,Promise.resolve(2),3, Promise.reject('Cannot connect')]);").UnwrapIfPromise();
            });
        }

        #endregion

        #region Race

        [Fact(Timeout = 5000)]
        public void PromiseRace_NoArgs_Rejects()
        {
            var engine = new Engine();

            Assert.Throws<PromiseRejectedException>(() => { engine.Evaluate("Promise.race();").UnwrapIfPromise(); });
        }

        [Fact(Timeout = 5000)]
        public void PromiseRace_InvalidIterator_Rejects()
        {
            var engine = new Engine();

            Assert.Throws<PromiseRejectedException>(() => { engine.Evaluate("Promise.race({});").UnwrapIfPromise(); });
        }

        [Fact(Timeout = 5000)]
        public void PromiseRaceNoPromises_ResolvesCorrectly()
        {
            var engine = new Engine();

            Assert.Equal(12d, engine.Evaluate("Promise.race([12,2,3]);").UnwrapIfPromise().ToObject());
        }

        [Fact(Timeout = 5000)]
        public void PromiseRaceMixturePromisesNoPromises_ResolvesCorrectly()
        {
            var engine = new Engine();

            Assert.Equal(12d, engine.Evaluate("Promise.race([12,Promise.resolve(2),3]);").UnwrapIfPromise().ToObject());
        }

        [Fact(Timeout = 5000)]
        public void PromiseRaceMixturePromisesNoPromises_ResolvesCorrectly2()
        {
            var engine = new Engine();

            Assert.Equal(2d, engine.Evaluate("Promise.race([Promise.resolve(2),6,3]);").UnwrapIfPromise().ToObject());
        }

        [Fact(Timeout = 5000)]
        public void PromiseRaceMixturePromisesNoPromises_ResolvesCorrectly3()
        {
            var engine = new Engine();
            var res = engine.Evaluate("Promise.race([new Promise((resolve,reject)=>{}),Promise.resolve(55),3]);").UnwrapIfPromise();

            Assert.Equal(55d, res.ToObject());
        }

        [Fact(Timeout = 5000)]
        public void PromiseRaceMixturePromisesNoPromises_ResolvesCorrectly4()
        {
            var engine = new Engine();

            Assert.Throws<PromiseRejectedException>(() =>
            {
                engine.Evaluate(
                    "Promise.race([new Promise((resolve,reject)=>{}),Promise.reject('Could not connect'),3]);").UnwrapIfPromise();
            });
        }

        #endregion

        #region Regression

        [Fact(Timeout = 5000)]
        public void PromiseRegression_SingleElementArrayWithClrDictionaryInPromiseAll()
        {
            var engine = new Engine();
            var dictionary = new Dictionary<string, object>
            {
                { "Value 1", 1 },
                { "Value 2", "a string" }
            };
            engine.SetValue("clrDictionary", dictionary);

            var resultAsObject = engine
                .Evaluate(@"
const promiseArray = [clrDictionary];
return Promise.all(promiseArray);") // Returning and array through Promise.any()
                .UnwrapIfPromise()
                .ToObject();

            var result = (object[]) resultAsObject;

            Assert.Single(result);
            Assert.IsType<Dictionary<string, object>>(result[0]);
        }

        #endregion
    }
}
