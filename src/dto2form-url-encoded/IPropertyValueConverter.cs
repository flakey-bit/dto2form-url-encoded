namespace flakeybit.dto2formurlencoded
{
    public interface IPropertyValueConverter
    {
        /// <summary>
        ///     Converts a property value into a string representation (prior to url-encoding)
        /// </summary>
        /// <param name="value">The value of the property to be represented in the form data</param>
        /// <returns>A string representation of the property value</returns>
        /// <remarks>Implementations should *not* url-encode the string representation</remarks>
        string Convert(object value);
    }
}