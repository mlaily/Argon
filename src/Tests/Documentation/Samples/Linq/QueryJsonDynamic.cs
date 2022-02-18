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

#if !NET5_0_OR_GREATER
using Xunit;

namespace Argon.Tests.Documentation.Samples.Linq;

public class QueryJsonDynamic : TestFixtureBase
{
    [Fact]
    public void Example()
    {
        #region Usage
        var json = @"[
          {
            'Title': 'Json.NET is awesome!',
            'Author': {
              'Name': 'James Newton-King',
              'Twitter': '@JamesNK',
              'Picture': '/jamesnk.png'
            },
            'Date': '2013-01-23T19:30:00',
            'BodyHtml': '&lt;h3&gt;Title!&lt;/h3&gt;\r\n&lt;p&gt;Content!&lt;/p&gt;'
          }
        ]";

        dynamic blogPosts = JArray.Parse(json);

        var blogPost = blogPosts[0];

        string title = blogPost.Title;

        Console.WriteLine(title);
        // Json.NET is awesome!

        string author = blogPost.Author.Name;

        Console.WriteLine(author);
        // James Newton-King

        DateTime postDate = blogPost.Date;

        Console.WriteLine(postDate);
        // 23/01/2013 7:30:00 p.m.
        #endregion

        Assert.Equal("Json.NET is awesome!", title);
    }
}

#endif