# Using JValue.Value

This sample gets `Argon.JValue` internal values using `Argon.JValue.Value`.

<!-- snippet: JValueValue -->
<a id='snippet-jvaluevalue'></a>
```cs
var s = new JValue("A string value");

Console.WriteLine(s.Value.GetType().Name);
// String
Console.WriteLine(s.Value);
// A string value

var u = new JValue(new Uri("http://www.google.com/"));

Console.WriteLine(u.Value.GetType().Name);
// Uri
Console.WriteLine(u.Value);
// http://www.google.com/
```
<sup><a href='/src/ArgonTests/Documentation/Samples/Linq/JValueValue.cs#L12-L28' title='Snippet source file'>snippet source</a> | <a href='#snippet-jvaluevalue' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->
