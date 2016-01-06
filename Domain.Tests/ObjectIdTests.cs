// Copyright (c) Microsoft. All rights reserved. 
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using FluentAssertions;
using Microsoft.Its.Domain.Serialization;
using Microsoft.Its.Recipes;
using Newtonsoft.Json;
using NUnit.Framework;
using Assert = NUnit.Framework.Assert;

namespace Microsoft.Its.Domain.Tests
{
    [TestFixture]
    public class ObjectIdTests
    {
        [Test]
        public void Id_round_trips_correctly_through_json_serializer()
        {
            var obj = new Widget
            {
                Id = 123
            };

            var json = obj.ToJson();

            var obj2 = json.FromJsonTo<Widget>();

            obj2.Id.Value.Should().Be(123);
        }

        [Test]
        public void Value_cannot_be_set_to_default()
        {
            Assert.Throws<ArgumentException>(() => new WidgetId(0));
            Assert.Throws<ArgumentException>(() => new GenericObjectId<Guid>(Guid.Empty));
        }

        [Test]
        public void Two_instances_of_ObjectId_having_the_same_underlying_value_are_equal()
        {
            Guid guid = Guid.NewGuid();
            var id1 = new GenericObjectId<Guid>(guid);
            var id2 = new GenericObjectId<Guid>(guid);

            (id1 == id2).Should().BeTrue();
            (id2 == id1).Should().BeTrue();
            (id1.Equals(id2)).Should().BeTrue();
            (id2.Equals(id1)).Should().BeTrue();
            (Equals(id1, id2)).Should().BeTrue();
        }

        [Test]
        public void Two_instances_of_type_derived_from_ObjectId_having_the_same_underlying_value_are_equal()
        {
            var id = Any.Int();
            var id1 = new WidgetId(id);
            var id2 = new WidgetId(id);

            (id1 == id2).Should().BeTrue();
            (id2 == id1).Should().BeTrue();
            (id1.Equals(id2)).Should().BeTrue();
            (id2.Equals(id1)).Should().BeTrue();
            (Equals(id1, id2)).Should().BeTrue();
        }

        [Test]
        public void An_ObjectId_is_equal_to_a_primitive_of_its_underlying_value()
        {
            var i = Any.Int();
            var objectId = new GenericObjectId<int>(i);

            (objectId == i).Should().BeTrue();
            // (i == objectId).Should().BeTrue();
            (objectId.Equals(i)).Should().BeTrue();
            // (i.Equals(objectId)).Should().BeTrue();
            (Equals(objectId, i)).Should().BeTrue();
        }

        [Test]
        public void An_instance_of_a_type_derived_from_ObjectId_is_equal_to_a_primitive_of_its_underlying_value()
        {
            var i = Any.Int();
            var objectId = new WidgetId(i);

            (objectId == i).Should().BeTrue();
            // (i == objectId).Should().BeTrue();
            (objectId.Equals(i)).Should().BeTrue();
            // (i.Equals(objectId)).Should().BeTrue();
            (Equals(objectId, i)).Should().BeTrue();
        }

        [Test]
        public void Two_ObjectIds_have_the_same_HashCode_if_created_with_the_same_underlying_value()
        {
            Guid guid = Guid.NewGuid();
            var id1 = new GenericObjectId<Guid>(guid);
            var id2 = new GenericObjectId<Guid>(guid);

            id1.GetHashCode().Should().Be(id2.GetHashCode());
        }

        [Test]
        public void ToString_returns_Value_ToString()
        {
            var id = new GenericObjectId<string>("hello");

            id.ToString().Should().Be("hello");
        }

        [Test]
        public void ObjectId_of_String_serializes_to_a_JSON_primitive()
        {
            var json = JsonConvert.SerializeObject(new GenericObjectId<string>("hello"));

            json.Should().Be("\"hello\"");
        }

        [Test]
        public void ObjectId_of_String_derived_classes_deserialize_from_JSON_primitives()
        {
            var json = "\"hello\"";

            var s = JsonConvert.DeserializeObject<GenericObjectId<string>>(json);

            s.Value.Should().Be("hello");
        }

        [Test]
        public void ObjectId_of_String_derived_classes_deserialize_from_JSON_objects()
        {
            var json = "{\"Value\":\"hello\"}";

            var s = JsonConvert.DeserializeObject<GenericObjectId<string>>(json);

            s.Value.Should().Be("hello");
        }

        [Test]
        public void ObjectId_of_int_serializes_to_a_JSON_primitive()
        {
            var json = JsonConvert.SerializeObject(new GenericObjectId<int>(1));

            json.Should().Be("1");
        }

        [Test]
        public void ObjectId_of_int_derived_classes_deserialize_from_JSON_primitives()
        {
            var json = "1";

            var s = JsonConvert.DeserializeObject<GenericObjectId<int>>(json);

            s.Value.Should().Be(1);
        }

        [Test]
        public void ObjectId_of_int_derived_classes_deserialize_from_JSON_objects()
        {
            var json = "{\"Value\":1}";

            var s = JsonConvert.DeserializeObject<GenericObjectId<int>>(json);

            s.Value.Should().Be(1);
        }

        [Test]
        public void ObjectId_of_int_correctly_serializes_to_null()
        {
            GenericObjectId<int> id = null;
            var s = JsonConvert.SerializeObject(id);
            s.Should().Be("null");
        }
    }

    public class Widget
    {
        public WidgetId Id { get; set; }
    }

    public class GenericObjectId<T> : ObjectId<T>
    {
        public GenericObjectId(T value) : base(value)
        {
        }
    }

    public class WidgetId : ObjectId<int>
    {
        public WidgetId(int value) : base(value)
        {
        }

        public static implicit operator WidgetId(int value)
        {
            return new WidgetId(value);
        }
    }
}
