# ConstructorHandling setting

This sample uses the `Argon.ConstructorHandling` setting to successfully deserialize the class using its non-public constructor.

<!-- snippet: DeserializeConstructorHandlingTypes -->
<a id='snippet-deserializeconstructorhandlingtypes'></a>
```cs
public class Website
{
    public string Url { get; set; }

    // ReSharper disable once UnusedMember.Local
    Website()
    {
    }

    public Website(Website website) =>
        Url = website.Url;
}
```
<sup><a href='/src/ArgonTests/Documentation/Samples/Serializer/DeserializeConstructorHandling.cs#L7-L22' title='Snippet source file'>snippet source</a> | <a href='#snippet-deserializeconstructorhandlingtypes' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

<!-- snippet: DeserializeConstructorHandlingUsage -->
<a id='snippet-deserializeconstructorhandlingusage'></a>
```cs
var json = @"{'Url':'http://www.google.com'}";

try
{
    JsonConvert.DeserializeObject<Website>(json);
}
catch (Exception exception)
{
    Console.WriteLine(exception.Message);
    // Value cannot be null.
    // Parameter name: website
}

var website = JsonConvert.DeserializeObject<Website>(json, new JsonSerializerSettings
{
    ConstructorHandling = ConstructorHandling.AllowNonPublicDefaultConstructor
});

Console.WriteLine(website.Url);
// http://www.google.com
```
<sup><a href='/src/ArgonTests/Documentation/Samples/Serializer/DeserializeConstructorHandling.cs#L27-L50' title='Snippet source file'>snippet source</a> | <a href='#snippet-deserializeconstructorhandlingusage' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->
