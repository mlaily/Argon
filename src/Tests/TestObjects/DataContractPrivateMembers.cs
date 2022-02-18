#region License
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

namespace Argon.Tests.TestObjects;

[DataContract]
public class DataContractPrivateMembers
{
    public DataContractPrivateMembers()
    {
    }

    public DataContractPrivateMembers(string name, int age, int rank, string title)
    {
        _name = name;
        Age = age;
        Rank = rank;
        Title = title;
    }

    [DataMember]
    string _name;

    [DataMember(Name = "_age")]
    int Age { get; set; }

    [JsonProperty]
    int Rank { get; set; }

    [JsonProperty(PropertyName = "JsonTitle")]
    [DataMember(Name = "DataTitle")]
    string Title { get; set; }

    public string NotIncluded { get; set; }

    public override string ToString()
    {
        return $"_name: {_name}, _age: {Age}, Rank: {Rank}, JsonTitle: {Title}";
    }
}