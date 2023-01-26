using Ultimate.Language.Jint.Runtime.Descriptors;

namespace Ultimate.Language.Jint.Collections
{
    internal sealed class PropertyDictionary : HybridDictionary<PropertyDescriptor>
    {
        public PropertyDictionary()
        {
        }

        public PropertyDictionary(int capacity, bool checkExistingKeys) : base(capacity, checkExistingKeys)
        {
        }
    }
}
