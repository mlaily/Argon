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

using Argon.Bson;
using Argon.Linq;
using System;
using System.IO;
using Xunit;
using Test = Xunit.FactAttribute;
using Assert = Argon.Tests.XUnitAssert;


namespace Argon.Tests.Documentation.Samples.Linq
{
    [TestFixture]
    public class ReadJTokenFromBson : TestFixtureBase
    {
#pragma warning disable 618
        [Fact]
        public void Example()
        {
            #region Usage
            var data = Convert.FromBase64String("KQAAAAJuYW1lMQAHAAAAdmFsdWUxAAJuYW1lMgAHAAAAdmFsdWUyAAA=");
            var ms = new MemoryStream(data);

            JObject o;
            using (var reader = new BsonReader(ms))
            {
                o = (JObject)JToken.ReadFrom(reader);
            }

            var value = (string)o["name1"];

            Console.WriteLine(value);
            // value1
            #endregion

            Assert.AreEqual("value1", value);
        }
#pragma warning restore 618
    }
}