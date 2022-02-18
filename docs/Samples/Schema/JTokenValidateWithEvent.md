<?xml version="1.0" encoding="utf-8"?>
<topic id="JTokenValidateWithEvent" revisionNumber="1">
  <developerConceptualDocument xmlns="http://ddue.schemas.microsoft.com/authoring/2003/5" xmlns:xlink="http://www.w3.org/1999/xlink">
    <introduction>
      <para>This sample validates a <codeEntityReference>T:Argon.Linq.JObject</codeEntityReference>
      using the <codeEntityReference>M:Argon.Schema.Extensions.IsValid(Argon.Linq.JToken,Argon.Schema.JsonSchema)</codeEntityReference>
      extension method and raises an event for each validation error.</para>
    </introduction>
<alert class="caution">
  <para>
    <legacyBold>Obsolete.</legacyBold> JSON Schema validation has been moved to its own package. See <externalLink>
        <linkText>https://www.newtonsoft.com/jsonschema</linkText>
        <linkUri>https://www.newtonsoft.com/jsonschema</linkUri>
        <linkTarget>_blank</linkTarget>
      </externalLink>
      for more details.
  </para>
</alert>
    <section>
      <title>Sample</title>
      <content>
        <code lang="cs" source="..\Src\Tests\Documentation\Samples\Schema\JTokenValidateWithEvent.cs" region="Usage" title="Usage" />
      </content>
    </section>
  </developerConceptualDocument>
</topic>