// Copyright (c) Microsoft. All rights reserved. 
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.IO;
using FluentAssertions;
using Microsoft.Its.Domain.Serialization;
using Microsoft.Its.Recipes;
using NUnit.Framework;
using Newtonsoft.Json;
using Test.Domain.Ordering;

namespace Microsoft.Its.Domain.Api.Tests
{
    [TestFixture]
    public class PrimitiveConverterTests
    {
        [Test]
        public void Serializes_an_object_property_into_a_primitive_json_string_property()
        {
            var settings = new JsonSerializerSettings();
            var converter = new PrimitiveConverter<FileInfo>(
                serialize: fi => fi.FullName,
                deserialize: o => new FileInfo((string) o));
            settings.Converters.Add(converter);
            var obj = new HasPropertyOf<FileInfo>
            {
                Property = new FileInfo(@"c:\temp\hello.txt")
            };

            var json = JsonConvert.SerializeObject(obj, settings);

            json.Should().Be("{\"Property\":\"c:\\\\temp\\\\hello.txt\"}");
        }

        [Test]
        public void Deserializes_a_complex_object_from_a_json_primitive_string_property()
        {
            var settings = new JsonSerializerSettings();
            var converter = new PrimitiveConverter<FileInfo>(
                serialize: fi => null,
                deserialize: o => new FileInfo((string) o));
            settings.Converters.Add(converter);

            var json = "{\"Property\":\"c:\\\\temp\\\\hello.txt\"}";

            var obj = JsonConvert.DeserializeObject<HasPropertyOf<FileInfo>>(json, settings);

            obj.Property.FullName.Should().Be(@"c:\temp\hello.txt");
        }

        [Test]
        public void Serializes_an_object_into_a_primitive_json_string_object()
        {
            var settings = new JsonSerializerSettings();
            var converter = new PrimitiveConverter<FileInfo>(
                serialize: fi => fi.FullName,
                deserialize: o => null);
            settings.Converters.Add(converter);
            var obj = new FileInfo(@"c:\temp\hello.txt");

            var json = JsonConvert.SerializeObject(obj, settings);

            json.Should().Be("\"c:\\\\temp\\\\hello.txt\"");
        }

        [Test]
        public void Deserializes_a_complex_object_from_a_primitive_json_string_object()
        {
            var settings = new JsonSerializerSettings();
            var converter = new PrimitiveConverter<FileInfo>(
                serialize: fi => null,
                deserialize: o => new FileInfo((string) o));
            settings.Converters.Add(converter);

            var json = "\"c:\\\\temp\\\\hello.txt\"";

            var obj = JsonConvert.DeserializeObject<FileInfo>(json, settings);

            obj.FullName.Should().Be(@"c:\temp\hello.txt");
        }

        [Test]
        public void Serializes_an_object_property_into_a_primitive_json_guid_property()
        {
            var settings = new JsonSerializerSettings();
            var converter = new PrimitiveConverter<HasPropertyOf<Guid>>(
                serialize: o => o.Property.ToString(),
                deserialize: o => null);
            settings.Converters.Add(converter);
            var guid = Guid.NewGuid();
            var obj = new HasPropertyOf<HasPropertyOf<Guid>>
            {
                Property = new HasPropertyOf<Guid>
                {
                    Property = guid
                }
            };

            var json = JsonConvert.SerializeObject(obj, settings);

            json.Should().Be("{\"Property\":\"" + guid + "\"}");
        }

        [Test]
        public void Deserializes_a_complex_object_from_a_json_primitive_guid_property()
        {
            var settings = new JsonSerializerSettings();
            var converter = new PrimitiveConverter<HasPropertyOf<Guid>>(
                serialize: o => null,
                deserialize: o =>
                             new HasPropertyOf<Guid>
                             {
                                 Property = Guid.Parse((string) o)
                             });
            var guid = Guid.NewGuid();
            settings.Converters.Add(converter);

            var json = "{\"Property\":\"" + guid + "\"}";

            var obj = JsonConvert.DeserializeObject<HasPropertyOf<HasPropertyOf<Guid>>>(json, settings);

            obj.Property.Property.Should().Be(guid);
        }

        [Test]
        public void Serializes_an_object_property_into_a_primitive_json_int_property()
        {
            var settings = new JsonSerializerSettings();
            var converter = new PrimitiveConverter<HasPropertyOf<int>>(
                serialize: o => o.Property,
                deserialize: o => null);
            settings.Converters.Add(converter);
            var obj = new HasPropertyOf<HasPropertyOf<int>>
            {
                Property = new HasPropertyOf<int>
                {
                    Property = 123
                }
            };

            var json = JsonConvert.SerializeObject(obj, settings);

            json.Should().Be("{\"Property\":123}");
        }

        [Test]
        public void Deserializes_a_complex_object_from_a_json_primitive_int_property()
        {
            var settings = new JsonSerializerSettings();
            var converter = new PrimitiveConverter<HasPropertyOf<Int64>>(
                serialize: o => null,
                deserialize: o =>
                             new HasPropertyOf<Int64>
                             {
                                 Property = (Int64) o
                             });
            settings.Converters.Add(converter);

            var json = "{\"Property\":123}";

            var obj = JsonConvert.DeserializeObject<HasPropertyOf<HasPropertyOf<Int64>>>(json, settings);

            obj.Property.Property.Should().Be(123);
        }

        [Test]
        public void Deserializes_a_String_T_property_when_there_is_a_value_property_in_the_json()
        {
            var settings = new JsonSerializerSettings();

            var converter = new PrimitiveConverter<EmailAddress>(
                serialize: e => e.Value,
                deserialize: j => new EmailAddress(j.ToString()));

            settings.Converters.Add(converter);

            var emailAddress = Any.Email();
            var json = "{\"NewEmailAddress\":{\"Value\":\"" + emailAddress + "\"}}";

            var obj = JsonConvert.DeserializeObject<ChangeEmailAddress>(json, settings);

            obj.NewEmailAddress.Should().Be(emailAddress);
        }

        public class HasPropertyOf<T>
        {
            public HasPropertyOf()
            {
            }

            public HasPropertyOf(T property)
            {
                Property = property;
            }

            public T Property { get; set; }
        }
    }
}
