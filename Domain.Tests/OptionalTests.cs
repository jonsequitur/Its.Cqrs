// Copyright (c) Microsoft. All rights reserved. 
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using FluentAssertions;
using Microsoft.Its.Domain.Serialization;
using NUnit.Framework;
using Test.Domain.Ordering;

namespace Microsoft.Its.Domain.Tests
{
    [TestFixture]
    public class OptionalTests
    {
        [Test]
        public void When_Value_is_set_to_null_then_IsSet_returns_true()
        {
            var opt = new Optional<string>();

            opt.Value = null;

            opt.IsSet.Should().BeTrue();
        }

        [Test]
        public void When_Value_is_set_to_some_value_then_IsSet_returns_true()
        {
            var opt = new Optional<string>();

            opt.Value = "some value";

            opt.IsSet.Should().BeTrue();
        }

        [Test]
        public void Unset_cannot_be_set()
        {
            var optional = Optional<string>.Unset;

            optional.Value = "hello";

            Optional<string>.Unset.IsSet.Should().BeFalse();
        }

        [Test]
        public void A_new_optional_constructed_using_default_ctor_has_IsSet_as_false()
        {
            var opt = new Optional<string>();

            opt.IsSet.Should().BeFalse();
        }
        
        [Test]
        public void When_IsSet_is_false_then_Value_throws()
        {
            var opt = new Optional<string>();

            Action getValue = () =>
            {
                var v = opt.Value;
            };

            getValue.ShouldThrow<InvalidOperationException>();
        }

        [Test]
        public void Unset_Optional_properties_are_not_included_in_json_during_serialization()
        {
            var info = new ChangeCustomerInfo
            {
                CustomerName = "joe",
                PostalCode = "12345",
                PhoneNumber = Optional<string>.Unset, // both of these properties are optional and excluded
                // not setting Address at all         // both of these properties are optional and excluded
            };

            info.Address.IsSet.Should().BeFalse();
            info.PhoneNumber.IsSet.Should().BeFalse();

            var json = info.ToJson();

            json.Should().NotContain("PhoneNumber");
            json.Should().NotContain("Address");
        }

        [Test]
        public void Set_Optional_parameters_are_included_in_json_during_serialization()
        {
            var json = new ChangeCustomerInfo
            {
                CustomerName = "joe",
                Address = "100 Main St."
            }.ToJson();

            var command = json.FromJsonTo<ChangeCustomerInfo>();

            command.CustomerName.Should().Be("joe");
            command.Address.Value.Should().Be("100 Main St.");
            command.Address.IsSet.Should().BeTrue();
            command.PostalCode.IsSet.Should().BeFalse();
            command.RegionOrCountry.IsSet.Should().BeFalse();
        }

        [Test]
        public void Unspecified_optional_parameters_are_deserialized_with_IsSet_equal_to_false()
        {
            var json = new { }.ToJson();

            var obj = json.FromJsonTo<HasOptionalProperty<Uri>>();

            obj.OptionalProperty.IsSet.Should().BeFalse();
        }

        [Test]
        public void Specified_optional_parameters_are_deserialized_with_IsSet_equal_to_true()
        {
            var json = new
            {
                OptionalProperty = "http://blammo.com/"
            }.ToJson();

            var obj = json.FromJsonTo<HasOptionalProperty<Uri>>();
     
            obj.OptionalProperty.IsSet.Should().BeTrue();
            obj.OptionalProperty.ToString().Should().Be("http://blammo.com/");
        }

        [Test]
        public void Optional_parameters_that_cannot_be_deserialized_due_to_type_issues_throw()
        {
            var json = new
            {
                OptionalProperty = "whoa that's not like any URL I ever saw..."
            }.ToJson();

            json.Invoking(j => j.FromJsonTo<HasOptionalProperty<Uri>>())
                .ShouldThrow<UriFormatException>();
        }

        [Test]
        public void Two_optional_values_are_unequal_if_one_is_set_and_the_other_is_not()
        {
            var none = new Optional<int>();
            var one = new Optional<int>(1);

            (one.Equals(none)).Should().BeFalse();
            (none.Equals(one)).Should().BeFalse();
        }

        [Test]
        public void Optional_properties_set_to_null_are_deserialized_with_IsSet_equal_to_true_and_the_value_to_null()
        {
            var obj = @"{""OptionalProperty"":null}".FromJsonTo<HasOptionalProperty<Uri>>();

            obj.OptionalProperty.IsSet.Should().BeTrue();
            obj.OptionalProperty.Value.Should().BeNull();
        }

        [Test]
        public void Optional_parameters_set_to_null_are_serialized_with_null()
        {
            var hasOptionalProperty = new HasOptionalProperty<string>
            {
                OptionalProperty = new Optional<string>(null)
            };

            hasOptionalProperty.ToJson()
                               .Should().Be(@"{""OptionalProperty"":null}");
        }

        [Test]
        public void ToString_is_informative()
        {
            var setToNull = new Optional<string>(null).ToString();
            Console.WriteLine(setToNull);
            setToNull.Should().Contain("set to null");

            var setToValue = new Optional<string>("hello").ToString();
            Console.WriteLine(setToValue);
            setToValue.Should().Contain("hello");

            var notSet = new Optional<string>().ToString();
            Console.WriteLine(notSet);
            notSet.Should().Contain("not set");
        }

        public class HasOptionalProperty<T>
        {
            public Optional<T> OptionalProperty { get; set; }
        }
    }
}
