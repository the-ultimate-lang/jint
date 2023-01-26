using Ultimate.Language.Jint.Native;
using Ultimate.Language.Jint.Runtime.Descriptors;

namespace Ultimate.Language.Jint.Collections
{
    internal sealed class SymbolDictionary : DictionarySlim<JsSymbol, PropertyDescriptor>
    {
        public SymbolDictionary()
        {
        }

        public SymbolDictionary(int capacity) : base(capacity)
        {
        }
    }
}
