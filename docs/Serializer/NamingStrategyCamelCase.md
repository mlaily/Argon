# Camel case property names

This sample uses a `Argon.CamelCaseNamingStrategy` specified using a contract resolver to camel case serialized property names.

<!-- snippet: NamingStrategyCamelCaseTypes -->
<a id='snippet-namingstrategycamelcasetypes'></a>
```cs
public class User
{
    public string UserName { get; set; }
    public bool Enabled { get; set; }
}
```
<sup><a href='/src/ArgonTests/Documentation/Samples/Serializer/NamingStrategyCamelCase.cs#L7-L15' title='Snippet source file'>snippet source</a> | <a href='#snippet-namingstrategycamelcasetypes' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

<!-- snippet: NamingStrategyCamelCaseUsage -->
<a id='snippet-namingstrategycamelcaseusage'></a>
```cs
var user1 = new User
{
    UserName = "jamesn",
    Enabled = true
};

var contractResolver = new DefaultContractResolver
{
    NamingStrategy = new CamelCaseNamingStrategy()
};

var json = JsonConvert.SerializeObject(user1, new JsonSerializerSettings
{
    ContractResolver = contractResolver,
    Formatting = Formatting.Indented
});

Console.WriteLine(json);
// {
//   "userName": "jamesn",
//   "enabled": true
// }
```
<sup><a href='/src/ArgonTests/Documentation/Samples/Serializer/NamingStrategyCamelCase.cs#L20-L45' title='Snippet source file'>snippet source</a> | <a href='#snippet-namingstrategycamelcaseusage' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->
