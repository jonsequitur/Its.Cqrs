using System;
using FluentAssertions;
using Microsoft.Its.Domain.Mapping;
using Microsoft.Its.Recipes;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NUnit.Framework;
using Sample.Domain.Ordering;
using Sample.Domain.Ordering.Commands;

namespace Microsoft.Its.Cqrs.Recipes.Tests
{
    [TestClass, TestFixture]
    public class MappingTests
    {
        [Test, TestMethod]
        public void New_maps_custom_properties_from_command_to_event()
        {
            var change = new ChangeCustomerInfo
            {
                Address = Any.Paragraph(),
                CustomerName = Any.Paragraph(2),
                PhoneNumber = Any.String(10, characterSet: Characters.Digits),
                PostalCode = Any.String(5, characterSet: Characters.Digits),
                RegionOrCountry = "U.S."
            };

            var changed = New<Order.CustomerInfoChanged>.From(change);

            changed.Address.Should().Be(change.Address);
            changed.CustomerName.Should().Be(change.CustomerName);
            changed.PhoneNumber.Should().Be(change.PhoneNumber);
            changed.PostalCode.Should().Be(change.PostalCode);
            changed.RegionOrCountry.Should().Be(change.RegionOrCountry);
        }

        [Test, TestMethod]
        public void When_command_is_null_then_mapper_throws_ArgumentNullException()
        {
            ChangeCustomerInfo change = null;

            Action callMapperWithNull = () => New<Order.CustomerInfoChanged>.From(change);

            callMapperWithNull.ShouldThrow<ArgumentNullException>();
        }
    }
}