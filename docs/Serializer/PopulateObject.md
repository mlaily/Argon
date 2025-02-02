# Populate an Object

This sample populates an existing object instance with values from JSON.

<!-- snippet: PopulateObjectTypes -->
<a id='snippet-populateobjecttypes'></a>
```cs
public class Account
{
    public string Email { get; set; }
    public bool Active { get; set; }
    public DateTime CreatedDate { get; set; }
    public List<string> Roles { get; set; }
}
```
<sup><a href='/src/ArgonTests/Documentation/Samples/Serializer/PopulateObject.cs#L7-L17' title='Snippet source file'>snippet source</a> | <a href='#snippet-populateobjecttypes' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

<!-- snippet: PopulateObjectUsage -->
<a id='snippet-populateobjectusage'></a>
```cs
var account = new Account
{
    Email = "james@example.com",
    Active = true,
    CreatedDate = new(2013, 1, 20, 0, 0, 0, DateTimeKind.Utc),
    Roles = new()
    {
        "User",
        "Admin"
    }
};

var json = """
    {
      'Active': false,
      'Roles': [
        'Expired'
      ]
    }
    """;

JsonConvert.PopulateObject(json, account);

Console.WriteLine(account.Email);
// james@example.com

Console.WriteLine(account.Active);
// false

Console.WriteLine(string.Join(", ", account.Roles.ToArray()));
// User, Admin, Expired
```
<sup><a href='/src/ArgonTests/Documentation/Samples/Serializer/PopulateObject.cs#L22-L56' title='Snippet source file'>snippet source</a> | <a href='#snippet-populateobjectusage' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->
