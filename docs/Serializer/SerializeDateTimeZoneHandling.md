# DateTimeZoneHandling setting

This sample uses the `Argon.DateTimeZoneHandling` setting to control how `System.DateTime` and `System.DateTimeOffset` are serialized.

<!-- snippet: SerializeDateTimeZoneHandlingTypes -->
<a id='snippet-serializedatetimezonehandlingtypes'></a>
```cs
public class Flight
{
    public string Destination { get; set; }
    public DateTime DepartureDate { get; set; }
    public DateTime DepartureDateUtc { get; set; }
    public DateTime DepartureDateLocal { get; set; }
    public TimeSpan Duration { get; set; }
}
```
<sup><a href='/src/ArgonTests/Documentation/Samples/Serializer/SerializeDateTimeZoneHandling.cs#L7-L18' title='Snippet source file'>snippet source</a> | <a href='#snippet-serializedatetimezonehandlingtypes' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

<!-- snippet: SerializeDateTimeZoneHandlingUsage -->
<a id='snippet-serializedatetimezonehandlingusage'></a>
```cs
var flight = new Flight
{
    Destination = "Dubai",
    DepartureDate = new(2013, 1, 21, 0, 0, 0, DateTimeKind.Unspecified),
    DepartureDateUtc = new(2013, 1, 21, 0, 0, 0, DateTimeKind.Utc),
    DepartureDateLocal = new(2013, 1, 21, 0, 0, 0, DateTimeKind.Local),
    Duration = TimeSpan.FromHours(5.5)
};

var jsonWithRoundtripTimeZone = JsonConvert.SerializeObject(flight, Formatting.Indented, new JsonSerializerSettings());

Console.WriteLine(jsonWithRoundtripTimeZone);
// {
//   "Destination": "Dubai",
//   "DepartureDate": "2013-01-21T00:00:00",
//   "DepartureDateUtc": "2013-01-21T00:00:00Z",
//   "DepartureDateLocal": "2013-01-21T00:00:00+01:00",
//   "Duration": "05:30:00"
// }
```
<sup><a href='/src/ArgonTests/Documentation/Samples/Serializer/SerializeDateTimeZoneHandling.cs#L23-L45' title='Snippet source file'>snippet source</a> | <a href='#snippet-serializedatetimezonehandlingusage' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->
