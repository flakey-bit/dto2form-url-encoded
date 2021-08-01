# dto2form-url-encoded
A simple library to assist with converting DTOs (data-transfer-objects) into ```FormUrlEncodedContent``` (application/x-www-form-urlencoded), suitable for ```HttpClient```. It supports arbitrary levels of nesting & allows control over how properties are named and serialized. 

Although [anyone sane wouldn't choose application/x-www-form-urlencoded for an API](https://brandur.org/fragments/application-x-wwww-form-urlencoded), sometimes it's unavoidable (it seems to be popular for APIs written in PHP)

## Basic Usage:
Define your DTO(s) with public properties:

```C#
public class PersonDto
{
    public string FirstName { get; set; }

    public Contact ContactDetails { get; set; }
    
    public class Contact
    {
        public string PhoneNumber {get; set;}
        public string Email { get; set; }
    }
}
```

Optionally, decorate properties with ```DtoFormUrlEncoderConverterAttribute``` to control the serialization 

```C#
    public class Contact
    {
        [DtoFormUrlEncoderConverter(typeof(PhoneNumberConverter))]
        public string PhoneNumber {get; set;}
        public string Email { get; set; }
    }
    
    public class PhoneNumberConverter : IPropertyValueConverter
    {
        public string Convert(object value) {
            var phoneNumber = value as string;
            return phoneNumber?.Replace(" ", "");
        }    
    }

```

Create your DTOs:

```C#
var myPerson = new PersonDto {
FirstName = "Joe Bloggs",
ContactDetails = new PersonDto.Contact {
    Email = "Joe.Bloggs@example.com",
    PhoneNumber = "0800 83 83 83"
}};
```

Instantiate the converter & convert objects

```C#
var converter = new DtoFormUrlEncoder();
FormUrlEncodedContent content = converter.ToFormUrlEncodedContent(myPerson);
```

Consume with ```HttpClient``` (or as needed)
```C#
var client = new HttpClient();
client.PostAsync("http://www.example.com", content);
```

