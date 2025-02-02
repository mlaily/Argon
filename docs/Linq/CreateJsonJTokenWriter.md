# Create JSON with JTokenWriter

This sample creates `Argon.JObject` and `Argon.JArray` instances using a `Argon.JTokenWriter`.

<!-- snippet: CreateJsonJTokenWriter -->
<a id='snippet-createjsonjtokenwriter'></a>
```cs
var writer = new JTokenWriter();
writer.WriteStartObject();
writer.WritePropertyName("name1");
writer.WriteValue("value1");
writer.WritePropertyName("name2");
writer.WriteStartArray();
writer.WriteValue(1);
writer.WriteValue(2);
writer.WriteEndArray();
writer.WriteEndObject();

var o = (JObject) writer.Token;

Console.WriteLine(o.ToString());
// {
//   "name1": "value1",
//   "name2": [
//     1,
//     2
//   ]
// }
```
<sup><a href='/src/ArgonTests/Documentation/Samples/Linq/CreateJsonJTokenWriter.cs#L12-L36' title='Snippet source file'>snippet source</a> | <a href='#snippet-createjsonjtokenwriter' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->
