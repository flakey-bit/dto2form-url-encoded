using System.Reflection;

namespace flakeybit.dto2formurlencoded
{
    /// <summary>
    ///     Default implementation of IPropertyNamer (uses the property name)
    /// </summary>
    public class DefaultPropertyNamer : IPropertyNamer
    {
        public virtual string GetLocalNameForProperty(PropertyInfo prop) {
            return prop.Name;
        }
    }
}