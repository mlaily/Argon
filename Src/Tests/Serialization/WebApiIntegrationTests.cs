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
using System.IO;
using System.Text;
using Xunit;
using Test = Xunit.FactAttribute;
using Assert = Argon.Tests.XUnitAssert;
using System.Runtime.Serialization.Json;
using Argon.Serialization;

namespace Argon.Tests.Serialization
{
    [TestFixture]
    public class WebApiIntegrationTests : TestFixtureBase
    {
        [Fact]
        public void SerializeSerializableType()
        {
            var serializableType = new SerializableType("protected")
            {
                publicField = "public",
                protectedInternalField = "protected internal",
                internalField = "internal",
                PublicProperty = "private",
                nonSerializedField = "Error"
            };

#if !NET5_0_OR_GREATER
            var ms = new MemoryStream();
            var dataContractJsonSerializer = new DataContractJsonSerializer(typeof(SerializableType));
            dataContractJsonSerializer.WriteObject(ms, serializableType);

            var dtJson = Encoding.UTF8.GetString(ms.ToArray());
            var dtExpected = @"{""internalField"":""internal"",""privateField"":""private"",""protectedField"":""protected"",""protectedInternalField"":""protected internal"",""publicField"":""public""}";

            Assert.AreEqual(dtExpected, dtJson);
#endif

            var expected = "{\"publicField\":\"public\",\"internalField\":\"internal\",\"protectedInternalField\":\"protected internal\",\"protectedField\":\"protected\",\"privateField\":\"private\"}";
            var json = JsonConvert.SerializeObject(serializableType, new JsonSerializerSettings
            {
                ContractResolver = new DefaultContractResolver
                {
                    IgnoreSerializableAttribute = false
                }
            });

            Assert.AreEqual(expected, json);
        }

        [Fact]
        public void SerializeInheritedType()
        {
            var serializableType = new InheritedType("protected")
            {
                publicField = "public",
                protectedInternalField = "protected internal",
                internalField = "internal",
                PublicProperty = "private",
                nonSerializedField = "Error",
                inheritedTypeField = "inherited"
            };

            var json = JsonConvert.SerializeObject(serializableType);

            Assert.AreEqual(@"{""inheritedTypeField"":""inherited"",""publicField"":""public"",""PublicProperty"":""private""}", json);
        }
    }

    public class InheritedType : SerializableType
    {
        public string inheritedTypeField;

        public InheritedType(string protectedFieldValue) : base(protectedFieldValue)
        {
        }
    }

    [Serializable]
    public class SerializableType : IEquatable<SerializableType>
    {
        public SerializableType(string protectedFieldValue)
        {
            protectedField = protectedFieldValue;
        }

        public string publicField;
        internal string internalField;
        protected internal string protectedInternalField;
        protected string protectedField;
        private string privateField;

        public string PublicProperty
        {
            get { return privateField; }
            set { privateField = value; }
        }

#if !NET5_0_OR_GREATER
        [NonSerialized]
#else
        [JsonIgnore]
#endif
        public string nonSerializedField;

        public bool Equals(SerializableType other)
        {
            return publicField == other.publicField &&
                   internalField == other.internalField &&
                   protectedInternalField == other.protectedInternalField &&
                   protectedField == other.protectedField &&
                   privateField == other.privateField;
        }
    }
}