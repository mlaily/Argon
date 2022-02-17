﻿#region License
// Copyright (c) 2007 James Newton-King
//
// Permission is hereby granted, free of charge, to any person
// obtaining a copy of this software and associated documentation
// files (the "Software"), to deal in the Software without
// restriction, including without limitation the rights to use,
// copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the
// Software is furnished to do so, subject to the following
// conditions:
//
// The above copyright notice and this permission notice shall be
// included in all copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND,
// EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES
// OF MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
// NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT
// HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY,
// WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING
// FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR
// OTHER DEALINGS IN THE SOFTWARE.
#endregion

using System;
using System.Collections.Generic;
using System.Runtime.Serialization.Formatters;
using Argon.Linq;
using Argon.Serialization;
using Argon.Tests.TestObjects;
using Argon.Tests.TestObjects.Organization;
using Argon.Utilities;using Xunit;
using Test = Xunit.FactAttribute;
using Assert = Argon.Tests.XUnitAssert;


namespace Argon.Tests.Serialization
{
    [TestFixture]
    public class MetadataPropertyHandlingTests : TestFixtureBase
    {
        public class User
        {
            public string Name { get; set; }
        }

        [Fact]
        public void Demo()
        {
            var json = @"{
	            'Name': 'James',
	            'Password': 'Password1',
	            '$type': 'Argon.Tests.Serialization.MetadataPropertyHandlingTests+User, Tests'
            }";

            var o = JsonConvert.DeserializeObject(json, new JsonSerializerSettings
            {
                TypeNameHandling = TypeNameHandling.All,
                // no longer needs to be first
                MetadataPropertyHandling = MetadataPropertyHandling.ReadAhead
            });

            var u = (User)o;

            Assert.AreEqual(u.Name, "James");
        }

        [Fact]
        public void DeserializeArraysWithPreserveObjectReferences()
        {
            var json = @"{
  ""$id"": ""1"",
  ""$values"": [
    null,
    {
      ""$id"": ""2"",
      ""$values"": [
        null
      ]
    },
    {
      ""$id"": ""3"",
      ""$values"": [
        {
          ""$id"": ""4"",
          ""$values"": [
            {
              ""$ref"": ""1""
            }
          ]
        }
      ]
    }
  ]
}";

            ExceptionAssert.Throws<JsonSerializationException>(() =>
            {
                JsonConvert.DeserializeObject<string[][]>(json,
                    new JsonSerializerSettings
                    {
                        PreserveReferencesHandling = PreserveReferencesHandling.All,
                        MetadataPropertyHandling = MetadataPropertyHandling.ReadAhead
                    });
            }, @"Cannot preserve reference to array or readonly list, or list created from a non-default constructor: System.String[][]. Path '$values', line 3, position 14.");
        }

        [Fact]
        public void SerializeDeserialize_DictionaryContextContainsGuid_DeserializesItemAsGuid()
        {
            const string contextKey = "k1";
            var someValue = new Guid("5dd2dba0-20c0-49f8-a054-1fa3b0a8d774");

            var inputContext = new Dictionary<string, Guid>();
            inputContext.Add(contextKey, someValue);

            var jsonSerializerSettings = new JsonSerializerSettings()
            {
                Formatting = Formatting.Indented,
                TypeNameHandling = TypeNameHandling.All,
                MetadataPropertyHandling = MetadataPropertyHandling.ReadAhead
            };
            var serializedString = JsonConvert.SerializeObject(inputContext, jsonSerializerSettings);

            StringAssert.AreEqual(@"{
  ""$type"": """ + ReflectionUtils.GetTypeName(typeof(Dictionary<string, Guid>), 0, DefaultSerializationBinder.Instance) + @""",
  ""k1"": ""5dd2dba0-20c0-49f8-a054-1fa3b0a8d774""
}", serializedString);

            var deserializedObject = (Dictionary<string, Guid>)JsonConvert.DeserializeObject(serializedString, jsonSerializerSettings);

            Assert.AreEqual(someValue, deserializedObject[contextKey]);
        }

        [Fact]
        public void DeserializeGuid()
        {
            var expected = new Item()
            {
                SourceTypeID = new Guid("d8220a4b-75b1-4b7a-8112-b7bdae956a45"),
                BrokerID = new Guid("951663c4-924e-4c86-a57a-7ed737501dbd"),
                Latitude = 33.657145,
                Longitude = -117.766684,
                TimeStamp = new DateTime(2000, 3, 1, 23, 59, 59, DateTimeKind.Utc),
                Payload = new byte[] { 0, 1, 2, 3, 4, 5, 6, 7, 8, 9 }
            };

            var jsonString = JsonConvert.SerializeObject(expected, Formatting.Indented);

            StringAssert.AreEqual(@"{
  ""SourceTypeID"": ""d8220a4b-75b1-4b7a-8112-b7bdae956a45"",
  ""BrokerID"": ""951663c4-924e-4c86-a57a-7ed737501dbd"",
  ""Latitude"": 33.657145,
  ""Longitude"": -117.766684,
  ""TimeStamp"": ""2000-03-01T23:59:59Z"",
  ""Payload"": {
    ""$type"": """ + ReflectionUtils.GetTypeName(typeof(byte[]), 0, DefaultSerializationBinder.Instance) + @""",
    ""$value"": ""AAECAwQFBgcICQ==""
  }
}", jsonString);

            var actual = JsonConvert.DeserializeObject<Item>(jsonString, new JsonSerializerSettings
            {
                MetadataPropertyHandling = MetadataPropertyHandling.ReadAhead
            });

            Assert.AreEqual(new Guid("d8220a4b-75b1-4b7a-8112-b7bdae956a45"), actual.SourceTypeID);
            Assert.AreEqual(new Guid("951663c4-924e-4c86-a57a-7ed737501dbd"), actual.BrokerID);
            var bytes = (byte[])actual.Payload;
            CollectionAssert.AreEquivalent(new byte[] { 0, 1, 2, 3, 4, 5, 6, 7, 8, 9 }, bytes);
        }

        [Fact]
        public void DeserializeListsWithPreserveObjectReferences()
        {
            var json = @"{
  ""$id"": ""1"",
  ""$values"": [
    null,
    {
      ""$id"": ""2"",
      ""$values"": [
        null
      ]
    },
    {
      ""$id"": ""3"",
      ""$values"": [
        {
          ""$id"": ""4"",
          ""$values"": [
            {
              ""$ref"": ""1""
            }
          ]
        }
      ]
    }
  ]
}";

            var circularList = JsonConvert.DeserializeObject<PreserveReferencesHandlingTests.CircularList>(json,
                new JsonSerializerSettings
                {
                    PreserveReferencesHandling = PreserveReferencesHandling.All,
                    MetadataPropertyHandling = MetadataPropertyHandling.ReadAhead
                });

            Assert.AreEqual(3, circularList.Count);
            Assert.AreEqual(null, circularList[0]);
            Assert.AreEqual(1, circularList[1].Count);
            Assert.AreEqual(1, circularList[2].Count);
            Assert.AreEqual(1, circularList[2][0].Count);
            Assert.IsTrue(ReferenceEquals(circularList, circularList[2][0][0]));
        }

        [Fact]
        public void DeserializeTypeNameOnly()
        {
            var json = @"{
  ""$id"": ""1"",
  ""$type"": ""Argon.Tests.TestObjects.Employee"",
  ""Name"": ""Name!"",
  ""Manager"": null
}";

            ExceptionAssert.Throws<JsonSerializationException>(() =>
            {
                JsonConvert.DeserializeObject(json, null, new JsonSerializerSettings
                {
                    TypeNameHandling = TypeNameHandling.Objects,
                    MetadataPropertyHandling = MetadataPropertyHandling.ReadAhead
                });
            }, "Type specified in JSON 'Argon.Tests.TestObjects.Employee' was not resolved. Path '$type', line 3, position 45.");
        }

        [Fact]
        public void SerializeRefNull()
        {
            var reference = new Dictionary<string, object>();
            reference.Add("blah", "blah!");
            reference.Add("$ref", null);
            reference.Add("$id", null);

            var child = new Dictionary<string, object>();
            child.Add("_id", 2);
            child.Add("Name", "Isabell");
            child.Add("Father", reference);

            var json = JsonConvert.SerializeObject(child, Formatting.Indented);

            StringAssert.AreEqual(@"{
  ""_id"": 2,
  ""Name"": ""Isabell"",
  ""Father"": {
    ""blah"": ""blah!"",
    ""$ref"": null,
    ""$id"": null
  }
}", json);

            var result = JsonConvert.DeserializeObject<Dictionary<string, object>>(json, new JsonSerializerSettings
            {
                MetadataPropertyHandling = MetadataPropertyHandling.ReadAhead
            });

            Assert.AreEqual(3, result.Count);
            Assert.AreEqual(1, ((JObject)result["Father"]).Count);
            Assert.AreEqual("blah!", (string)((JObject)result["Father"])["blah"]);
        }

        [Fact]
        public void DeserializeEmployeeReference()
        {
            var json = @"[
  {
    ""Name"": ""Mike Manager"",
    ""$id"": ""1"",
    ""Manager"": null
  },
  {
    ""Name"": ""Joe User"",
    ""$id"": ""2"",
    ""Manager"": {
      ""$ref"": ""1""
    }
  }
]";

            var employees = JsonConvert.DeserializeObject<List<EmployeeReference>>(json, new JsonSerializerSettings
            {
                MetadataPropertyHandling = MetadataPropertyHandling.ReadAhead
            });

            Assert.AreEqual(2, employees.Count);
            Assert.AreEqual("Mike Manager", employees[0].Name);
            Assert.AreEqual("Joe User", employees[1].Name);
            Assert.AreEqual(employees[0], employees[1].Manager);
        }

        [Fact]
        public void DeserializeFromJToken()
        {
            var json = @"[
  {
    ""Name"": ""Mike Manager"",
    ""$id"": ""1"",
    ""Manager"": null
  },
  {
    ""Name"": ""Joe User"",
    ""$id"": ""2"",
    ""Manager"": {
      ""$ref"": ""1""
    }
  }
]";

            var t1 = JToken.Parse(json);
            var t2 = t1.CloneToken();

            var serializer = JsonSerializer.Create(new JsonSerializerSettings
            {
                MetadataPropertyHandling = MetadataPropertyHandling.ReadAhead
            });
            var employees = t1.ToObject<List<EmployeeReference>>(serializer);

            Assert.AreEqual(2, employees.Count);
            Assert.AreEqual("Mike Manager", employees[0].Name);
            Assert.AreEqual("Joe User", employees[1].Name);
            Assert.AreEqual(employees[0], employees[1].Manager);

            Assert.IsTrue(JToken.DeepEquals(t1, t2));
        }

        [Fact]
        public void DeserializeGenericObjectListWithTypeName()
        {
            var employeeRef = typeof(EmployeeReference).AssemblyQualifiedName;
            var personRef = typeof(Person).AssemblyQualifiedName;

            var json = @"[
  {
    ""Name"": ""Bob"",
    ""$id"": ""1"",
    ""$type"": """ + employeeRef + @""",
    ""Manager"": {
      ""$id"": ""2"",
      ""$type"": """ + employeeRef + @""",
      ""Name"": ""Frank"",
      ""Manager"": null
    }
  },
  {
    ""Name"": null,
    ""$type"": """ + personRef + @""",
    ""BirthDate"": ""\/Date(978134400000)\/"",
    ""LastModified"": ""\/Date(978134400000)\/""
  },
  ""String!"",
  -2147483648
]";

            var values = (List<object>)JsonConvert.DeserializeObject(json, typeof(List<object>), new JsonSerializerSettings
            {
                TypeNameHandling = TypeNameHandling.Objects,
#pragma warning disable 618
                TypeNameAssemblyFormat = FormatterAssemblyStyle.Full,
#pragma warning restore 618
                MetadataPropertyHandling = MetadataPropertyHandling.ReadAhead
            });

            Assert.AreEqual(4, values.Count);

            var e = (EmployeeReference)values[0];
            var p = (Person)values[1];

            Assert.AreEqual("Bob", e.Name);
            Assert.AreEqual("Frank", e.Manager.Name);

            Assert.AreEqual(null, p.Name);
            Assert.AreEqual(new DateTime(2000, 12, 30, 0, 0, 0, DateTimeKind.Utc), p.BirthDate);
            Assert.AreEqual(new DateTime(2000, 12, 30, 0, 0, 0, DateTimeKind.Utc), p.LastModified);

            Assert.AreEqual("String!", values[2]);
            Assert.AreEqual((long)int.MinValue, values[3]);
        }

        [Fact]
        public void WriteListTypeNameForProperty()
        {
            var listRef = ReflectionUtils.GetTypeName(typeof(List<int>), TypeNameAssemblyFormatHandling.Simple, null);

            var typeNameProperty = new TypeNameHandlingTests.TypeNameProperty
            {
                Name = "Name!",
                Value = new List<int> { 1, 2, 3, 4, 5 }
            };

            var json = JsonConvert.SerializeObject(typeNameProperty, Formatting.Indented);

            StringAssert.AreEqual(@"{
  ""Name"": ""Name!"",
  ""Value"": {
    ""$type"": """ + listRef + @""",
    ""$values"": [
      1,
      2,
      3,
      4,
      5
    ]
  }
}", json);

            var deserialized = JsonConvert.DeserializeObject<TypeNameHandlingTests.TypeNameProperty>(json, new JsonSerializerSettings
            {
                MetadataPropertyHandling = MetadataPropertyHandling.ReadAhead
            });
            Assert.AreEqual("Name!", deserialized.Name);
            CustomAssert.IsInstanceOfType(typeof(List<int>), deserialized.Value);

            var nested = (List<int>)deserialized.Value;
            Assert.AreEqual(5, nested.Count);
            Assert.AreEqual(1, nested[0]);
            Assert.AreEqual(2, nested[1]);
            Assert.AreEqual(3, nested[2]);
            Assert.AreEqual(4, nested[3]);
            Assert.AreEqual(5, nested[4]);
        }

        public class MetadataPropertyDisabledTestClass
        {
            [JsonProperty("$id")]
            public string Id { get; set; }

            [JsonProperty("$ref")]
            public string Ref { get; set; }

            [JsonProperty("$value")]
            public string Value { get; set; }

            [JsonProperty("$values")]
            public string Values { get; set; }

            [JsonProperty("$type")]
            public string Type { get; set; }
        }

        [Fact]
        public void MetadataPropertyHandlingIgnore()
        {
            var c1 = new MetadataPropertyDisabledTestClass
            {
                Id = "Id!",
                Ref = "Ref!",
                Type = "Type!",
                Value = "Value!",
                Values = "Values!"
            };

            var json = JsonConvert.SerializeObject(c1, Formatting.Indented);

            StringAssert.AreEqual(@"{
  ""$id"": ""Id!"",
  ""$ref"": ""Ref!"",
  ""$value"": ""Value!"",
  ""$values"": ""Values!"",
  ""$type"": ""Type!""
}", json);

            var c2 = JsonConvert.DeserializeObject<MetadataPropertyDisabledTestClass>(json, new JsonSerializerSettings
            {
                MetadataPropertyHandling = MetadataPropertyHandling.Ignore
            });

            Assert.AreEqual("Id!", c2.Id);
            Assert.AreEqual("Ref!", c2.Ref);
            Assert.AreEqual("Type!", c2.Type);
            Assert.AreEqual("Value!", c2.Value);
            Assert.AreEqual("Values!", c2.Values);
        }

        [Fact]
        public void MetadataPropertyHandlingIgnore_EmptyObject()
        {
            var json = @"{}";

            var c = JsonConvert.DeserializeObject<MetadataPropertyDisabledTestClass>(json, new JsonSerializerSettings
            {
                MetadataPropertyHandling = MetadataPropertyHandling.Ignore
            });

            Assert.AreEqual(null, c.Id);
        }

        [Fact]
        public void PrimitiveType_MetadataPropertyIgnore()
        {
            var actual = JsonConvert.DeserializeObject<Item>(@"{
  ""SourceTypeID"": ""d8220a4b-75b1-4b7a-8112-b7bdae956a45"",
  ""BrokerID"": ""951663c4-924e-4c86-a57a-7ed737501dbd"",
  ""Latitude"": 33.657145,
  ""Longitude"": -117.766684,
  ""TimeStamp"": ""2000-03-01T23:59:59Z"",
  ""Payload"": {
    ""$type"": ""System.Byte[], mscorlib"",
    ""$value"": ""AAECAwQFBgcICQ==""
  }
}",
                new JsonSerializerSettings
                {
                    MetadataPropertyHandling = MetadataPropertyHandling.Ignore
                });

            Assert.AreEqual(new Guid("d8220a4b-75b1-4b7a-8112-b7bdae956a45"), actual.SourceTypeID);
            Assert.AreEqual(new Guid("951663c4-924e-4c86-a57a-7ed737501dbd"), actual.BrokerID);
            var o = (JObject)actual.Payload;
            Assert.AreEqual("System.Byte[], mscorlib", (string)o["$type"]);
            Assert.AreEqual("AAECAwQFBgcICQ==", (string)o["$value"]);
            Assert.AreEqual(null, o.Parent);
        }

        [Fact]
        public void ReadAhead_JObject_NoParent()
        {
            var actual = JsonConvert.DeserializeObject<ItemWithUntypedPayload>(@"{
  ""Payload"": {}
}",
                new JsonSerializerSettings
                {
                    MetadataPropertyHandling = MetadataPropertyHandling.ReadAhead
                });

            var o = (JObject)actual.Payload;
            Assert.AreEqual(null, o.Parent);
        }

        public class ItemWithJTokens
        {
            public JValue Payload1 { get; set; }
            public JObject Payload2 { get; set; }
            public JArray Payload3 { get; set; }
        }

        [Fact]
        public void ReadAhead_TypedJValue_NoParent()
        {
            var actual = (ItemWithJTokens)JsonConvert.DeserializeObject(@"{
  ""Payload1"": 1,
  ""Payload2"": {'prop1':1,'prop2':[2]},
  ""Payload3"": [1],
  ""$type"": ""Argon.Tests.Serialization.MetadataPropertyHandlingTests+ItemWithJTokens, Tests""
}",
                new JsonSerializerSettings
                {
                    MetadataPropertyHandling = MetadataPropertyHandling.ReadAhead,
                    TypeNameHandling = TypeNameHandling.All
                });

            Assert.AreEqual(JTokenType.Integer, actual.Payload1.Type);
            Assert.AreEqual(1, (int)actual.Payload1);
            Assert.AreEqual(null, actual.Payload1.Parent);

            Assert.AreEqual(JTokenType.Object, actual.Payload2.Type);
            Assert.AreEqual(1, (int)actual.Payload2["prop1"]);
            Assert.AreEqual(2, (int)actual.Payload2["prop2"][0]);
            Assert.AreEqual(null, actual.Payload2.Parent);

            Assert.AreEqual(1, (int)actual.Payload3[0]);
            Assert.AreEqual(null, actual.Payload3.Parent);
        }

        [Fact]
        public void ReadAhead_JArray_NoParent()
        {
            var actual = JsonConvert.DeserializeObject<ItemWithUntypedPayload>(@"{
  ""Payload"": [1]
}",
                new JsonSerializerSettings
                {
                    MetadataPropertyHandling = MetadataPropertyHandling.ReadAhead
                });

            var o = (JArray)actual.Payload;
            Assert.AreEqual(null, o.Parent);
        }

        public class ItemWithUntypedPayload
        {
            public object Payload { get; set; }
        }

        [Fact]
        public void PrimitiveType_MetadataPropertyIgnore_WithNoType()
        {
            var actual = JsonConvert.DeserializeObject<ItemWithUntypedPayload>(@"{
  ""Payload"": {
    ""$type"": ""System.Single, mscorlib"",
    ""$value"": ""5""
  }
}",
                new JsonSerializerSettings
                {
                    TypeNameHandling = TypeNameHandling.Auto
                });

            Assert.AreEqual(5f, actual.Payload);

            actual = JsonConvert.DeserializeObject<ItemWithUntypedPayload>(@"{
  ""Payload"": {
    ""$type"": ""System.Single, mscorlib"",
    ""$value"": ""5""
  }
}",
                new JsonSerializerSettings
                {
                    TypeNameHandling = TypeNameHandling.Auto,
                    MetadataPropertyHandling = MetadataPropertyHandling.Ignore
                });

            Assert.IsTrue(actual.Payload is JObject);
        }

        [Fact]
        public void DeserializeCircularReferencesWithConverter()
        {
            var json = @"{
  ""$id"": ""1"",
  ""$type"": ""CircularReferenceClass""
}";

            var c = new MetadataPropertyDisabledTestClass();

            JsonConvert.PopulateObject(json, c, new JsonSerializerSettings
            {
                MetadataPropertyHandling = MetadataPropertyHandling.Ignore
            });

            Assert.AreEqual("1", c.Id);
            Assert.AreEqual("CircularReferenceClass", c.Type);
        }
    }
}