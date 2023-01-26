using Esprima.Ast;

namespace Ultimate.Language.Jint.Runtime
{
    /// <summary>
    /// Workaround for situation where engine is not easily accessible.
    /// </summary>
    internal sealed class TypeErrorException : JintException
    {
        public TypeErrorException(string? message, Node? node) : base(message)
        {
            Node = node;
        }

        public Node? Node { get; }
    }
}
