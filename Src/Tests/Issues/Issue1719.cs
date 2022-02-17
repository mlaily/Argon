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

using System.Collections.Generic;
using System.ComponentModel;
using Xunit;
using Test = Xunit.FactAttribute;
using Assert = Argon.Tests.XUnitAssert;

namespace Argon.Tests.Issues
{
    [TestFixture]
    public class Issue1719 : TestFixtureBase
    {
        [Fact]
        public void Test()
        {
            var a = JsonConvert.DeserializeObject<ExtensionDataTestClass>("{\"E\":null}", new JsonSerializerSettings
            {
                NullValueHandling = NullValueHandling.Ignore,
            });

            Assert.IsNull(a.PropertyBag);
        }

        [Fact]
        public void Test_PreviousWorkaround()
        {
            var a = JsonConvert.DeserializeObject<ExtensionDataTestClassWorkaround>("{\"E\":null}", new JsonSerializerSettings
            {
                NullValueHandling = NullValueHandling.Ignore,
            });

            Assert.IsNull(a.PropertyBag);
        }

        [Fact]
        public void Test_DefaultValue()
        {
            var a = JsonConvert.DeserializeObject<ExtensionDataWithDefaultValueTestClass>("{\"E\":2}", new JsonSerializerSettings
            {
                DefaultValueHandling = DefaultValueHandling.Ignore,
            });

            Assert.IsNull(a.PropertyBag);
        }

        class ExtensionDataTestClass
        {
            public B? E { get; set; }

            [JsonExtensionData]
            public IDictionary<string, object> PropertyBag { get; set; }
        }

        class ExtensionDataWithDefaultValueTestClass
        {
            [DefaultValue(2)]
            public int? E { get; set; }

            [JsonExtensionData]
            public IDictionary<string, object> PropertyBag { get; set; }
        }

        enum B
        {
            One,
            Two
        }

        class ExtensionDataTestClassWorkaround
        {
            [JsonProperty(DefaultValueHandling = DefaultValueHandling.IgnoreAndPopulate, NullValueHandling = NullValueHandling.Include)]
            public B? E { get; set; }

            [JsonExtensionData]
            public IDictionary<string, object> PropertyBag { get; set; }
        }
    }
}