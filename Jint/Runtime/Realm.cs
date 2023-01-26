using Esprima.Ast;
using Ultimate.Language.Jint.Native;
using Ultimate.Language.Jint.Native.Object;
using Ultimate.Language.Jint.Runtime.Environments;

namespace Ultimate.Language.Jint.Runtime
{
    public sealed class Realm
    {
        internal readonly Dictionary<Node, JsArray> _templateMap = new();


        // helps when debugging which nested realm we are in...
#if DEBUG
        private static int globalId = 1;

        public Realm()
        {
            Id = globalId++;
        }

        internal int Id;
#endif

        /// <summary>
        /// The intrinsic values used by code associated with this realm.
        /// </summary>
        public Intrinsics Intrinsics { get; internal set; } = null!;

        /// <summary>
        /// The global object for this realm.
        /// </summary>
        public ObjectInstance GlobalObject { get; internal set; } = null!;

        /// <summary>
        /// The global environment for this realm.
        /// </summary>
        public GlobalEnvironmentRecord GlobalEnv { get; internal set; } = null!;

        /// <summary>
        /// Field reserved for use by hosts that need to associate additional information with a Realm Record.
        /// </summary>
        public object? HostDefined { get; set; }
    }
}
