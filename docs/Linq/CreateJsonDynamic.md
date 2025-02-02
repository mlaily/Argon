# Create JSON with dynamic

This sample creates `Argon.JObject` and `Argon.JArray` instances using the C# dynamic functionality.

<!-- snippet: CreateJsonDynamic -->
<a id='snippet-createjsondynamic'></a>
```cs
dynamic product = new JObject();
product.ProductName = "Elbow Grease";
product.Enabled = true;
product.Price = 4.90m;
product.StockCount = 9000;
product.StockValue = 44100;
product.Tags = new JArray("Real", "OnSale");

Console.WriteLine(product.ToString());
// {
//   "ProductName": "Elbow Grease",
//   "Enabled": true,
//   "Price": 4.90,
//   "StockCount": 9000,
//   "StockValue": 44100,
//   "Tags": [
//     "Real",
//     "OnSale"
//   ]
// }
```
<sup><a href='/src/ArgonTests/Documentation/Samples/Linq/CreateJsonDynamic.cs#L13-L36' title='Snippet source file'>snippet source</a> | <a href='#snippet-createjsondynamic' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->
