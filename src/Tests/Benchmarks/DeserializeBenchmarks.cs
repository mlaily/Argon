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

using BenchmarkDotNet.Attributes;
using Argon.Tests.TestObjects;

namespace Argon.Tests.Benchmarks;

public class DeserializeBenchmarks
{
    static readonly string LargeJsonText;
    static readonly string FloatArrayJson;

    static DeserializeBenchmarks()
    {
        LargeJsonText = System.IO.File.ReadAllText("large.json");

        FloatArrayJson = new JArray(Enumerable.Range(0, 5000).Select(i => i * 1.1m)).ToString(Formatting.None);
    }

    [Benchmark]
    public IList<RootObject> DeserializeLargeJsonText()
    {
        return JsonConvert.DeserializeObject<IList<RootObject>>(LargeJsonText);
    }

    [Benchmark]
    public IList<RootObject> DeserializeLargeJsonFile()
    {
        using var jsonFile = System.IO.File.OpenText("large.json");
        using var jsonTextReader = new JsonTextReader(jsonFile);
        var serializer = new JsonSerializer();
        return serializer.Deserialize<IList<RootObject>>(jsonTextReader);
    }

    [Benchmark]
    public IList<double> DeserializeDoubleList()
    {
        return JsonConvert.DeserializeObject<IList<double>>(FloatArrayJson);
    }

    [Benchmark]
    public IList<decimal> DeserializeDecimalList()
    {
        return JsonConvert.DeserializeObject<IList<decimal>>(FloatArrayJson);
    }
}