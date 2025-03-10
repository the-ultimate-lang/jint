using System.Collections;
using Esprima;
using Ultimate.Language.Jint.Native;
using Ultimate.Language.Jint.Runtime.CallStack;

namespace Ultimate.Language.Jint.Runtime.Debugger
{
    public sealed class DebugCallStack : IReadOnlyList<CallFrame>
    {
        private readonly List<CallFrame> _stack;

        internal DebugCallStack(Engine engine, Location location, JintCallStack callStack, JsValue? returnValue)
        {
            _stack = new List<CallFrame>(callStack.Count + 1);
            var executionContext = new CallStackExecutionContext(engine.ExecutionContext);
            foreach (var element in callStack.Stack)
            {
                _stack.Add(new CallFrame(element, executionContext, location, returnValue));
                location = element.Location;
                returnValue = null;
                executionContext = element.CallingExecutionContext;
            }
            // Add root location
            _stack.Add(new CallFrame(null, executionContext, location, returnValue: null));
        }

        public CallFrame this[int index] => _stack[index];

        public int Count => _stack.Count;

        public IEnumerator<CallFrame> GetEnumerator()
        {
            return _stack.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }
}
