// Copyright (c) Microsoft. All rights reserved. 
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using FluentAssertions;
using Newtonsoft.Json;
using NUnit.Framework;

namespace Microsoft.Its.Domain.Tests
{
    [TestFixture]
    public class StringTTests
    {
        [Test]
        public void StringTs_differing_by_case_are_equal_via_Equals_instance_method()
        {
            var US = new CountryCode("US");
            var us = new CountryCode("us");

            Assert.That(US.Equals(us));
        }

        [Test]
        public void StringTs_differing_by_value_are_not_equal_via_Equals_instance_method()
        {
            var US = new CountryCode("US");
            var FR = new CountryCode("FR");

            Assert.That(!US.Equals(FR));
        }

        [Test]
        public void StringTs_differing_by_case_are_equal_via_Object_Equals_static_method()
        {
            var US = new CountryCode("US");
            var us = new CountryCode("us");

            Assert.That(Equals(us, US));
            Assert.That(Equals(US, us));
        }

        [Test]
        public void StringTs_differing_by_value_are_not_equal_via_Object_Equals_static_method()
        {
            var US = new CountryCode("US");
            var FR = new CountryCode("FR");

            Assert.That(!Equals(FR, US));
            Assert.That(!Equals(US, FR));
        }

        [Test]
        public void StringTs_and_equivalent_strings_are_equal_via_Object_Equals_static_method_when_StringT_is_first_arg()
        {
            var US = new CountryCode("US");

            Assert.That(Equals(US, "US"));
            Assert.That(Equals(US, "us"));
        }

        [Test]
        public void StringTs_differing_by_case_are_equal_via_equality_operator()
        {
            var US = new CountryCode("US");
            var us = new CountryCode("us");

            Assert.That(US == us);
            Assert.That(us == US);
        }

        [Test]
        public void StringTs_differing_by_value_are_not_equal_via_inequality_operator()
        {
            var US = new CountryCode("US");
            var FR = new CountryCode("FR");

            Assert.That(US != FR);
            Assert.That(FR != US);
        }

        [Test]
        public void StringT_and_equivalent_string_are_equal_via_Equals_instance_method()
        {
            var US = new CountryCode("US");

            Assert.That(US.Equals("US"));
            Assert.That(US.Equals("us"));
        }

        [Test]
        public void String_Equals_instance_method_will_always_return_false_when_comparing_to_StringT_instances()
        {
            var US = new CountryCode("US");

            // implicit cast to string is not allowed so this fails:
            Assert.That(!"US".Equals(US));

            // casting to string loses the StringComparer, so this fails even if explicitly cast:
            Assert.That(!"us".Equals(US));
        }

        [Test]
        public void StringT_and_nonequivalent_string_are_not_equal_via_Equals_instance_method()
        {
            var US = new CountryCode("US");

            Assert.That(!US.Equals("FR"));
            Assert.That(!"FR".Equals(US));
        }

        [Test]
        public void StringT_and_equivalent_string_are_equal_via_equality_operator()
        {
            var US = new CountryCode("US");

            Assert.That(US == "US");
            Assert.That(US == "us");
            Assert.That("US" == US);
            Assert.That("us" == US);
        }

        [Test]
        public void StringT_and_nonequivalent_string_are_not_equal_via_equality_operator()
        {
            var US = new CountryCode("US");

            Assert.That(!(US == "FR"));
            Assert.That(!("FR" == US));
        }

        [Test]
        public void Assignment_from_string_to_StringT()
        {
            String<CountryCode> us = "US";

            Assert.AreEqual(new CountryCode("US"), us);
        }

        [Test]
        public void Cast_from_StringT_to_string()
        {
            var us = (string) new CountryCode("us");

            Assert.AreEqual("us", us);
        }

        [Test]
        public void GetHashCode_returns_same_value_for_equivalent_StringTs()
        {
            var us1 = new CountryCode("us");
            var us2 = new CountryCode("us");

            Assert.That(us1.GetHashCode(), Is.EqualTo(us2.GetHashCode()));
        }

        [Test]
        public void GetHashCode_returns_different_value_for_StringT_and_equivalent_string()
        {
            var us1 = "us";
            var us2 = new CountryCode("us");

            Assert.That(us1.GetHashCode(), Is.Not.EqualTo(us2.GetHashCode()));
        }

        [Test]
        public void Equals_compares_to_null_correctly()
        {
            CountryCode nullCountry = null;

            Assert.That(nullCountry == null);
            Assert.That(null == nullCountry);
            Assert.That(new CountryCode("fr") != null);
            Assert.That(null != new CountryCode("fr"));
            Assert.That(!new CountryCode("fr").Equals(null));
        }

        [Test]
        public void Equivalent_strings_are_not_equivalent_StringT_and_StringU()
        {
            var state = new UsStateCode("CA");
            var country = new CountryCode("CA");

            Assert.That(state != country);
            Assert.That(country != state);
            Assert.That(!state.Equals(country));
            Assert.That(!country.Equals(state));
            Assert.That(!Equals(state, country));
            Assert.That(!Equals(country, state));
        }

        [Test]
        public void StringT_serializes_to_a_JSON_primitive()
        {
            var json = JsonConvert.SerializeObject(new { CountryCode = new CountryCode("CA") });

            json.Should().Be("{\"CountryCode\":\"CA\"}");
        }

        [Test]
        public void StringT_derived_classes_deserialize_from_JSON_primitives()
        {
            var json = "{\"CountryCode\":\"CA\"}";

            var s = JsonConvert.DeserializeObject<Address>(json);

            s.CountryCode.Value.Should().Be("CA");
        }

        [Test]
        public void StringT_derived_classes_deserialize_from_JSON_objects()
        {
            var json = "{\"CountryCode\":{\"Value\":\"CA\"}}";

            var s = JsonConvert.DeserializeObject<Address>(json);

            s.CountryCode.Should().Be("CA");
        }

        [Test]
        public void StringT_correctly_serializes_to_null()
        {
            CountryCode countryCode = null;
            var s = JsonConvert.SerializeObject(countryCode);
            s.Should().Be("null");
        }

        public class Address
        {
            public CountryCode CountryCode { get; set; }
        }

        public class CountryCode : String<CountryCode>
        {
            public CountryCode(string value) : base(value)
            {
                Validate();
            }

            private void Validate()
            {
                if (Value.Length != 2)
                {
                    throw new ArgumentException();
                }
            }
        }

        public class UsStateCode : String<UsStateCode>
        {
            public UsStateCode(string value) : base(value)
            {
                Validate();
            }

            private void Validate()
            {
                if (Value.Length != 2)
                {
                    throw new ArgumentException();
                }
            }
        }
    }
}