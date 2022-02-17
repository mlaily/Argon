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
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using Xunit;
using Test = Xunit.FactAttribute;
using Assert = Argon.Tests.XUnitAssert;
using Argon.Converters;
using Argon.Linq;
using Argon.Tests.TestObjects;
using Argon.Tests.TestObjects.Organization;
using System.Linq;
using System.IO;
using System.Runtime.Serialization;

namespace Argon.Tests.Linq
{
    [TestFixture]
    public class LinqToJsonTest : TestFixtureBase
    {
        [Fact]
        public void EscapedQuotePath()
        {
            var v = new JValue(1);
            var o = new JObject();
            o["We're offline!"] = v;

            Assert.AreEqual(@"['We\'re offline!']", v.Path);
        }

        public class DemoClass
        {
            public decimal maxValue;
        }

        [Fact]
        public void ToObjectDecimal()
        {
            var jArray = JArray.Parse("[{ maxValue:10000000000000000000 }]");
            var list = jArray.ToObject<List<DemoClass>>();

            Assert.AreEqual(10000000000000000000m, list[0].maxValue);
        }

        [Fact]
        public void ToObjectFromGuidToString()
        {
            var token = new JValue(new Guid("91274484-3b20-48b4-9d18-7d936b2cb88f"));
            var value = token.ToObject<string>();
            Assert.AreEqual("91274484-3b20-48b4-9d18-7d936b2cb88f", value);
        }

        [Fact]
        public void ToObjectFromIntegerToString()
        {
            var token = new JValue(1234);
            var value = token.ToObject<string>();
            Assert.AreEqual("1234", value);
        }

        [Fact]
        public void ToObjectFromStringToInteger()
        {
            var token = new JValue("1234");
            var value = token.ToObject<int>();
            Assert.AreEqual(1234, value);
        }

        [Fact]
        public void FromObjectGuid()
        {
            var token1 = new JValue(Guid.NewGuid());
            var token2 = JToken.FromObject(token1);
            Assert.IsTrue(JToken.DeepEquals(token1, token2));
            Assert.AreEqual(token1.Type, token2.Type);
        }

        [Fact]
        public void FromObjectTimeSpan()
        {
            var token1 = new JValue(TimeSpan.FromDays(1));
            var token2 = JToken.FromObject(token1);
            Assert.IsTrue(JToken.DeepEquals(token1, token2));
            Assert.AreEqual(token1.Type, token2.Type);
        }

        [Fact]
        public void FromObjectUri()
        {
            var token1 = new JValue(new Uri("http://www.newtonsoft.com"));
            var token2 = JToken.FromObject(token1);
            Assert.IsTrue(JToken.DeepEquals(token1, token2));
            Assert.AreEqual(token1.Type, token2.Type);
        }

        [Fact]
        public void ToObject_Guid()
        {
            var anon = new JObject
            {
                ["id"] = Guid.NewGuid()
            };
            Assert.AreEqual(JTokenType.Guid, anon["id"].Type);

            var dict = anon.ToObject<Dictionary<string, JToken>>();
            Assert.AreEqual(JTokenType.Guid, dict["id"].Type);
        }

        public class TestClass_ULong
        {
            public ulong Value { get; set; }
        }

        [Fact]
        public void FromObject_ULongMaxValue()
        {
            var instance = new TestClass_ULong { Value = ulong.MaxValue };
            var output = JObject.FromObject(instance);

            StringAssert.AreEqual(@"{
  ""Value"": 18446744073709551615
}", output.ToString());
        }

        public class TestClass_Byte
        {
            public byte Value { get; set; }
        }

        [Fact]
        public void FromObject_ByteMaxValue()
        {
            var instance = new TestClass_Byte { Value = byte.MaxValue };
            var output = JObject.FromObject(instance);

            StringAssert.AreEqual(@"{
  ""Value"": 255
}", output.ToString());
        }

        [Fact]
        public void ToObject_Base64AndGuid()
        {
            var o = JObject.Parse("{'responseArray':'AAAAAAAAAAAAAAAAAAAAAAAAAAABAAAA'}");
            var data = o["responseArray"].ToObject<byte[]>();
            var expected = Convert.FromBase64String("AAAAAAAAAAAAAAAAAAAAAAAAAAABAAAA");

            CollectionAssert.AreEqual(expected, data);

            o = JObject.Parse("{'responseArray':'AAAAAAAA-AAAA-AAAA-AAAA-AAAAAAABAAAA'}");
            data = o["responseArray"].ToObject<byte[]>();
            expected = new Guid("AAAAAAAA-AAAA-AAAA-AAAA-AAAAAAABAAAA").ToByteArray();

            CollectionAssert.AreEqual(expected, data);
        }

        [Fact]
        public void IncompleteContainers()
        {
            ExceptionAssert.Throws<JsonReaderException>(
                () => JArray.Parse("[1,"),
                "Unexpected end of content while loading JArray. Path '[0]', line 1, position 3.");

            ExceptionAssert.Throws<JsonReaderException>(
                () => JArray.Parse("[1"),
                "Unexpected end of content while loading JArray. Path '[0]', line 1, position 2.");

            ExceptionAssert.Throws<JsonReaderException>(
                () => JObject.Parse("{'key':1,"),
                "Unexpected end of content while loading JObject. Path 'key', line 1, position 9.");

            ExceptionAssert.Throws<JsonReaderException>(
                () => JObject.Parse("{'key':1"),
                "Unexpected end of content while loading JObject. Path 'key', line 1, position 8.");
        }

        [Fact]
        public void EmptyJEnumerableCount()
        {
            var tokens = new JEnumerable<JToken>();

            Assert.AreEqual(0, tokens.Count());
        }

        [Fact]
        public void EmptyJEnumerableAsEnumerable()
        {
            IEnumerable tokens = new JEnumerable<JToken>();

            Assert.AreEqual(0, tokens.Cast<JToken>().Count());
        }

        [Fact]
        public void EmptyJEnumerableEquals()
        {
            var tokens1 = new JEnumerable<JToken>();
            var tokens2 = new JEnumerable<JToken>();

            Assert.IsTrue(tokens1.Equals(tokens2));

            object o1 = new JEnumerable<JToken>();
            object o2 = new JEnumerable<JToken>();

            Assert.IsTrue(o1.Equals(o2));
        }

        [Fact]
        public void EmptyJEnumerableGetHashCode()
        {
            var tokens = new JEnumerable<JToken>();

            Assert.AreEqual(0, tokens.GetHashCode());
        }

        [Fact]
        public void CommentsAndReadFrom()
        {
            var textReader = new StringReader(@"[
    // hi
    1,
    2,
    3
]");

            var jsonReader = new JsonTextReader(textReader);
            var a = (JArray)JToken.ReadFrom(jsonReader, new JsonLoadSettings
            {
                CommentHandling = CommentHandling.Load
            });

            Assert.AreEqual(4, a.Count);
            Assert.AreEqual(JTokenType.Comment, a[0].Type);
            Assert.AreEqual(" hi", ((JValue)a[0]).Value);
        }

        [Fact]
        public void CommentsAndReadFrom_IgnoreComments()
        {
            var textReader = new StringReader(@"[
    // hi
    1,
    2,
    3
]");

            var jsonReader = new JsonTextReader(textReader);
            var a = (JArray)JToken.ReadFrom(jsonReader);

            Assert.AreEqual(3, a.Count);
            Assert.AreEqual(JTokenType.Integer, a[0].Type);
            Assert.AreEqual(1L, ((JValue)a[0]).Value);
        }

        [Fact]
        public void StartingCommentAndReadFrom()
        {
            var textReader = new StringReader(@"
// hi
[
    1,
    2,
    3
]");

            var jsonReader = new JsonTextReader(textReader);
            var v = (JValue)JToken.ReadFrom(jsonReader, new JsonLoadSettings
            {
                CommentHandling = CommentHandling.Load
            });

            Assert.AreEqual(JTokenType.Comment, v.Type);

            IJsonLineInfo lineInfo = v;
            Assert.AreEqual(true, lineInfo.HasLineInfo());
            Assert.AreEqual(2, lineInfo.LineNumber);
            Assert.AreEqual(5, lineInfo.LinePosition);
        }

        [Fact]
        public void StartingCommentAndReadFrom_IgnoreComments()
        {
            var textReader = new StringReader(@"
// hi
[
    1,
    2,
    3
]");

            var jsonReader = new JsonTextReader(textReader);
            var a = (JArray)JToken.ReadFrom(jsonReader, new JsonLoadSettings
            {
                CommentHandling = CommentHandling.Ignore
            });

            Assert.AreEqual(JTokenType.Array, a.Type);

            IJsonLineInfo lineInfo = a;
            Assert.AreEqual(true, lineInfo.HasLineInfo());
            Assert.AreEqual(3, lineInfo.LineNumber);
            Assert.AreEqual(1, lineInfo.LinePosition);
        }

        [Fact]
        public void StartingUndefinedAndReadFrom()
        {
            var textReader = new StringReader(@"
undefined
[
    1,
    2,
    3
]");

            var jsonReader = new JsonTextReader(textReader);
            var v = (JValue)JToken.ReadFrom(jsonReader);

            Assert.AreEqual(JTokenType.Undefined, v.Type);

            IJsonLineInfo lineInfo = v;
            Assert.AreEqual(true, lineInfo.HasLineInfo());
            Assert.AreEqual(2, lineInfo.LineNumber);
            Assert.AreEqual(9, lineInfo.LinePosition);
        }

        [Fact]
        public void StartingEndArrayAndReadFrom()
        {
            var textReader = new StringReader(@"[]");

            var jsonReader = new JsonTextReader(textReader);
            jsonReader.Read();
            jsonReader.Read();

            ExceptionAssert.Throws<JsonReaderException>(() => JToken.ReadFrom(jsonReader), @"Error reading JToken from JsonReader. Unexpected token: EndArray. Path '', line 1, position 2.");
        }

        [Fact]
        public void JPropertyPath()
        {
            var o = new JObject
            {
                {
                    "person",
                    new JObject
                    {
                        { "$id", 1 }
                    }
                }
            };

            var idProperty = o["person"]["$id"].Parent;
            Assert.AreEqual("person.$id", idProperty.Path);
        }

        [Fact]
        public void EscapedPath()
        {
            var json = @"{
  ""frameworks"": {
    ""NET5_0_OR_GREATER"": {
      ""dependencies"": {
        ""System.Xml.ReaderWriter"": {
          ""source"": ""NuGet""
        }
      }
    }
  }
}";

            var o = JObject.Parse(json);

            var v1 = o["frameworks"]["NET5_0_OR_GREATER"]["dependencies"]["System.Xml.ReaderWriter"]["source"];

            Assert.AreEqual("frameworks.NET5_0_OR_GREATER.dependencies['System.Xml.ReaderWriter'].source", v1.Path);

            var v2 = o.SelectToken(v1.Path);

            Assert.AreEqual(v1, v2);
        }

        [Fact]
        public void EscapedPathTests()
        {
            EscapedPathAssert("this has spaces", "['this has spaces']");
            EscapedPathAssert("(RoundBraces)", "['(RoundBraces)']");
            EscapedPathAssert("[SquareBraces]", "['[SquareBraces]']");
            EscapedPathAssert("this.has.dots", "['this.has.dots']");
        }

        private void EscapedPathAssert(string propertyName, string expectedPath)
        {
            var v1 = int.MaxValue;
            var value = new JValue(v1);

            var o = new JObject(new JProperty(propertyName, value));

            Assert.AreEqual(expectedPath, value.Path);

            var selectedValue = (JValue)o.SelectToken(value.Path);

            Assert.AreEqual(value, selectedValue);
        }

        [Fact]
        public void ForEach()
        {
            var items = new JArray(new JObject(new JProperty("name", "value!")));

            foreach (JObject friend in items)
            {
                StringAssert.AreEqual(@"{
  ""name"": ""value!""
}", friend.ToString());
            }
        }

        [Fact]
        public void DoubleValue()
        {
            var j = JArray.Parse("[-1E+4,100.0e-2]");

            var value = (double)j[0];
            Assert.AreEqual(-10000d, value);

            value = (double)j[1];
            Assert.AreEqual(1d, value);
        }

        [Fact]
        public void Manual()
        {
            var array = new JArray();
            var text = new JValue("Manual text");
            var date = new JValue(new DateTime(2000, 5, 23));

            array.Add(text);
            array.Add(date);

            var json = array.ToString();
            // [
            //   "Manual text",
            //   "\/Date(958996800000+1200)\/"
            // ]
        }

        [Fact]
        public void LinqToJsonDeserialize()
        {
            var o = new JObject(
                new JProperty("Name", "John Smith"),
                new JProperty("BirthDate", new DateTime(1983, 3, 20))
                );

            var serializer = new JsonSerializer();
            var p = (Person)serializer.Deserialize(new JTokenReader(o), typeof(Person));

            Assert.AreEqual("John Smith", p.Name);
        }

        [Fact]
        public void ObjectParse()
        {
            var json = @"{
        CPU: 'Intel',
        Drives: [
          'DVD read/writer',
          ""500 gigabyte hard drive""
        ]
      }";

            var o = JObject.Parse(json);
            IList<JProperty> properties = o.Properties().ToList();

            Assert.AreEqual("CPU", properties[0].Name);
            Assert.AreEqual("Intel", (string)properties[0].Value);
            Assert.AreEqual("Drives", properties[1].Name);

            var list = (JArray)properties[1].Value;
            Assert.AreEqual(2, list.Children().Count());
            Assert.AreEqual("DVD read/writer", (string)list.Children().ElementAt(0));
            Assert.AreEqual("500 gigabyte hard drive", (string)list.Children().ElementAt(1));

            var parameterValues =
                (from p in o.Properties()
                    where p.Value is JValue
                    select ((JValue)p.Value).Value).ToList();

            Assert.AreEqual(1, parameterValues.Count);
            Assert.AreEqual("Intel", parameterValues[0]);
        }

        [Fact]
        public void CreateLongArray()
        {
            var json = @"[0,1,2,3,4,5,6,7,8,9]";

            var a = JArray.Parse(json);
            var list = a.Values<int>().ToList();

            var expected = new List<int>() { 0, 1, 2, 3, 4, 5, 6, 7, 8, 9 };

            CollectionAssert.AreEqual(expected, list);
        }

        [Fact]
        public void GoogleSearchAPI()
        {
            #region GoogleJson
            var json = @"{
    results:
        [
            {
                GsearchResultClass:""GwebSearch"",
                unescapedUrl : ""http://www.google.com/"",
                url : ""http://www.google.com/"",
                visibleUrl : ""www.google.com"",
                cacheUrl : 
""http://www.google.com/search?q=cache:zhool8dxBV4J:www.google.com"",
                title : ""Google"",
                titleNoFormatting : ""Google"",
                content : ""Enables users to search the Web, Usenet, and 
images. Features include PageRank,   caching and translation of 
results, and an option to find similar pages.""
            },
            {
                GsearchResultClass:""GwebSearch"",
                unescapedUrl : ""http://news.google.com/"",
                url : ""http://news.google.com/"",
                visibleUrl : ""news.google.com"",
                cacheUrl : 
""http://www.google.com/search?q=cache:Va_XShOz_twJ:news.google.com"",
                title : ""Google News"",
                titleNoFormatting : ""Google News"",
                content : ""Aggregated headlines and a search engine of many of the world's news sources.""
            },
            
            {
                GsearchResultClass:""GwebSearch"",
                unescapedUrl : ""http://groups.google.com/"",
                url : ""http://groups.google.com/"",
                visibleUrl : ""groups.google.com"",
                cacheUrl : 
""http://www.google.com/search?q=cache:x2uPD3hfkn0J:groups.google.com"",
                title : ""Google Groups"",
                titleNoFormatting : ""Google Groups"",
                content : ""Enables users to search and browse the Usenet 
archives which consist of over 700   million messages, and post new 
comments.""
            },
            
            {
                GsearchResultClass:""GwebSearch"",
                unescapedUrl : ""http://maps.google.com/"",
                url : ""http://maps.google.com/"",
                visibleUrl : ""maps.google.com"",
                cacheUrl : 
""http://www.google.com/search?q=cache:dkf5u2twBXIJ:maps.google.com"",
                title : ""Google Maps"",
                titleNoFormatting : ""Google Maps"",
                content : ""Provides directions, interactive maps, and 
satellite/aerial imagery of the United   States. Can also search by 
keyword such as type of business.""
            }
        ],
        
    adResults:
        [
            {
                GsearchResultClass:""GwebSearch.ad"",
                title : ""Gartner Symposium/ITxpo"",
                content1 : ""Meet brilliant Gartner IT analysts"",
                content2 : ""20-23 May 2007- Barcelona, Spain"",
                url : 
""http://www.google.com/url?sa=L&ai=BVualExYGRo3hD5ianAPJvejjD8-s6ye7kdTwArbI4gTAlrECEAEYASDXtMMFOAFQubWAjvr_____AWDXw_4EiAEBmAEAyAEBgAIB&num=1&q=http://www.gartner.com/it/sym/2007/spr8/spr8.jsp%3Fsrc%3D_spain_07_%26WT.srch%3D1&usg=__CxRH06E4Xvm9Muq13S4MgMtnziY="", 

                impressionUrl : 
""http://www.google.com/uds/css/ad-indicator-on.gif?ai=BVualExYGRo3hD5ianAPJvejjD8-s6ye7kdTwArbI4gTAlrECEAEYASDXtMMFOAFQubWAjvr_____AWDXw_4EiAEBmAEAyAEBgAIB"", 

                unescapedUrl : 
""http://www.google.com/url?sa=L&ai=BVualExYGRo3hD5ianAPJvejjD8-s6ye7kdTwArbI4gTAlrECEAEYASDXtMMFOAFQubWAjvr_____AWDXw_4EiAEBmAEAyAEBgAIB&num=1&q=http://www.gartner.com/it/sym/2007/spr8/spr8.jsp%3Fsrc%3D_spain_07_%26WT.srch%3D1&usg=__CxRH06E4Xvm9Muq13S4MgMtnziY="", 

                visibleUrl : ""www.gartner.com""
            }
        ]
}
";
            #endregion

            var o = JObject.Parse(json);

            var resultObjects = o["results"].Children<JObject>().ToList();

            Assert.AreEqual(32, resultObjects.Properties().Count());

            Assert.AreEqual(32, resultObjects.Cast<JToken>().Values().Count());

            Assert.AreEqual(4, resultObjects.Cast<JToken>().Values("GsearchResultClass").Count());

            Assert.AreEqual(5, o.PropertyValues().Cast<JArray>().Children().Count());

            var resultUrls = o["results"].Children().Values<string>("url").ToList();

            var expectedUrls = new List<string>() { "http://www.google.com/", "http://news.google.com/", "http://groups.google.com/", "http://maps.google.com/" };

            CollectionAssert.AreEqual(expectedUrls, resultUrls);

            var descendants = o.Descendants().ToList();
            Assert.AreEqual(89, descendants.Count);
        }

        [Fact]
        public void JTokenToString()
        {
            var json = @"{
  CPU: 'Intel',
  Drives: [
    'DVD read/writer',
    ""500 gigabyte hard drive""
  ]
}";

            var o = JObject.Parse(json);

            StringAssert.AreEqual(@"{
  ""CPU"": ""Intel"",
  ""Drives"": [
    ""DVD read/writer"",
    ""500 gigabyte hard drive""
  ]
}", o.ToString());

            var list = o.Value<JArray>("Drives");

            StringAssert.AreEqual(@"[
  ""DVD read/writer"",
  ""500 gigabyte hard drive""
]", list.ToString());

            var cpuProperty = o.Property("CPU");
            Assert.AreEqual(@"""CPU"": ""Intel""", cpuProperty.ToString());

            var drivesProperty = o.Property("Drives");
            StringAssert.AreEqual(@"""Drives"": [
  ""DVD read/writer"",
  ""500 gigabyte hard drive""
]", drivesProperty.ToString());
        }

        [Fact]
        public void JTokenToStringTypes()
        {
            var json = @"{""Color"":2,""Establised"":new Date(1264118400000),""Width"":1.1,""Employees"":999,""RoomsPerFloor"":[1,2,3,4,5,6,7,8,9],""Open"":false,""Symbol"":""@"",""Mottos"":[""Hello World"",""öäüÖÄÜ\\'{new Date(12345);}[222]_µ@²³~"",null,"" ""],""Cost"":100980.1,""Escape"":""\r\n\t\f\b?{\\r\\n\""'"",""product"":[{""Name"":""Rocket"",""ExpiryDate"":new Date(949532490000),""Price"":0},{""Name"":""Alien"",""ExpiryDate"":new Date(-62135596800000),""Price"":0}]}";

            var o = JObject.Parse(json);

            StringAssert.AreEqual(@"""Establised"": new Date(
  1264118400000
)", o.Property("Establised").ToString());
            StringAssert.AreEqual(@"new Date(
  1264118400000
)", o.Property("Establised").Value.ToString());
            Assert.AreEqual(@"""Width"": 1.1", o.Property("Width").ToString());
            Assert.AreEqual(@"1.1", ((JValue)o.Property("Width").Value).ToString(CultureInfo.InvariantCulture));
            Assert.AreEqual(@"""Open"": false", o.Property("Open").ToString());
            Assert.AreEqual(@"False", o.Property("Open").Value.ToString());

            json = @"[null,undefined]";

            var a = JArray.Parse(json);
            StringAssert.AreEqual(@"[
  null,
  undefined
]", a.ToString());
            Assert.AreEqual(@"", a.Children().ElementAt(0).ToString());
            Assert.AreEqual(@"", a.Children().ElementAt(1).ToString());
        }

        [Fact]
        public void CreateJTokenTree()
        {
            var o =
                new JObject(
                    new JProperty("Test1", "Test1Value"),
                    new JProperty("Test2", "Test2Value"),
                    new JProperty("Test3", "Test3Value"),
                    new JProperty("Test4", null)
                    );

            Assert.AreEqual(4, o.Properties().Count());

            StringAssert.AreEqual(@"{
  ""Test1"": ""Test1Value"",
  ""Test2"": ""Test2Value"",
  ""Test3"": ""Test3Value"",
  ""Test4"": null
}", o.ToString());

            var a =
                new JArray(
                    o,
                    new DateTime(2000, 10, 10, 0, 0, 0, DateTimeKind.Utc),
                    55,
                    new JArray(
                        "1",
                        2,
                        3.0,
                        new DateTime(4, 5, 6, 7, 8, 9, DateTimeKind.Utc)
                        ),
                    new JConstructor(
                        "ConstructorName",
                        "param1",
                        2,
                        3.0
                        )
                    );

            Assert.AreEqual(5, a.Count());
            StringAssert.AreEqual(@"[
  {
    ""Test1"": ""Test1Value"",
    ""Test2"": ""Test2Value"",
    ""Test3"": ""Test3Value"",
    ""Test4"": null
  },
  ""2000-10-10T00:00:00Z"",
  55,
  [
    ""1"",
    2,
    3.0,
    ""0004-05-06T07:08:09Z""
  ],
  new ConstructorName(
    ""param1"",
    2,
    3.0
  )
]", a.ToString());
        }

        private class Post
        {
            public string Title { get; set; }
            public string Description { get; set; }
            public string Link { get; set; }
            public IList<string> Categories { get; set; }
        }

        private List<Post> GetPosts()
        {
            return new List<Post>()
            {
                new Post()
                {
                    Title = "LINQ to JSON beta",
                    Description = "Announcing LINQ to JSON",
                    Link = "http://james.newtonking.com/projects/json-net.aspx",
                    Categories = new List<string>() { "Json.NET", "LINQ" }
                },
                new Post()
                {
                    Title = "Json.NET 1.3 + New license + Now on CodePlex",
                    Description = "Announcing the release of Json.NET 1.3, the MIT license and being available on CodePlex",
                    Link = "http://james.newtonking.com/projects/json-net.aspx",
                    Categories = new List<string>() { "Json.NET", "CodePlex" }
                }
            };
        }

        [Fact]
        public void FromObjectExample()
        {
            var p = new Post
            {
                Title = "How to use FromObject",
                Categories = new[] { "LINQ to JSON" }
            };

            // serialize Post to JSON then parse JSON – SLOW!
            //JObject o = JObject.Parse(JsonConvert.SerializeObject(p));

            // create JObject directly from the Post
            var o = JObject.FromObject(p);

            o["Title"] = o["Title"] + " - Super effective!";

            var json = o.ToString();
            // {
            //   "Title": "How to use FromObject - It's super effective!",
            //   "Categories": [
            //     "LINQ to JSON"
            //   ]
            // }

            StringAssert.AreEqual(@"{
  ""Title"": ""How to use FromObject - Super effective!"",
  ""Description"": null,
  ""Link"": null,
  ""Categories"": [
    ""LINQ to JSON""
  ]
}", json);
        }

        [Fact]
        public void QueryingExample()
        {
            var posts = JArray.Parse(@"[
              {
                'Title': 'JSON Serializer Basics',
                'Date': '2013-12-21T00:00:00',
                'Categories': []
              },
              {
                'Title': 'Querying LINQ to JSON',
                'Date': '2014-06-03T00:00:00',
                'Categories': [
                  'LINQ to JSON'
                ]
              }
            ]");

            var serializerBasics = posts
                .Single(p => (string)p["Title"] == "JSON Serializer Basics");
            // JSON Serializer Basics

            IList<JToken> since2012 = posts
                .Where(p => (DateTime)p["Date"] > new DateTime(2012, 1, 1)).ToList();
            // JSON Serializer Basics
            // Querying LINQ to JSON

            IList<JToken> linqToJson = posts
                .Where(p => p["Categories"].Any(c => (string)c == "LINQ to JSON")).ToList();
            // Querying LINQ to JSON

            Assert.IsNotNull(serializerBasics);
            Assert.AreEqual(2, since2012.Count);
            Assert.AreEqual(1, linqToJson.Count);
        }

        [Fact]
        public void CreateJTokenTreeNested()
        {
            var posts = GetPosts();

            var rss =
                new JObject(
                    new JProperty("channel",
                        new JObject(
                            new JProperty("title", "James Newton-King"),
                            new JProperty("link", "http://james.newtonking.com"),
                            new JProperty("description", "James Newton-King's blog."),
                            new JProperty("item",
                                new JArray(
                                    from p in posts
                                    orderby p.Title
                                    select new JObject(
                                        new JProperty("title", p.Title),
                                        new JProperty("description", p.Description),
                                        new JProperty("link", p.Link),
                                        new JProperty("category",
                                            new JArray(
                                                from c in p.Categories
                                                select new JValue(c)))))))));

            StringAssert.AreEqual(@"{
  ""channel"": {
    ""title"": ""James Newton-King"",
    ""link"": ""http://james.newtonking.com"",
    ""description"": ""James Newton-King's blog."",
    ""item"": [
      {
        ""title"": ""Json.NET 1.3 + New license + Now on CodePlex"",
        ""description"": ""Announcing the release of Json.NET 1.3, the MIT license and being available on CodePlex"",
        ""link"": ""http://james.newtonking.com/projects/json-net.aspx"",
        ""category"": [
          ""Json.NET"",
          ""CodePlex""
        ]
      },
      {
        ""title"": ""LINQ to JSON beta"",
        ""description"": ""Announcing LINQ to JSON"",
        ""link"": ""http://james.newtonking.com/projects/json-net.aspx"",
        ""category"": [
          ""Json.NET"",
          ""LINQ""
        ]
      }
    ]
  }
}", rss.ToString());

            var postTitles =
                from p in rss["channel"]["item"]
                select p.Value<string>("title");

            Assert.AreEqual("Json.NET 1.3 + New license + Now on CodePlex", postTitles.ElementAt(0));
            Assert.AreEqual("LINQ to JSON beta", postTitles.ElementAt(1));

            var categories =
                from c in rss["channel"]["item"].Children()["category"].Values<string>()
                group c by c
                into g
                orderby g.Count() descending
                select new { Category = g.Key, Count = g.Count() };

            Assert.AreEqual("Json.NET", categories.ElementAt(0).Category);
            Assert.AreEqual(2, categories.ElementAt(0).Count);
            Assert.AreEqual("CodePlex", categories.ElementAt(1).Category);
            Assert.AreEqual(1, categories.ElementAt(1).Count);
            Assert.AreEqual("LINQ", categories.ElementAt(2).Category);
            Assert.AreEqual(1, categories.ElementAt(2).Count);
        }

        [Fact]
        public void BasicQuerying()
        {
            var json = @"{
                        ""channel"": {
                          ""title"": ""James Newton-King"",
                          ""link"": ""http://james.newtonking.com"",
                          ""description"": ""James Newton-King's blog."",
                          ""item"": [
                            {
                              ""title"": ""Json.NET 1.3 + New license + Now on CodePlex"",
                              ""description"": ""Announcing the release of Json.NET 1.3, the MIT license and being available on CodePlex"",
                              ""link"": ""http://james.newtonking.com/projects/json-net.aspx"",
                              ""category"": [
                                ""Json.NET"",
                                ""CodePlex""
                              ]
                            },
                            {
                              ""title"": ""LINQ to JSON beta"",
                              ""description"": ""Announcing LINQ to JSON"",
                              ""link"": ""http://james.newtonking.com/projects/json-net.aspx"",
                              ""category"": [
                                ""Json.NET"",
                                ""LINQ""
                              ]
                            }
                          ]
                        }
                      }";

            var o = JObject.Parse(json);

            Assert.AreEqual(null, o["purple"]);
            Assert.AreEqual(null, o.Value<string>("purple"));

            CustomAssert.IsInstanceOfType(typeof(JArray), o["channel"]["item"]);

            Assert.AreEqual(2, o["channel"]["item"].Children()["title"].Count());
            Assert.AreEqual(0, o["channel"]["item"].Children()["monkey"].Count());

            Assert.AreEqual("Json.NET 1.3 + New license + Now on CodePlex", (string)o["channel"]["item"][0]["title"]);

            CollectionAssert.AreEqual(new string[] { "Json.NET 1.3 + New license + Now on CodePlex", "LINQ to JSON beta" }, o["channel"]["item"].Children().Values<string>("title").ToArray());
        }

        [Fact]
        public void JObjectIntIndex()
        {
            ExceptionAssert.Throws<ArgumentException>(() =>
            {
                var o = new JObject();
                Assert.AreEqual(null, o[0]);
            }, "Accessed JObject values with invalid key value: 0. Object property name expected.");
        }

        [Fact]
        public void JArrayStringIndex()
        {
            ExceptionAssert.Throws<ArgumentException>(() =>
            {
                var a = new JArray();
                Assert.AreEqual(null, a["purple"]);
            }, @"Accessed JArray values with invalid key value: ""purple"". Int32 array index expected.");
        }

        [Fact]
        public void JConstructorStringIndex()
        {
            ExceptionAssert.Throws<ArgumentException>(() =>
            {
                var c = new JConstructor("ConstructorValue");
                Assert.AreEqual(null, c["purple"]);
            }, @"Accessed JConstructor values with invalid key value: ""purple"". Argument position index expected.");
        }

        [Fact]
        public void ToStringJsonConverter()
        {
            var o =
                new JObject(
                    new JProperty("Test1", new DateTime(2000, 10, 15, 5, 5, 5, DateTimeKind.Utc)),
                    new JProperty("Test2", new DateTimeOffset(2000, 10, 15, 5, 5, 5, new TimeSpan(11, 11, 0))),
                    new JProperty("Test3", "Test3Value"),
                    new JProperty("Test4", null)
                    );

            var serializer = new JsonSerializer();
            serializer.Converters.Add(new JavaScriptDateTimeConverter());
            var sw = new StringWriter();
            JsonWriter writer = new JsonTextWriter(sw);
            writer.Formatting = Formatting.Indented;
            serializer.Serialize(writer, o);

            var json = sw.ToString();

            StringAssert.AreEqual(@"{
  ""Test1"": new Date(
    971586305000
  ),
  ""Test2"": new Date(
    971546045000
  ),
  ""Test3"": ""Test3Value"",
  ""Test4"": null
}", json);
        }

        [Fact]
        public void DateTimeOffset()
        {
            var testDates = new List<DateTimeOffset>
            {
                new DateTimeOffset(new DateTime(100, 1, 1, 1, 1, 1, DateTimeKind.Utc)),
                new DateTimeOffset(2000, 1, 1, 1, 1, 1, TimeSpan.Zero),
                new DateTimeOffset(2000, 1, 1, 1, 1, 1, TimeSpan.FromHours(13)),
                new DateTimeOffset(2000, 1, 1, 1, 1, 1, TimeSpan.FromHours(-3.5)),
            };

            var jsonSerializer = new JsonSerializer();

            JTokenWriter jsonWriter;
            using (jsonWriter = new JTokenWriter())
            {
                jsonSerializer.Serialize(jsonWriter, testDates);
            }

            Assert.AreEqual(4, jsonWriter.Token.Children().Count());
        }

        [Fact]
        public void FromObject()
        {
            var posts = GetPosts();

            var o = JObject.FromObject(new
            {
                channel = new
                {
                    title = "James Newton-King",
                    link = "http://james.newtonking.com",
                    description = "James Newton-King's blog.",
                    item =
                        from p in posts
                        orderby p.Title
                        select new
                        {
                            title = p.Title,
                            description = p.Description,
                            link = p.Link,
                            category = p.Categories
                        }
                }
            });

            StringAssert.AreEqual(@"{
  ""channel"": {
    ""title"": ""James Newton-King"",
    ""link"": ""http://james.newtonking.com"",
    ""description"": ""James Newton-King's blog."",
    ""item"": [
      {
        ""title"": ""Json.NET 1.3 + New license + Now on CodePlex"",
        ""description"": ""Announcing the release of Json.NET 1.3, the MIT license and being available on CodePlex"",
        ""link"": ""http://james.newtonking.com/projects/json-net.aspx"",
        ""category"": [
          ""Json.NET"",
          ""CodePlex""
        ]
      },
      {
        ""title"": ""LINQ to JSON beta"",
        ""description"": ""Announcing LINQ to JSON"",
        ""link"": ""http://james.newtonking.com/projects/json-net.aspx"",
        ""category"": [
          ""Json.NET"",
          ""LINQ""
        ]
      }
    ]
  }
}", o.ToString());

            CustomAssert.IsInstanceOfType(typeof(JObject), o);
            CustomAssert.IsInstanceOfType(typeof(JObject), o["channel"]);
            Assert.AreEqual("James Newton-King", (string)o["channel"]["title"]);
            Assert.AreEqual(2, o["channel"]["item"].Children().Count());

            var a = JArray.FromObject(new List<int>() { 0, 1, 2, 3, 4 });
            CustomAssert.IsInstanceOfType(typeof(JArray), a);
            Assert.AreEqual(5, a.Count());
        }

        [Fact]
        public void FromAnonDictionary()
        {
            var posts = GetPosts();

            var o = JObject.FromObject(new
            {
                channel = new Dictionary<string, object>
                {
                    { "title", "James Newton-King" },
                    { "link", "http://james.newtonking.com" },
                    { "description", "James Newton-King's blog." },
                    {
                        "item",
                        (from p in posts
                            orderby p.Title
                            select new
                            {
                                title = p.Title,
                                description = p.Description,
                                link = p.Link,
                                category = p.Categories
                            })
                    }
                }
            });

            StringAssert.AreEqual(@"{
  ""channel"": {
    ""title"": ""James Newton-King"",
    ""link"": ""http://james.newtonking.com"",
    ""description"": ""James Newton-King's blog."",
    ""item"": [
      {
        ""title"": ""Json.NET 1.3 + New license + Now on CodePlex"",
        ""description"": ""Announcing the release of Json.NET 1.3, the MIT license and being available on CodePlex"",
        ""link"": ""http://james.newtonking.com/projects/json-net.aspx"",
        ""category"": [
          ""Json.NET"",
          ""CodePlex""
        ]
      },
      {
        ""title"": ""LINQ to JSON beta"",
        ""description"": ""Announcing LINQ to JSON"",
        ""link"": ""http://james.newtonking.com/projects/json-net.aspx"",
        ""category"": [
          ""Json.NET"",
          ""LINQ""
        ]
      }
    ]
  }
}", o.ToString());

            CustomAssert.IsInstanceOfType(typeof(JObject), o);
            CustomAssert.IsInstanceOfType(typeof(JObject), o["channel"]);
            Assert.AreEqual("James Newton-King", (string)o["channel"]["title"]);
            Assert.AreEqual(2, o["channel"]["item"].Children().Count());

            var a = JArray.FromObject(new List<int>() { 0, 1, 2, 3, 4 });
            CustomAssert.IsInstanceOfType(typeof(JArray), a);
            Assert.AreEqual(5, a.Count());
        }

        [Fact]
        public void AsJEnumerable()
        {
            JObject o = null;
            IJEnumerable<JToken> enumerable = null;

            enumerable = o.AsJEnumerable();
            Assert.IsNull(enumerable);

            o =
                new JObject(
                    new JProperty("Test1", new DateTime(2000, 10, 15, 5, 5, 5, DateTimeKind.Utc)),
                    new JProperty("Test2", "Test2Value"),
                    new JProperty("Test3", null)
                    );

            enumerable = o.AsJEnumerable();
            Assert.IsNotNull(enumerable);
            Assert.AreEqual(o, enumerable);

            var d = enumerable["Test1"].Value<DateTime>();

            Assert.AreEqual(new DateTime(2000, 10, 15, 5, 5, 5, DateTimeKind.Utc), d);
        }

        [Fact]
        public void CovariantIJEnumerable()
        {
            IEnumerable<JObject> o = new[]
            {
                JObject.FromObject(new { First = 1, Second = 2 }),
                JObject.FromObject(new { First = 1, Second = 2 })
            };

            IJEnumerable<JToken> values = o.Properties();
            Assert.AreEqual(4, values.Count());
        }

        [Fact]
        public void LinqCast()
        {
            JToken olist = JArray.Parse("[12,55]");

            var list1 = olist.AsEnumerable().Values<int>().ToList();

            Assert.AreEqual(12, list1[0]);
            Assert.AreEqual(55, list1[1]);
        }

        [Fact]
        public void ChildrenExtension()
        {
            var json = @"[
                        {
                          ""title"": ""James Newton-King"",
                          ""link"": ""http://james.newtonking.com"",
                          ""description"": ""James Newton-King's blog."",
                          ""item"": [
                            {
                              ""title"": ""Json.NET 1.3 + New license + Now on CodePlex"",
                              ""description"": ""Announcing the release of Json.NET 1.3, the MIT license and being available on CodePlex"",
                              ""link"": ""http://james.newtonking.com/projects/json-net.aspx"",
                              ""category"": [
                                ""Json.NET"",
                                ""CodePlex""
                              ]
                            },
                            {
                              ""title"": ""LINQ to JSON beta"",
                              ""description"": ""Announcing LINQ to JSON"",
                              ""link"": ""http://james.newtonking.com/projects/json-net.aspx"",
                              ""category"": [
                                ""Json.NET"",
                                ""LINQ""
                              ]
                            }
                          ]
                        },
                        {
                          ""title"": ""James Newton-King"",
                          ""link"": ""http://james.newtonking.com"",
                          ""description"": ""James Newton-King's blog."",
                          ""item"": [
                            {
                              ""title"": ""Json.NET 1.3 + New license + Now on CodePlex"",
                              ""description"": ""Announcing the release of Json.NET 1.3, the MIT license and being available on CodePlex"",
                              ""link"": ""http://james.newtonking.com/projects/json-net.aspx"",
                              ""category"": [
                                ""Json.NET"",
                                ""CodePlex""
                              ]
                            },
                            {
                              ""title"": ""LINQ to JSON beta"",
                              ""description"": ""Announcing LINQ to JSON"",
                              ""link"": ""http://james.newtonking.com/projects/json-net.aspx"",
                              ""category"": [
                                ""Json.NET"",
                                ""LINQ""
                              ]
                            }
                          ]
                        }
                      ]";

            var o = JArray.Parse(json);

            Assert.AreEqual(4, o.Children()["item"].Children()["title"].Count());
            CollectionAssert.AreEqual(new string[]
            {
                "Json.NET 1.3 + New license + Now on CodePlex",
                "LINQ to JSON beta",
                "Json.NET 1.3 + New license + Now on CodePlex",
                "LINQ to JSON beta"
            },
                o.Children()["item"].Children()["title"].Values<string>().ToArray());
        }

        [Fact]
        public void UriGuidTimeSpanTestClassEmptyTest()
        {
            var c1 = new UriGuidTimeSpanTestClass();
            var o = JObject.FromObject(c1);

            StringAssert.AreEqual(@"{
  ""Guid"": ""00000000-0000-0000-0000-000000000000"",
  ""NullableGuid"": null,
  ""TimeSpan"": ""00:00:00"",
  ""NullableTimeSpan"": null,
  ""Uri"": null
}", o.ToString());

            var c2 = o.ToObject<UriGuidTimeSpanTestClass>();
            Assert.AreEqual(c1.Guid, c2.Guid);
            Assert.AreEqual(c1.NullableGuid, c2.NullableGuid);
            Assert.AreEqual(c1.TimeSpan, c2.TimeSpan);
            Assert.AreEqual(c1.NullableTimeSpan, c2.NullableTimeSpan);
            Assert.AreEqual(c1.Uri, c2.Uri);
        }

        [Fact]
        public void UriGuidTimeSpanTestClassValuesTest()
        {
            var c1 = new UriGuidTimeSpanTestClass
            {
                Guid = new Guid("1924129C-F7E0-40F3-9607-9939C531395A"),
                NullableGuid = new Guid("9E9F3ADF-E017-4F72-91E0-617EBE85967D"),
                TimeSpan = TimeSpan.FromDays(1),
                NullableTimeSpan = TimeSpan.FromHours(1),
                Uri = new Uri("http://testuri.com")
            };
            var o = JObject.FromObject(c1);

            StringAssert.AreEqual(@"{
  ""Guid"": ""1924129c-f7e0-40f3-9607-9939c531395a"",
  ""NullableGuid"": ""9e9f3adf-e017-4f72-91e0-617ebe85967d"",
  ""TimeSpan"": ""1.00:00:00"",
  ""NullableTimeSpan"": ""01:00:00"",
  ""Uri"": ""http://testuri.com""
}", o.ToString());

            var c2 = o.ToObject<UriGuidTimeSpanTestClass>();
            Assert.AreEqual(c1.Guid, c2.Guid);
            Assert.AreEqual(c1.NullableGuid, c2.NullableGuid);
            Assert.AreEqual(c1.TimeSpan, c2.TimeSpan);
            Assert.AreEqual(c1.NullableTimeSpan, c2.NullableTimeSpan);
            Assert.AreEqual(c1.Uri, c2.Uri);

            var j = JsonConvert.SerializeObject(c1, Formatting.Indented);

            StringAssert.AreEqual(j, o.ToString());
        }

        [Fact]
        public void ParseWithPrecendingComments()
        {
            var json = @"/* blah */ {'hi':'hi!'}";
            var o = JObject.Parse(json);
            Assert.AreEqual("hi!", (string)o["hi"]);

            json = @"/* blah */ ['hi!']";
            var a = JArray.Parse(json);
            Assert.AreEqual("hi!", (string)a[0]);
        }

        [Fact]
        public void ExceptionFromOverloadWithJValue()
        {
            dynamic name = new JValue("Matthew Doig");

            IDictionary<string, string> users = new Dictionary<string, string>();

            // unfortunatly there doesn't appear to be a way around this
            ExceptionAssert.Throws<Microsoft.CSharp.RuntimeBinder.RuntimeBinderException>(() =>
            {
                users.Add("name2", name);

                Assert.AreEqual(users["name2"], "Matthew Doig");
            }, "The best overloaded method match for 'System.Collections.Generic.IDictionary<string,string>.Add(string, string)' has some invalid arguments");
        }

        [JsonConverter(typeof(StringEnumConverter))]
        public enum FooBar
        {
            [EnumMember(Value = "SOME_VALUE")]
            SomeValue,

            [EnumMember(Value = "SOME_OTHER_VALUE")]
            SomeOtherValue
        }

        public class MyObject
        {
            public FooBar FooBar { get; set; }
        }

        [Fact]
        public void ToObject_Enum_Converter()
        {
            var o = JObject.Parse("{'FooBar':'SOME_OTHER_VALUE'}");

            var e = o["FooBar"].ToObject<FooBar>();
            Assert.AreEqual(FooBar.SomeOtherValue, e);
        }

        public enum FooBarNoEnum
        {
            [EnumMember(Value = "SOME_VALUE")]
            SomeValue,

            [EnumMember(Value = "SOME_OTHER_VALUE")]
            SomeOtherValue
        }

        public class MyObjectNoEnum
        {
            public FooBarNoEnum FooBarNoEnum { get; set; }
        }

        [Fact]
        public void ToObject_Enum_NoConverter()
        {
            var o = JObject.Parse("{'FooBarNoEnum':'SOME_OTHER_VALUE'}");

            var e = o["FooBarNoEnum"].ToObject<FooBarNoEnum>();
            Assert.AreEqual(FooBarNoEnum.SomeOtherValue, e);
        }

        [Fact]
        public void SerializeWithNoRedundentIdPropertiesTest()
        {
            var dic1 = new Dictionary<string, object>();
            var dic2 = new Dictionary<string, object>();
            var dic3 = new Dictionary<string, object>();
            var list1 = new List<object>();
            var list2 = new List<object>();

            dic1.Add("list1", list1);
            dic1.Add("list2", list2);
            dic1.Add("dic1", dic1);
            dic1.Add("dic2", dic2);
            dic1.Add("dic3", dic3);
            dic1.Add("integer", 12345);

            list1.Add("A string!");
            list1.Add(dic1);
            list1.Add(new List<object>());

            dic3.Add("dic3", dic3);

            var json = SerializeWithNoRedundentIdProperties(dic1);

            StringAssert.AreEqual(@"{
  ""$id"": ""1"",
  ""list1"": [
    ""A string!"",
    {
      ""$ref"": ""1""
    },
    []
  ],
  ""list2"": [],
  ""dic1"": {
    ""$ref"": ""1""
  },
  ""dic2"": {},
  ""dic3"": {
    ""$id"": ""3"",
    ""dic3"": {
      ""$ref"": ""3""
    }
  },
  ""integer"": 12345
}", json);
        }

        private static string SerializeWithNoRedundentIdProperties(object o)
        {
            var writer = new JTokenWriter();
            var serializer = JsonSerializer.Create(new JsonSerializerSettings
            {
                Formatting = Formatting.Indented,
                PreserveReferencesHandling = PreserveReferencesHandling.Objects
            });
            serializer.Serialize(writer, o);

            var t = writer.Token;

            if (t is JContainer)
            {
                var c = t as JContainer;

                // find all the $id properties in the JSON
                IList<JProperty> ids = c.Descendants().OfType<JProperty>().Where(d => d.Name == "$id").ToList();

                if (ids.Count > 0)
                {
                    // find all the $ref properties in the JSON
                    IList<JProperty> refs = c.Descendants().OfType<JProperty>().Where(d => d.Name == "$ref").ToList();

                    foreach (var idProperty in ids)
                    {
                        // check whether the $id property is used by a $ref
                        var idUsed = refs.Any(r => idProperty.Value.ToString() == r.Value.ToString());

                        if (!idUsed)
                        {
                            // remove unused $id
                            idProperty.Remove();
                        }
                    }
                }
            }

            var json = t.ToString();
            return json;
        }

        [Fact]
        public void HashCodeTests()
        {
            var o1 = new JObject
            {
                ["prop"] = 1
            };
            var o2 = new JObject
            {
                ["prop"] = 1
            };

            Assert.IsFalse(ReferenceEquals(o1, o2));
            Assert.IsFalse(Equals(o1, o2));
            Assert.IsFalse(o1.GetHashCode() == o2.GetHashCode());
            Assert.IsTrue(o1.GetDeepHashCode() == o2.GetDeepHashCode());
            Assert.IsTrue(JToken.DeepEquals(o1, o2));

            var a1 = new JArray
            {
                1
            };
            var a2 = new JArray
            {
                1
            };

            Assert.IsFalse(ReferenceEquals(a1, a2));
            Assert.IsFalse(Equals(a1, a2));
            Assert.IsFalse(a1.GetHashCode() == a2.GetHashCode());
            Assert.IsTrue(a1.GetDeepHashCode() == a2.GetDeepHashCode());
            Assert.IsTrue(JToken.DeepEquals(a1, a2));
        }
    }
}