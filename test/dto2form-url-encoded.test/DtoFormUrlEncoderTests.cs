using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Reflection;
using System.Text;
using Microsoft.AspNetCore.Mvc;
using Moq;
using NUnit.Framework;

// ReSharper disable UnusedAutoPropertyAccessor.Local

namespace flakeybit.dto2formurlencoded.test
{
    [TestFixture]
    public class DtoFormUrlEncoderTests
    {
        [SetUp]
        public void SetUp() {
            _converter = new DtoFormUrlEncoder(new ModelBinderPropertyNamer());
        }

        private DtoFormUrlEncoder _converter;

        private class TestDatePropertyValueConverter : IPropertyValueConverter
        {
            public string Convert(object value) {
                return ((DateTime) value).ToString("O");
            }
        }

        private class TestDto1
        {
            public string SomeProperty { get; set; }

            [ModelBinder(Name = "another_property")]
            public string AnotherProperty { get; set; }

            public TestDto1Child Child { get; set; }

            private string PrivatePropertyShouldNotBeMapped => "failure";
            internal string InternalPropertyShouldNotBeMapped => "failure";

            public class TestDto1Child
            {
                [ModelBinder(Name = "the_number")]
                public int SomeNumber { get; set; }

                [ModelBinder(Name = "nested_child")]
                public TestDto1ChildNestedChild Nested { get; set; }

                public class TestDto1ChildNestedChild
                {
                    [ModelBinder(Name = "a_value")]
                    [DtoFormUrlEncoderConverter(typeof(TestDatePropertyValueConverter))]
                    public DateTime SomeDate { get; set; }
                }
            }
        }

        private static string GetEncodedContentAsString(FormUrlEncodedContent result) {
            var bytes = result.ReadAsByteArrayAsync().Result;
            var asText = Encoding.UTF8.GetString(bytes);
            return asText;
        }

        [Test]
        public void TestExplodeObjectForFormMapping() {
            var dto = new TestDto1 {
                Child = new TestDto1.TestDto1Child {
                    SomeNumber = 42,
                    Nested = new TestDto1.TestDto1Child.TestDto1ChildNestedChild {
                        SomeDate = new DateTime(1983, 11, 7)
                    }
                },
                SomeProperty = "Foo",
                AnotherProperty = "Bar 123"
            };


            var result = _converter.ExplodeObjectForFormMapping(dto).ToDictionary(tuple => tuple.Path, tuple => tuple.Value);
            var expected = new Dictionary<string, object> {
                {"SomeProperty", "Foo"},
                {"another_property", "Bar 123"},
                {"Child[the_number]", 42},
                {"Child[nested_child][a_value]", new DateTime(1983, 11, 7)}
            };
            CollectionAssert.AreEquivalent(expected, result);
        }

        [Test]
        public void TestPropertyNamesAreEncoded() {
            var mockNamer = new Mock<IPropertyNamer>(MockBehavior.Strict);
            mockNamer.Setup(namer => namer.GetLocalNameForProperty(It.IsAny<PropertyInfo>())).Returns("this property [contains & ? ] characters");
            var converter = new DtoFormUrlEncoder(mockNamer.Object);

            var result = converter.ToFormUrlEncodedContent(new TestDto1 {
                AnotherProperty = "1"
            });
            var asString = GetEncodedContentAsString(result);
            Assert.That(asString, Is.EqualTo("this+property+%5Bcontains+%26+%3F+%5D+characters=1"));
        }

        [Test]
        public void TestPropertyValuesAreEncoded() {
            var dto = new TestDto1 {
                AnotherProperty = "hello this [ is url & encoded ] ? or is it!"
            };

            var result = _converter.ToFormUrlEncodedContent(dto);
            var asString = GetEncodedContentAsString(result);
            Assert.That(asString, Is.EqualTo("another_property=hello+this+%5B+is+url+%26+encoded+%5D+%3F+or+is+it%21"));
        }

        [Test]
        public void TestToFormUrlEncodedContent() {
            var dto = new TestDto1 {
                Child = new TestDto1.TestDto1Child {
                    SomeNumber = 42,
                    Nested = new TestDto1.TestDto1Child.TestDto1ChildNestedChild {
                        SomeDate = new DateTime(1983, 11, 7)
                    }
                },
                SomeProperty = "Foo",
                AnotherProperty = "Bar 123"
            };

            var result = _converter.ToFormUrlEncodedContent(dto);
            var expected =
                "SomeProperty=Foo&another_property=Bar+123&Child%5Bthe_number%5D=42&Child%5Bnested_child%5D%5Ba_value%5D=1983-11-07T00%3A00%3A00.0000000";
            var asString = GetEncodedContentAsString(result);
            Assert.That(asString, Is.EqualTo(expected));
        }

        [Test]
        public void TestToFormUrlEncodedContentCyclicObjectThrows() {
            var encoder = new DtoFormUrlEncoder();

            var instance = new RecursiveDto {
                SomeProperty = new RecursiveDto()
            };

            instance.SomeProperty.SomeProperty = instance;

            Assert.That(() => {
                encoder.ToFormUrlEncodedContent(instance);
            }, Throws.ArgumentException.With.Message.Contains("Object contains cycles and cannot be encoded"));
        }

        private class RecursiveDto
        {
            public RecursiveDto SomeProperty { get; set; }
        }
    }
}