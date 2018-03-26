using System.Reflection;
using Microsoft.AspNetCore.Mvc;

namespace flakeybit.dto2formurlencoded.test
{
    /// <summary>
    ///     An implementation of IPropertyNamer which uses the ASP.NET <see cref="ModelBinderAttribute" /> (if present on the
    ///     property)
    ///     to determine the mapped property name, falling back to the property name itself
    /// </summary>
    public class ModelBinderPropertyNamer : DefaultPropertyNamer
    {
        public override string GetLocalNameForProperty(PropertyInfo prop) {
            var modelBinderAttr = prop.GetCustomAttribute<ModelBinderAttribute>();
            if (!string.IsNullOrEmpty(modelBinderAttr?.Name)) {
                return modelBinderAttr.Name;
            }

            return base.GetLocalNameForProperty(prop);
        }
    }
}