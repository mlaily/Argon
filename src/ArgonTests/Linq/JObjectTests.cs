// Copyright (c) 2007 James Newton-King. All rights reserved.
// Use of this source code is governed by The MIT License,
// as found in the license.md file.

using TestObjects;

public class JObjectTests : TestFixtureBase
{
    [Fact]
    public void EmbedJValueStringInNewJObject()
    {
        string s = null;
        var v = new JValue(s);
        dynamic o = JObject.FromObject(new
        {
            title = v
        });

        string output = o.ToString();

        XUnitAssert.AreEqualNormalized(
            """
            {
              "title": null
            }
            """,
            output);

        Assert.Equal(null, v.Value);
        Assert.Null((string) o.title);
    }

    [Fact]
    public void ReadWithSupportMultipleContent()
    {
        var json = @"{ 'name': 'Admin' }{ 'name': 'Publisher' }";

        IList<JObject> roles = new List<JObject>();

        var reader = new JsonTextReader(new StringReader(json));
        reader.SupportMultipleContent = true;

        while (true)
        {
            var role = (JObject) JToken.ReadFrom(reader);

            roles.Add(role);

            if (!reader.Read())
            {
                break;
            }
        }

        Assert.Equal(2, roles.Count);
        Assert.Equal("Admin", (string) roles[0]["name"]);
        Assert.Equal("Publisher", (string) roles[1]["name"]);
    }

    [Fact]
    public void JObjectWithComments()
    {
        var json = """
            { /*comment2*/
                    "Name": /*comment3*/ "Apple" /*comment4*/, /*comment5*/
                    "ExpiryDate": "\/Date(1230422400000)\/",
                    "Price": 3.99,
                    "Sizes": /*comment6*/ [ /*comment7*/
                      "Small", /*comment8*/
                      "Medium" /*comment9*/,
                      /*comment10*/ "Large"
                    /*comment11*/ ] /*comment12*/
                  } /*comment13*/
            """;

        var o = JToken.Parse(json);

        Assert.Equal("Apple", (string) o["Name"]);
    }

    [Fact]
    public void WritePropertyWithNoValue()
    {
        var o = new JObject
        {
            new JProperty("novalue")
        };

        XUnitAssert.AreEqualNormalized(
            """
            {
              "novalue": null
            }
            """,
            o.ToString());
    }

    [Fact]
    public void Keys()
    {
        var o = new JObject();
        var d = (IDictionary<string, JToken>) o;

        Assert.Equal(0, d.Keys.Count);

        o["value"] = true;

        Assert.Equal(1, d.Keys.Count);
    }

    [Fact]
    public void TryGetValue()
    {
        var o = new JObject
        {
            {
                "PropertyNameValue", new JValue(1)
            }
        };
        Assert.Equal(1, o.Children().Count());

        XUnitAssert.False(o.TryGetValue("sdf", out var t));
        Assert.Equal(null, t);

        XUnitAssert.True(o.TryGetValue("PropertyNameValue", out t));
        XUnitAssert.True(JToken.DeepEquals(new JValue(1), t));
    }

    [Fact]
    public void DictionaryItemShouldSet()
    {
        var o = new JObject
        {
            ["PropertyNameValue"] = new JValue(1)
        };
        Assert.Equal(1, o.Children().Count());

        XUnitAssert.True(o.TryGetValue("PropertyNameValue", out var t));
        XUnitAssert.True(JToken.DeepEquals(new JValue(1), t));

        o["PropertyNameValue"] = new JValue(2);
        Assert.Equal(1, o.Children().Count());

        XUnitAssert.True(o.TryGetValue("PropertyNameValue", out t));
        XUnitAssert.True(JToken.DeepEquals(new JValue(2), t));

        o["PropertyNameValue"] = null;
        Assert.Equal(1, o.Children().Count());

        XUnitAssert.True(o.TryGetValue("PropertyNameValue", out t));
        XUnitAssert.True(JToken.DeepEquals(JValue.CreateNull(), t));
    }

    [Fact]
    public void Remove()
    {
        var o = new JObject
        {
            {
                "PropertyNameValue", new JValue(1)
            }
        };
        Assert.Equal(1, o.Children().Count());

        XUnitAssert.False(o.Remove("sdf"));
        XUnitAssert.True(o.Remove("PropertyNameValue"));

        Assert.Equal(0, o.Children().Count());
    }

    [Fact]
    public void GenericCollectionRemove()
    {
        var v = new JValue(1);
        var o = new JObject
        {
            {
                "PropertyNameValue", v
            }
        };
        Assert.Equal(1, o.Children().Count());

        XUnitAssert.False(((ICollection<KeyValuePair<string, JToken>>) o).Remove(new("PropertyNameValue1", new JValue(1))));
        XUnitAssert.False(((ICollection<KeyValuePair<string, JToken>>) o).Remove(new("PropertyNameValue", new JValue(2))));
        XUnitAssert.False(((ICollection<KeyValuePair<string, JToken>>) o).Remove(new("PropertyNameValue", new JValue(1))));
        XUnitAssert.True(((ICollection<KeyValuePair<string, JToken>>) o).Remove(new("PropertyNameValue", v)));

        Assert.Equal(0, o.Children().Count());
    }

    [Fact]
    public void DuplicatePropertyNameShouldThrow() =>
        XUnitAssert.Throws<ArgumentException>(
            () =>
            {
                var o = new JObject
                {
                    {
                        "PropertyNameValue", null
                    },
                    {
                        "PropertyNameValue", null
                    }
                };
            },
            "Can not add property PropertyNameValue to Argon.JObject. Property with the same name already exists on object.");

    [Fact]
    public void GenericDictionaryAdd()
    {
        var o = new JObject
        {
            {
                "PropertyNameValue", new JValue(1)
            }
        };

        Assert.Equal(1, (int) o["PropertyNameValue"]);

        o.Add("PropertyNameValue1", null);
        Assert.Equal(null, ((JValue) o["PropertyNameValue1"]).Value);

        Assert.Equal(2, o.Children().Count());
    }

    [Fact]
    public void GenericCollectionAdd()
    {
        var o = new JObject();
        ((ICollection<KeyValuePair<string, JToken>>) o).Add(new("PropertyNameValue", new JValue(1)));

        Assert.Equal(1, (int) o["PropertyNameValue"]);
        Assert.Equal(1, o.Children().Count());
    }

    [Fact]
    public void GenericCollectionClear()
    {
        var o = new JObject
        {
            {
                "PropertyNameValue", new JValue(1)
            }
        };
        Assert.Equal(1, o.Children().Count());

        var p = (JProperty) o.Children().ElementAt(0);

        ((ICollection<KeyValuePair<string, JToken>>) o).Clear();
        Assert.Equal(0, o.Children().Count());

        Assert.Equal(null, p.Parent);
    }

    [Fact]
    public void GenericCollectionContains()
    {
        var v = new JValue(1);
        var o = new JObject
        {
            {
                "PropertyNameValue", v
            }
        };
        Assert.Equal(1, o.Children().Count());

        var contains = ((ICollection<KeyValuePair<string, JToken>>) o).Contains(new("PropertyNameValue", new JValue(1)));
        XUnitAssert.False(contains);

        contains = ((ICollection<KeyValuePair<string, JToken>>) o).Contains(new("PropertyNameValue", v));
        XUnitAssert.True(contains);

        contains = ((ICollection<KeyValuePair<string, JToken>>) o).Contains(new("PropertyNameValue", new JValue(2)));
        XUnitAssert.False(contains);

        contains = ((ICollection<KeyValuePair<string, JToken>>) o).Contains(new("PropertyNameValue1", new JValue(1)));
        XUnitAssert.False(contains);
    }

    [Fact]
    public void Contains()
    {
        var o = new JObject
        {
            {
                "PropertyNameValue", new JValue(1)
            }
        };
        Assert.Equal(1, o.Children().Count());

        var contains = o.ContainsKey("PropertyNameValue");
        XUnitAssert.True(contains);

        contains = o.ContainsKey("does not exist");
        XUnitAssert.False(contains);
    }

    [Fact]
    public void GenericDictionaryContains()
    {
        var o = new JObject
        {
            {
                "PropertyNameValue", new JValue(1)
            }
        };
        Assert.Equal(1, o.Children().Count());

        var contains = ((IDictionary<string, JToken>) o).ContainsKey("PropertyNameValue");
        XUnitAssert.True(contains);
    }

    [Fact]
    public void GenericCollectionCopyTo()
    {
        var o = new JObject
        {
            {
                "PropertyNameValue", new JValue(1)
            },
            {
                "PropertyNameValue2", new JValue(2)
            },
            {
                "PropertyNameValue3", new JValue(3)
            }
        };
        Assert.Equal(3, o.Children().Count());

        var a = new KeyValuePair<string, JToken>[5];

        ((ICollection<KeyValuePair<string, JToken>>) o).CopyTo(a, 1);

        Assert.Equal(default, a[0]);

        Assert.Equal("PropertyNameValue", a[1].Key);
        Assert.Equal(1, (int) a[1].Value);

        Assert.Equal("PropertyNameValue2", a[2].Key);
        Assert.Equal(2, (int) a[2].Value);

        Assert.Equal("PropertyNameValue3", a[3].Key);
        Assert.Equal(3, (int) a[3].Value);

        Assert.Equal(default, a[4]);
    }

    [Fact]
    public void GenericCollectionCopyToNegativeArrayIndexShouldThrow() =>
        XUnitAssert.Throws<ArgumentOutOfRangeException>(() =>
            {
                var o = new JObject();
                ((ICollection<KeyValuePair<string, JToken>>) o).CopyTo(new KeyValuePair<string, JToken>[1], -1);
            },
            """
                arrayIndex is less than 0.
                Parameter name: arrayIndex
                """,
            "arrayIndex is less than 0. (Parameter 'arrayIndex')");

    [Fact]
    public void GenericCollectionCopyToArrayIndexEqualGreaterToArrayLengthShouldThrow() =>
        XUnitAssert.Throws<ArgumentException>(
            () =>
            {
                var o = new JObject();
                ((ICollection<KeyValuePair<string, JToken>>) o).CopyTo(new KeyValuePair<string, JToken>[1], 1);
            },
            @"arrayIndex is equal to or greater than the length of array.");

    [Fact]
    public void GenericCollectionCopyToInsufficientArrayCapacity() =>
        XUnitAssert.Throws<ArgumentException>(
            () =>
            {
                var o = new JObject
                {
                    {
                        "PropertyNameValue", new JValue(1)
                    },
                    {
                        "PropertyNameValue2", new JValue(2)
                    },
                    {
                        "PropertyNameValue3", new JValue(3)
                    }
                };

                ((ICollection<KeyValuePair<string, JToken>>) o).CopyTo(new KeyValuePair<string, JToken>[3], 1);
            },
            @"The number of elements in the source JObject is greater than the available space from arrayIndex to the end of the destination array.");

    [Fact]
    public void FromObjectRaw()
    {
        var raw = new PersonRaw
        {
            FirstName = "FirstNameValue",
            RawContent = new("[1,2,3,4,5]"),
            LastName = "LastNameValue"
        };

        var o = JObject.FromObject(raw);

        Assert.Equal("FirstNameValue", (string) o["first_name"]);
        Assert.Equal(JTokenType.Raw, ((JValue) o["RawContent"]).Type);
        Assert.Equal("[1,2,3,4,5]", (string) o["RawContent"]);
        Assert.Equal("LastNameValue", (string) o["last_name"]);
    }

    [Fact]
    public void JTokenReader()
    {
        var raw = new PersonRaw
        {
            FirstName = "FirstNameValue",
            RawContent = new("[1,2,3,4,5]"),
            LastName = "LastNameValue"
        };

        var o = JObject.FromObject(raw);

        JsonReader reader = new JTokenReader(o);

        Assert.True(reader.Read());
        Assert.Equal(JsonToken.StartObject, reader.TokenType);

        Assert.True(reader.Read());
        Assert.Equal(JsonToken.PropertyName, reader.TokenType);

        Assert.True(reader.Read());
        Assert.Equal(JsonToken.String, reader.TokenType);

        Assert.True(reader.Read());
        Assert.Equal(JsonToken.PropertyName, reader.TokenType);

        Assert.True(reader.Read());
        Assert.Equal(JsonToken.Raw, reader.TokenType);

        Assert.True(reader.Read());
        Assert.Equal(JsonToken.PropertyName, reader.TokenType);

        Assert.True(reader.Read());
        Assert.Equal(JsonToken.String, reader.TokenType);

        Assert.True(reader.Read());
        Assert.Equal(JsonToken.EndObject, reader.TokenType);

        Assert.False(reader.Read());
    }

    [Fact]
    public void DeserializeFromRaw()
    {
        var raw = new PersonRaw
        {
            FirstName = "FirstNameValue",
            RawContent = new("[1,2,3,4,5]"),
            LastName = "LastNameValue"
        };

        var o = JObject.FromObject(raw);

        JsonReader reader = new JTokenReader(o);
        var serializer = new JsonSerializer();
        raw = (PersonRaw) serializer.Deserialize(reader, typeof(PersonRaw));

        Assert.Equal("FirstNameValue", raw.FirstName);
        Assert.Equal("LastNameValue", raw.LastName);
        Assert.Equal("[1,2,3,4,5]", raw.RawContent.Value);
    }

    [Fact]
    public void Parse_ShouldThrowOnUnexpectedToken() =>
        XUnitAssert.Throws<JsonReaderException>(
            () =>
            {
                var json = @"[""prop""]";
                JObject.Parse(json);
            },
            "Error reading JObject from JsonReader. Current JsonReader item is not an object: StartArray. Path '', line 1, position 1.");

    [Fact]
    public void GenericValueCast()
    {
        var json = @"{""foo"":true}";
        var o = (JObject) JsonConvert.DeserializeObject(json);
        var value = o.Value<bool?>("foo");
        XUnitAssert.True(value);

        json = @"{""foo"":null}";
        o = (JObject) JsonConvert.DeserializeObject(json);
        value = o.Value<bool?>("foo");
        Assert.Equal(null, value);
    }

    [Fact]
    public void Blog() =>
        XUnitAssert.Throws<JsonReaderException>(
            () => JObject.Parse(
                """
                {
                    "name": "James",
                    ]!#$THIS IS: BAD JSON![{}}}}]
                  }
                """),
            "Invalid property identifier character: ]. Path 'name', line 3, position 4.");

    [Fact]
    public void RawChildValues()
    {
        var o = new JObject
        {
            ["val1"] = new JRaw("1"),
            ["val2"] = new JRaw("1")
        };

        var json = o.ToString();

        XUnitAssert.AreEqualNormalized(
            """
            {
              "val1": 1,
              "val2": 1
            }
            """,
            json);
    }

    [Fact]
    public void Iterate()
    {
        var o = new JObject
        {
            {
                "PropertyNameValue1", new JValue(1)
            },
            {
                "PropertyNameValue2", new JValue(2)
            }
        };

        JToken t = o;

        var i = 1;
        foreach (JProperty property in t)
        {
            Assert.Equal($"PropertyNameValue{i}", property.Name);
            Assert.Equal(i, (int) property.Value);

            i++;
        }
    }

    [Fact]
    public void KeyValuePairIterate()
    {
        var o = new JObject
        {
            {
                "PropertyNameValue1", new JValue(1)
            },
            {
                "PropertyNameValue2", new JValue(2)
            }
        };

        var i = 1;
        foreach (var pair in o)
        {
            Assert.Equal($"PropertyNameValue{i}", pair.Key);
            Assert.Equal(i, (int) pair.Value);

            i++;
        }
    }

    [Fact]
    public void WriteObjectNullStringValue()
    {
        string s = null;
        var v = new JValue(s);
        Assert.Equal(null, v.Value);
        Assert.Equal(JTokenType.String, v.Type);

        var o = new JObject
        {
            ["title"] = v
        };

        var output = o.ToString();

        XUnitAssert.AreEqualNormalized(
            """
            {
              "title": null
            }
            """,
            output);
    }

    [Fact]
    public void Example()
    {
        var json = """
            {
                Name: 'Apple',
                Expiry: '2014-06-04T00:00:00Z',
                Price: 3.99,
                Sizes: [
                  'Small',
                  'Medium',
                  'Large'
                ]
            }
            """;

        var o = JObject.Parse(json);

        var name = (string) o["Name"];
        // Apple

        var sizes = (JArray) o["Sizes"];

        var smallest = (string) sizes[0];
        // Small

        Assert.Equal("Apple", name);
        Assert.Equal("Small", smallest);
    }

    [Fact]
    public void DeserializeClassManually()
    {
        var jsonText = """
            {
              "short":
              {
                "original":"http://www.foo.com/",
                "short":"krehqk",
                "error":
                {
                  "code":0,
                  "msg":"No action taken"
                }
              }
            }
            """;

        var json = JObject.Parse(jsonText);

        var shortie = new Shortie
        {
            Original = (string) json["short"]["original"],
            Short = (string) json["short"]["short"],
            Error = new()
            {
                Code = (int) json["short"]["error"]["code"],
                ErrorMessage = (string) json["short"]["error"]["msg"]
            }
        };

        Assert.Equal("http://www.foo.com/", shortie.Original);
        Assert.Equal("krehqk", shortie.Short);
        Assert.Equal(null, shortie.Shortened);
        Assert.Equal(0, shortie.Error.Code);
        Assert.Equal("No action taken", shortie.Error.ErrorMessage);
    }

    [Fact]
    public void JObjectContainingHtml()
    {
        var o = new JObject
        {
            ["rc"] = new JValue(200),
            ["m"] = new JValue(""),
            ["o"] = new JValue($@"<div class='s1'>{StringUtils.CarriageReturnLineFeed}</div>")
        };

        XUnitAssert.AreEqualNormalized(
            """
            {
              "rc": 200,
              "m": "",
              "o": "<div class='s1'>\r\n</div>"
            }
            """,
            o.ToString());
    }

    [Fact]
    public void ImplicitValueConversions()
    {
        var moss = new JObject
        {
            ["FirstName"] = new JValue("Maurice"),
            ["LastName"] = new JValue("Moss"),
            ["BirthDate"] = new JValue(new DateTime(1977, 12, 30)),
            ["Department"] = new JValue("IT"),
            ["JobTitle"] = new JValue("Support")
        };

        XUnitAssert.AreEqualNormalized(
            """
            {
              "FirstName": "Maurice",
              "LastName": "Moss",
              "BirthDate": "1977-12-30T00:00:00",
              "Department": "IT",
              "JobTitle": "Support"
            }
            """,
            moss.ToString());

        var jen = new JObject
        {
            ["FirstName"] = "Jen",
            ["LastName"] = "Barber",
            ["BirthDate"] = new DateTime(1978, 3, 15),
            ["Department"] = "IT",
            ["JobTitle"] = "Manager"
        };

        XUnitAssert.AreEqualNormalized(
            """
            {
              "FirstName": "Jen",
              "LastName": "Barber",
              "BirthDate": "1978-03-15T00:00:00",
              "Department": "IT",
              "JobTitle": "Manager"
            }
            """,
            jen.ToString());
    }

    [Fact]
    public void ReplaceJPropertyWithJPropertyWithSameName()
    {
        var p1 = new JProperty("Test1", 1);
        var p2 = new JProperty("Test2", "Two");

        var o = new JObject(p1, p2);
        IList<JToken> l = o;
        Assert.Equal(p1, l[0]);
        Assert.Equal(p2, l[1]);

        var p3 = new JProperty("Test1", "III");

        p1.Replace(p3);
        Assert.Equal(null, p1.Parent);
        Assert.Equal(l, p3.Parent);

        Assert.Equal(p3, l[0]);
        Assert.Equal(p2, l[1]);

        Assert.Equal(2, l.Count);
        Assert.Equal(2, o.Properties().Count());

        var p4 = new JProperty("Test4", "IV");

        p2.Replace(p4);
        Assert.Equal(null, p2.Parent);
        Assert.Equal(l, p4.Parent);

        Assert.Equal(p3, l[0]);
        Assert.Equal(p4, l[1]);
    }

    [Fact]
    public void IListContains()
    {
        var p = new JProperty("Test", 1);
        IList<JToken> l = new JObject(p);

        Assert.True(l.Contains(p));
        Assert.False(l.Contains(new JProperty("Test", 1)));
    }

    [Fact]
    public void IListIndexOf()
    {
        var p = new JProperty("Test", 1);
        IList<JToken> l = new JObject(p);

        Assert.Equal(0, l.IndexOf(p));
        Assert.Equal(-1, l.IndexOf(new JProperty("Test", 1)));
    }

    [Fact]
    public void IListClear()
    {
        var p = new JProperty("Test", 1);
        IList<JToken> l = new JObject(p);

        Assert.Equal(1, l.Count);

        l.Clear();

        Assert.Equal(0, l.Count);
    }

    [Fact]
    public void IListAdd()
    {
        var p1 = new JProperty("Test1", 1);
        var p2 = new JProperty("Test2", "Two");
        IList<JToken> l = new JObject(p1, p2);

        var p3 = new JProperty("Test3", "III");

        l.Add(p3);

        Assert.Equal(3, l.Count);
        Assert.Equal(p3, l[2]);
    }

    [Fact]
    public void IListAddBadToken() =>
        XUnitAssert.Throws<ArgumentException>(
            () =>
            {
                var p1 = new JProperty("Test1", 1);
                var p2 = new JProperty("Test2", "Two");
                IList<JToken> l = new JObject(p1, p2);

                l.Add(new JValue("Bad!"));
            },
            "Can not add Argon.JValue to Argon.JObject.");

    [Fact]
    public void IListAddBadValue() =>
        XUnitAssert.Throws<ArgumentException>(
            () =>
            {
                var p1 = new JProperty("Test1", 1);
                var p2 = new JProperty("Test2", "Two");
                IList<JToken> l = new JObject(p1, p2);

                l.Add("Bad!");
            },
            "Can not add Argon.JValue to Argon.JObject.");

    [Fact]
    public void IListAddPropertyWithExistingName() =>
        XUnitAssert.Throws<ArgumentException>(
            () =>
            {
                var p1 = new JProperty("Test1", 1);
                var p2 = new JProperty("Test2", "Two");
                IList<JToken> l = new JObject(p1, p2);

                var p3 = new JProperty("Test2", "II");

                l.Add(p3);
            },
            "Can not add property Test2 to Argon.JObject. Property with the same name already exists on object.");

    [Fact]
    public void IListRemove()
    {
        var p1 = new JProperty("Test1", 1);
        var p2 = new JProperty("Test2", "Two");
        IList<JToken> l = new JObject(p1, p2);

        var p3 = new JProperty("Test3", "III");

        // won't do anything
        l.Remove(p3);
        Assert.Equal(2, l.Count);

        l.Remove(p1);
        Assert.Equal(1, l.Count);
        Assert.False(l.Contains(p1));
        Assert.True(l.Contains(p2));

        l.Remove(p2);
        Assert.Equal(0, l.Count);
        Assert.False(l.Contains(p2));
        Assert.Equal(null, p2.Parent);
    }

    [Fact]
    public void IListRemoveAt()
    {
        var p1 = new JProperty("Test1", 1);
        var p2 = new JProperty("Test2", "Two");
        IList<JToken> l = new JObject(p1, p2);

        // won't do anything
        l.RemoveAt(0);

        l.Remove(p1);
        Assert.Equal(1, l.Count);

        l.Remove(p2);
        Assert.Equal(0, l.Count);
    }

    [Fact]
    public void IListInsert()
    {
        var p1 = new JProperty("Test1", 1);
        var p2 = new JProperty("Test2", "Two");
        IList<JToken> l = new JObject(p1, p2);

        var p3 = new JProperty("Test3", "III");

        l.Insert(1, p3);
        Assert.Equal(l, p3.Parent);

        Assert.Equal(p1, l[0]);
        Assert.Equal(p3, l[1]);
        Assert.Equal(p2, l[2]);
    }

    [Fact]
    public void IListIsReadOnly()
    {
        IList<JToken> l = new JObject();
        Assert.False(l.IsReadOnly);
    }

    [Fact]
    public void IListSetItem()
    {
        var p1 = new JProperty("Test1", 1);
        var p2 = new JProperty("Test2", "Two");
        IList<JToken> l = new JObject(p1, p2);

        var p3 = new JProperty("Test3", "III");

        l[0] = p3;

        Assert.Equal(p3, l[0]);
        Assert.Equal(p2, l[1]);
    }

    [Fact]
    public void IListSetItemAlreadyExists() =>
        XUnitAssert.Throws<ArgumentException>(
            () =>
            {
                var p1 = new JProperty("Test1", 1);
                var p2 = new JProperty("Test2", "Two");
                IList<JToken> l = new JObject(p1, p2);

                var p3 = new JProperty("Test3", "III");

                l[0] = p3;
                l[1] = p3;
            },
            "Can not add property Test3 to Argon.JObject. Property with the same name already exists on object.");

    [Fact]
    public void IListSetItemInvalid() =>
        XUnitAssert.Throws<ArgumentException>(
            () =>
            {
                var p1 = new JProperty("Test1", 1);
                var p2 = new JProperty("Test2", "Two");
                IList<JToken> l = new JObject(p1, p2);

                l[0] = new JValue(true);
            },
            @"Can not add Argon.JValue to Argon.JObject.");

    [Fact]
    public void GenericListJTokenContains()
    {
        var p = new JProperty("Test", 1);
        IList<JToken> l = new JObject(p);

        Assert.True(l.Contains(p));
        Assert.False(l.Contains(new JProperty("Test", 1)));
    }

    [Fact]
    public void GenericListJTokenIndexOf()
    {
        var p = new JProperty("Test", 1);
        IList<JToken> l = new JObject(p);

        Assert.Equal(0, l.IndexOf(p));
        Assert.Equal(-1, l.IndexOf(new JProperty("Test", 1)));
    }

    [Fact]
    public void GenericListJTokenClear()
    {
        var p = new JProperty("Test", 1);
        IList<JToken> l = new JObject(p);

        Assert.Equal(1, l.Count);

        l.Clear();

        Assert.Equal(0, l.Count);
    }

    [Fact]
    public void GenericListJTokenCopyTo()
    {
        var p1 = new JProperty("Test1", 1);
        var p2 = new JProperty("Test2", "Two");
        IList<JToken> l = new JObject(p1, p2);

        var a = new JToken[l.Count];

        l.CopyTo(a, 0);

        Assert.Equal(p1, a[0]);
        Assert.Equal(p2, a[1]);
    }

    [Fact]
    public void GenericListJTokenAdd()
    {
        var p1 = new JProperty("Test1", 1);
        var p2 = new JProperty("Test2", "Two");
        IList<JToken> l = new JObject(p1, p2);

        var p3 = new JProperty("Test3", "III");

        l.Add(p3);

        Assert.Equal(3, l.Count);
        Assert.Equal(p3, l[2]);
    }

    [Fact]
    public void GenericListJTokenAddBadToken() =>
        XUnitAssert.Throws<ArgumentException>(
            () =>
            {
                var p1 = new JProperty("Test1", 1);
                var p2 = new JProperty("Test2", "Two");
                IList<JToken> l = new JObject(p1, p2);

                l.Add(new JValue("Bad!"));
            },
            "Can not add Argon.JValue to Argon.JObject.");

    [Fact]
    public void GenericListJTokenAddBadValue() =>
        XUnitAssert.Throws<ArgumentException>(
            () =>
            {
                var p1 = new JProperty("Test1", 1);
                var p2 = new JProperty("Test2", "Two");
                IList<JToken> l = new JObject(p1, p2);

                // string is implicitly converted to JValue
                l.Add("Bad!");
            },
            "Can not add Argon.JValue to Argon.JObject.");

    [Fact]
    public void GenericListJTokenAddPropertyWithExistingName() =>
        XUnitAssert.Throws<ArgumentException>(
            () =>
            {
                var p1 = new JProperty("Test1", 1);
                var p2 = new JProperty("Test2", "Two");
                IList<JToken> l = new JObject(p1, p2);

                var p3 = new JProperty("Test2", "II");

                l.Add(p3);
            },
            "Can not add property Test2 to Argon.JObject. Property with the same name already exists on object.");

    [Fact]
    public void GenericListJTokenRemove()
    {
        var p1 = new JProperty("Test1", 1);
        var p2 = new JProperty("Test2", "Two");
        IList<JToken> l = new JObject(p1, p2);

        var p3 = new JProperty("Test3", "III");

        // won't do anything
        Assert.False(l.Remove(p3));
        Assert.Equal(2, l.Count);

        Assert.True(l.Remove(p1));
        Assert.Equal(1, l.Count);
        Assert.False(l.Contains(p1));
        Assert.True(l.Contains(p2));

        Assert.True(l.Remove(p2));
        Assert.Equal(0, l.Count);
        Assert.False(l.Contains(p2));
        Assert.Equal(null, p2.Parent);
    }

    [Fact]
    public void GenericListJTokenRemoveAt()
    {
        var p1 = new JProperty("Test1", 1);
        var p2 = new JProperty("Test2", "Two");
        IList<JToken> l = new JObject(p1, p2);

        // won't do anything
        l.RemoveAt(0);

        l.Remove(p1);
        Assert.Equal(1, l.Count);

        l.Remove(p2);
        Assert.Equal(0, l.Count);
    }

    [Fact]
    public void GenericListJTokenInsert()
    {
        var p1 = new JProperty("Test1", 1);
        var p2 = new JProperty("Test2", "Two");
        IList<JToken> l = new JObject(p1, p2);

        var p3 = new JProperty("Test3", "III");

        l.Insert(1, p3);
        Assert.Equal(l, p3.Parent);

        Assert.Equal(p1, l[0]);
        Assert.Equal(p3, l[1]);
        Assert.Equal(p2, l[2]);
    }

    [Fact]
    public void GenericListJTokenIsReadOnly()
    {
        IList<JToken> l = new JObject();
        Assert.False(l.IsReadOnly);
    }

    [Fact]
    public void GenericListJTokenSetItem()
    {
        var p1 = new JProperty("Test1", 1);
        var p2 = new JProperty("Test2", "Two");
        IList<JToken> l = new JObject(p1, p2);

        var p3 = new JProperty("Test3", "III");

        l[0] = p3;

        Assert.Equal(p3, l[0]);
        Assert.Equal(p2, l[1]);
    }

    [Fact]
    public void GenericListJTokenSetItemAlreadyExists() =>
        XUnitAssert.Throws<ArgumentException>(
            () =>
            {
                var p1 = new JProperty("Test1", 1);
                var p2 = new JProperty("Test2", "Two");
                IList<JToken> l = new JObject(p1, p2);

                var p3 = new JProperty("Test3", "III");

                l[0] = p3;
                l[1] = p3;
            },
            "Can not add property Test3 to Argon.JObject. Property with the same name already exists on object.");

    [Fact]
    public void GetGeocodeAddress()
    {
        var json = """
            {
              "name": "Address: 435 North Mulford Road Rockford, IL 61107",
              "Status": {
                "code": 200,
                "request": "geocode"
              },
              "Placemark": [ {
                "id": "p1",
                "address": "435 N Mulford Rd, Rockford, IL 61107, USA",
                "AddressDetails": {
               "Accuracy" : 8,
               "Country" : {
                  "AdministrativeArea" : {
                     "AdministrativeAreaName" : "IL",
                     "SubAdministrativeArea" : {
                        "Locality" : {
                           "LocalityName" : "Rockford",
                           "PostalCode" : {
                              "PostalCodeNumber" : "61107"
                           },
                           "Thoroughfare" : {
                              "ThoroughfareName" : "435 N Mulford Rd"
                           }
                        },
                        "SubAdministrativeAreaName" : "Winnebago"
                     }
                  },
                  "CountryName" : "USA",
                  "CountryNameCode" : "US"
               }
            },
                "ExtendedData": {
                  "LatLonBox": {
                    "north": 42.2753076,
                    "south": 42.2690124,
                    "east": -88.9964645,
                    "west": -89.0027597
                  }
                },
                "Point": {
                  "coordinates": [ -88.9995886, 42.2721596, 0 ]
                }
              } ]
            }
            """;

        var o = JObject.Parse(json);

        var searchAddress = (string) o["Placemark"][0]["AddressDetails"]["Country"]["AdministrativeArea"]["SubAdministrativeArea"]["Locality"]["Thoroughfare"]["ThoroughfareName"];
        Assert.Equal("435 N Mulford Rd", searchAddress);
    }

    [Fact]
    public void SetValueWithInvalidPropertyName() =>
        XUnitAssert.Throws<ArgumentException>(
            () =>
            {
                var o = new JObject
                {
                    [0] = new JValue(3)
                };
            },
            "Set JObject values with invalid key value: 0. Object property name expected.");

    [Fact]
    public void SetValue()
    {
        object key = "TestKey";

        var o = new JObject
        {
            [key] = new JValue(3)
        };

        Assert.Equal(3, (int) o[key]);
    }

    [Fact]
    public Task ParseMultipleProperties()
    {
        var json = """
            {
                Name: 'Name1',
                Name: 'Name2'
            }
            """;

        return Throws(() => JObject.Parse(json)).IgnoreStackTrace();
    }

    [Fact]
    public Task ParseMultipleProperties_EmptySettings()
    {
        var json = """
            {
                Name: 'Name1',
                Name: 'Name2'
            }
            """;

        return Throws(() => JObject.Parse(json, new())).IgnoreStackTrace();
    }

    [Fact]
    public void WriteObjectNullDBNullValue()
    {
        var dbNull = DBNull.Value;
        var v = new JValue(dbNull);
        Assert.Equal(DBNull.Value, v.Value);
        Assert.Equal(JTokenType.Null, v.Type);

        var o = new JObject
        {
            ["title"] = v
        };

        var output = o.ToString();

        XUnitAssert.AreEqualNormalized(
            """
            {
              "title": null
            }
            """,
            output);
    }

    [Fact]
    public void InvalidValueCastExceptionMessage() =>
        XUnitAssert.Throws<ArgumentException>(
            () =>
            {
                var json = """
                    {
                      "responseData": {}, 
                      "responseDetails": null, 
                      "responseStatus": 200
                    }
                    """;

                var o = JObject.Parse(json);

                var name = (string) o["responseData"];
            },
            "Can not convert Object to String.");

    [Fact]
    public void InvalidPropertyValueCastExceptionMessage() =>
        XUnitAssert.Throws<ArgumentException>(
            () =>
            {
                var json = """
                    {
                      "responseData": {}, 
                      "responseDetails": null, 
                      "responseStatus": 200
                    }
                    """;

                var o = JObject.Parse(json);

                var name = (string) o.Property("responseData");
            },
            "Can not convert Object to String.");

    [Fact]
    public void ParseIncomplete() =>
        XUnitAssert.Throws<Exception>(
            () => JObject.Parse("{ foo:"),
            "Unexpected end of content while loading JObject. Path 'foo', line 1, position 6.");

    [Fact]
    public void LoadFromNestedObject()
    {
        var jsonText = """
            {
              "short":
              {
                "error":
                {
                  "code":0,
                  "msg":"No action taken"
                }
              }
            }
            """;

        var reader = new JsonTextReader(new StringReader(jsonText));
        reader.Read();
        reader.Read();
        reader.Read();
        reader.Read();
        reader.Read();

        var o = (JObject) JToken.ReadFrom(reader);
        Assert.NotNull(o);
        XUnitAssert.AreEqualNormalized(
            """
            {
              "code": 0,
              "msg": "No action taken"
            }
            """,
            o.ToString(Formatting.Indented));
    }

    [Fact]
    public void LoadFromNestedObjectIncomplete() =>
        XUnitAssert.Throws<JsonReaderException>(
            () =>
            {
                var jsonText = """
                {
                  "short":
                  {
                    "error":
                    {
                      "code":0
                """;

                var reader = new JsonTextReader(new StringReader(jsonText));
                reader.Read();
                reader.Read();
                reader.Read();
                reader.Read();
                reader.Read();

                JToken.ReadFrom(reader);
            },
            "Unexpected end of content while loading JObject. Path 'short.error.code', line 6, position 14.");

    [Fact]
    public void ParseEmptyObjectWithComment()
    {
        var o = JObject.Parse("{ /* A Comment */ }");
        Assert.Equal(0, o.Count);
    }

    [Fact]
    public void FromObjectTimeSpan()
    {
        var v = (JValue) JToken.FromObject(TimeSpan.FromDays(1));
        Assert.Equal(v.Value, TimeSpan.FromDays(1));

        Assert.Equal("1.00:00:00", v.ToString());
    }

    [Fact]
    public void FromObjectUri()
    {
        var v = (JValue) JToken.FromObject(new Uri("http://www.stuff.co.nz"));
        Assert.Equal(v.Value, new Uri("http://www.stuff.co.nz"));

        Assert.Equal("http://www.stuff.co.nz/", v.ToString());
    }

    [Fact]
    public void FromObjectGuid()
    {
        var v = (JValue) JToken.FromObject(new Guid("9065ACF3-C820-467D-BE50-8D4664BEAF35"));
        Assert.Equal(v.Value, new Guid("9065ACF3-C820-467D-BE50-8D4664BEAF35"));

        Assert.Equal("9065acf3-c820-467d-be50-8d4664beaf35", v.ToString());
    }

    [Fact]
    public void ParseAdditionalContent() =>
        XUnitAssert.Throws<JsonReaderException>(
            () =>
            {
                var json = """
                    {
                        Name: 'Apple',
                        Expiry: '2014-06-04T00:00:00Z',
                        Price: 3.99,
                        Sizes: [
                            'Small',
                            'Medium',
                            'Large'
                        ]
                    }, 987987
                    """;

                var o = JObject.Parse(json);
            },
            "Additional text encountered after finished reading JSON content: ,. Path '', line 10, position 1.");

    [Fact]
    public void DeepEqualsIgnoreOrder()
    {
        var o1 = new JObject(
            new JProperty("null", null),
            new JProperty("integer", 1),
            new JProperty("string", "string!"),
            new JProperty("decimal", 0.5m),
            new JProperty("array", new JArray(1, 2)));

        Assert.True(o1.DeepEquals(o1));

        var o2 = new JObject(
            new JProperty("null", null),
            new JProperty("string", "string!"),
            new JProperty("decimal", 0.5m),
            new JProperty("integer", 1),
            new JProperty("array", new JArray(1, 2)));

        Assert.True(o1.DeepEquals(o2));

        var o3 = new JObject(
            new JProperty("null", null),
            new JProperty("string", "string!"),
            new JProperty("decimal", 0.5m),
            new JProperty("integer", 2),
            new JProperty("array", new JArray(1, 2)));

        Assert.False(o1.DeepEquals(o3));

        var o4 = new JObject(
            new JProperty("null", null),
            new JProperty("string", "string!"),
            new JProperty("decimal", 0.5m),
            new JProperty("integer", 1),
            new JProperty("array", new JArray(2, 1)));

        Assert.False(o1.DeepEquals(o4));

        var o5 = new JObject(
            new JProperty("null", null),
            new JProperty("string", "string!"),
            new JProperty("decimal", 0.5m),
            new JProperty("integer", 1));

        Assert.False(o1.DeepEquals(o5));

        Assert.False(o1.DeepEquals(null));
    }

    [Fact]
    public void ToListOnEmptyObject()
    {
        var o = JObject.Parse(@"{}");
        var l1 = o.ToList<JToken>();
        Assert.Equal(0, l1.Count);

        var l2 = o.ToList<KeyValuePair<string, JToken>>();
        Assert.Equal(0, l2.Count);

        o = JObject.Parse(@"{'hi':null}");

        l1 = o.ToList<JToken>();
        Assert.Equal(1, l1.Count);

        l2 = o.ToList<KeyValuePair<string, JToken>>();
        Assert.Equal(1, l2.Count);
    }

    [Fact]
    public void EmptyObjectDeepEquals()
    {
        Assert.True(JToken.DeepEquals(new JObject(), new JObject()));

        var a = new JObject();
        var b = new JObject
        {
            {
                "hi", "bye"
            }
        };

        b.Remove("hi");

        Assert.True(JToken.DeepEquals(a, b));
        Assert.True(JToken.DeepEquals(b, a));
    }

    [Fact]
    public void GetValueBlogExample()
    {
        var o = JObject.Parse(
            """
            {
                'name': 'Lower',
                'NAME': 'Upper'
            }
            """);

        var exactMatch = (string) o.GetValue("NAME", StringComparison.OrdinalIgnoreCase);
        // Upper

        var ignoreCase = (string) o.GetValue("Name", StringComparison.OrdinalIgnoreCase);
        // Lower

        Assert.Equal("Upper", exactMatch);
        Assert.Equal("Lower", ignoreCase);
    }

    [Fact]
    public void GetValue()
    {
        var a = new JObject
        {
            ["Name"] = "Name!",
            ["name"] = "name!",
            ["title"] = "Title!"
        };

        Assert.Equal(null, a.GetValue("NAME", StringComparison.Ordinal));
        Assert.Equal(null, a.GetValue("NAME"));
        Assert.Equal(null, a.GetValue("TITLE"));
        Assert.Equal("Name!", (string) a.GetValue("NAME", StringComparison.OrdinalIgnoreCase));
        Assert.Equal("name!", (string) a.GetValue("name", StringComparison.Ordinal));
        Assert.Equal(null, a.GetValue(null, StringComparison.Ordinal));
        Assert.Equal(null, a.GetValue(null));

        Assert.False(a.TryGetValue("NAME", StringComparison.Ordinal, out var v));
        Assert.Equal(null, v);

        Assert.False(a.TryGetValue("NAME", out v));
        Assert.False(a.TryGetValue("TITLE", out v));

        Assert.True(a.TryGetValue("NAME", StringComparison.OrdinalIgnoreCase, out v));
        Assert.Equal("Name!", (string) v);

        Assert.True(a.TryGetValue("name", StringComparison.Ordinal, out v));
        Assert.Equal("name!", (string) v);

        Assert.False(a.TryGetValue(null, StringComparison.Ordinal, out v));
    }

    public class FooJsonConverter : JsonConverter
    {
        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            var token = JToken.FromObject(value, new()
            {
                ContractResolver = new CamelCasePropertyNamesContractResolver()
            });
            if (token.Type == JTokenType.Object)
            {
                var o = (JObject) token;
                o.AddFirst(new JProperty("foo", "bar"));
                o.WriteTo(writer);
            }
            else
            {
                token.WriteTo(writer);
            }
        }

        public override object ReadJson(JsonReader reader, Type type, object existingValue, JsonSerializer serializer) =>
            throw new NotSupportedException("This custom converter only supports serialization and not deserialization.");

        public override bool CanRead => false;

        public override bool CanConvert(Type type) =>
            true;
    }

    [Fact]
    public void FromObjectInsideConverterWithCustomSerializer()
    {
        var p = new Person
        {
            Name = "Daniel Wertheim"
        };

        var settings = new JsonSerializerSettings
        {
            Converters = new()
            {
                new FooJsonConverter()
            },
            ContractResolver = new CamelCasePropertyNamesContractResolver()
        };

        var json = JsonConvert.SerializeObject(p, settings);

        Assert.Equal(@"{""foo"":""bar"",""name"":""Daniel Wertheim"",""birthDate"":""0001-01-01T00:00:00"",""lastModified"":""0001-01-01T00:00:00""}", json);
    }

    [Fact]
    public void Parse_NoComments()
    {
        var json = "{'prop':[1,2/*comment*/,3]}";

        var o = JObject.Parse(json, new()
        {
            CommentHandling = CommentHandling.Ignore
        });

        Assert.Equal(3, o["prop"].Count());
        Assert.Equal(1, (int) o["prop"][0]);
        Assert.Equal(2, (int) o["prop"][1]);
        Assert.Equal(3, (int) o["prop"][2]);
    }

    [Fact]
    public void Parse_ExcessiveContentJustComments()
    {
        var json = @"{'prop':[1,2,3]}/*comment*/
//Another comment.";

        var o = JObject.Parse(json);

        Assert.Equal(3, o["prop"].Count());
        Assert.Equal(1, (int) o["prop"][0]);
        Assert.Equal(2, (int) o["prop"][1]);
        Assert.Equal(3, (int) o["prop"][2]);
    }

    [Fact]
    public void Parse_ExcessiveContent()
    {
        var json = @"{'prop':[1,2,3]}/*comment*/
//Another comment.
[]";

        XUnitAssert.Throws<JsonReaderException>(
            () => JObject.Parse(json),
            "Additional text encountered after finished reading JSON content: [. Path '', line 3, position 0.");
    }

    [Fact]
    public void Property()
    {
        var a = new JObject
        {
            ["Name"] = "Name!",
            ["name"] = "name!",
            ["title"] = "Title!"
        };

        Assert.Equal(null, a.PropertyOrNull("NAME"));
        Assert.Equal(null, a.PropertyOrNull("TITLE"));

        // Return first match when ignoring case
        Assert.Equal("Name", a.PropertyOrNull("NAME", StringComparison.OrdinalIgnoreCase).Name);
        // Return exact match before ignoring case
        Assert.Equal("name", a.PropertyOrNull("name", StringComparison.OrdinalIgnoreCase).Name);
    }
}