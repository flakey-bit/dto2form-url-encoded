using System.Reflection;

namespace flakeybit.dto2formurlencoded
{
    public interface IPropertyNamer
    {
        /// <summary>
        ///     Generates the local (i.e. in the context of the current object / not fully qualified) name for the property
        /// </summary>
        /// <param name="propInfo">The public property to be included in the form-url-encoded data</param>
        /// <remarks>Implementations should *not* url-encode the name</remarks>
        string GetLocalNameForProperty(PropertyInfo propInfo);
    }
}