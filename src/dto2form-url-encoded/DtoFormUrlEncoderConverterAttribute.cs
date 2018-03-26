using System;

namespace flakeybit.dto2formurlencoded
{
    /// <summary>
    ///     Attribute used to specify how <see cref="DtoFormUrlEncoder" /> should convert the raw value to a string (prior to
    ///     url-encoding it)
    /// </summary>
    [AttributeUsage(AttributeTargets.Property)]
    public class DtoFormUrlEncoderConverterAttribute : Attribute
    {
        /// <param name="converterType">
        ///     A concrete implementation of <see cref="IPropertyValueConverter" /> with a parameterless
        ///     constructor
        /// </param>
        public DtoFormUrlEncoderConverterAttribute(Type converterType) {
            if (converterType == null) {
                throw new ArgumentNullException(nameof(converterType));
            }

            if (!typeof(IPropertyValueConverter).IsAssignableFrom(converterType)) {
                throw new ArgumentException($"Converter type must implement {typeof(IPropertyValueConverter).Name}", nameof(converterType));
            }

            if (converterType.GetConstructor(new Type[0]) == null) {
                throw new ArgumentException("Converter type must have a parameterless constructor", nameof(converterType));
            }

            ConverterType = converterType;
        }

        public Type ConverterType { get; }
    }
}