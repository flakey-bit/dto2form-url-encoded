using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Reflection;
using System.Runtime.CompilerServices;

[assembly: InternalsVisibleTo("flakeybit.dto2form-url-encoded.test")]

namespace flakeybit.dto2formurlencoded
{
    public interface IDtoFormUrlEncoder
    {
        FormUrlEncodedContent ToFormUrlEncodedContent(object dto);
    }

    public sealed class DtoFormUrlEncoder : IDtoFormUrlEncoder
    {
        private static readonly HashSet<Type> BasicTypes = new HashSet<Type> {
            typeof(string),
            typeof(int),
            typeof(decimal),
            typeof(double)
        };

        private readonly IPropertyNamer _propertyNamer;

        /// <summary>
        ///     Construct an encoder with the specified property namer implementation
        /// </summary>
        /// <param name="propertyNamer">Namer implementation to be used</param>
        public DtoFormUrlEncoder(IPropertyNamer propertyNamer) {
            _propertyNamer = propertyNamer;
        }

        /// <summary>
        ///     Construct an encoder with the default property namer implementation
        /// </summary>
        public DtoFormUrlEncoder() : this(new DefaultPropertyNamer()) {
        }

        /// <summary>
        ///     Converts a DTO (data-transfer-object) into <see cref="FormUrlEncodedContent" /> suitable for
        ///     <see cref="HttpClient" /> etc
        /// </summary>
        /// <param name="obj">The object to be converted</param>
        /// <remarks>
        ///     Only public properties are mapped. Use <see cref="DtoFormUrlEncoderConverterAttribute" /> on properties to
        ///     control the conversion of raw values
        /// </remarks>
        public FormUrlEncodedContent ToFormUrlEncodedContent(object obj) {
            var unmappedValues = ExplodeObjectForFormMapping(obj);
            return new FormUrlEncodedContent(ConvertValues(unmappedValues));
        }

        internal IEnumerable<(string Path, object Value, Type Converter)> ExplodeObjectForFormMapping(object obj) {
            HashSet<object> seen = new HashSet<object>();
            seen.Add(obj);
            foreach (var tuple in ExplodeChildObjectForMapping(null, obj, _propertyNamer, seen)) {
                string leafChildName;
                if (tuple.Path.Length > 1) {
                    leafChildName = tuple.Path[0] + string.Join("", tuple.Path.Skip(1).Select(part => $"[{part}]"));
                } else {
                    leafChildName = tuple.Path[0];
                }

                yield return (leafChildName, tuple.Value, tuple.Converter);
            }
        }

        private static IEnumerable<(string[] Path, object Value, Type Converter)> ExplodeChildObjectForMapping(
            string[] path,
            object obj,
            IPropertyNamer propertyNamer,
            HashSet<object> seen) {
            if (obj == null) {
                throw new ArgumentNullException();
            }

            var objType = obj.GetType();

            if (objType == typeof(string)) {
                throw new ArgumentException("Argument cannot be a string", nameof(obj));
            }

            if (!objType.GetTypeInfo().IsClass) {
                throw new ArgumentException("Argument must be a class instance", nameof(obj));
            }

            foreach (var prop in objType.GetProperties(BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static)) {
                var val = prop.GetValue(obj, null);
                var name = propertyNamer.GetLocalNameForProperty(prop);
                var converter = GetConverter(prop);

                string[] propPath;
                if (path == null) {
                    propPath = new[] {
                        name
                    };
                } else {
                    propPath = new string[path.Length + 1];
                    Array.Copy(path, propPath, path.Length);
                    propPath[propPath.Length - 1] = name;
                }

                if (val == null) {
                    yield return (propPath, null, converter);
                } else if (val.GetType().GetTypeInfo().IsClass && val.GetType() != typeof(string)) {
                    if (seen.Contains(val)) {
                        throw new ArgumentException("Object contains cycles and cannot be encoded");
                    }

                    seen.Add(val);

                    foreach (var tuple in ExplodeChildObjectForMapping(propPath, val, propertyNamer, seen)) {
                        yield return (tuple.Path, tuple.Value, tuple.Converter);
                    }
                } else {
                    yield return (propPath, val, converter);
                }
            }
        }

        private static Type GetConverter(PropertyInfo prop) {
            var converterAttr = prop.GetCustomAttribute<DtoFormUrlEncoderConverterAttribute>();
            if (converterAttr != null) {
                return converterAttr.ConverterType;
            }

            return null;
        }

        private static IEnumerable<KeyValuePair<string, string>> ConvertValues(IEnumerable<(string Path, object Value, Type Converter)> unmappedValues) {
            foreach (var tuple in unmappedValues) {
                var convertedValue = ConvertValue(tuple.Path, tuple.Value, tuple.Converter);
                if (convertedValue == null) {
                    continue;
                }

                yield return new KeyValuePair<string, string>(tuple.Path, convertedValue);
            }
        }

        private static string ConvertValue(string formFieldName, object value, Type converterType) {
            if (converterType != null) {
                var converter = CreateConverter(converterType);
                return converter.Convert(value);
            }

            if (value == null) {
                return null;
            }

            var valueType = value.GetType();
            if (BasicTypes.Contains(valueType)) {
                return value.ToString();
            }

            throw new ArgumentException($"No support for mapping value of type {valueType.Name} - form field name: {formFieldName}, value: {value}",
                                        nameof(value));
        }

        private static IPropertyValueConverter CreateConverter(Type converterType) {
            var ctr = converterType.GetConstructor(new Type[0]);
            return (IPropertyValueConverter) ctr.Invoke(new object[0]);
        }
    }
}