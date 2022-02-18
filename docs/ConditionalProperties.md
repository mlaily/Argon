<?xml version="1.0" encoding="utf-8"?>
<topic id="ConditionalProperties" revisionNumber="1">
  <developerConceptualDocument xmlns="http://ddue.schemas.microsoft.com/authoring/2003/5" xmlns:xlink="http://www.w3.org/1999/xlink">
    <introduction>
      <para>Json.NET has the ability to conditionally serialize properties by placing a ShouldSerialize method on a class.
      This functionality is similar to the <externalLink>
<linkText>XmlSerializer ShouldSerialize feature</linkText>
<linkUri>http://msdn.microsoft.com/en-us/library/53b8022e.aspx</linkUri>
<linkTarget>_blank</linkTarget>
</externalLink>.</para>
      <autoOutline lead="none" excludeRelatedTopics="true" />
    </introduction>
    <section address="ShouldSerialize">
      <title>ShouldSerialize</title>
      <content>
        <para>To conditionally serialize a property, add a method that returns boolean with the same name as the property and then prefix the method name
        with ShouldSerialize. The result of the method determines whether the property is serialized. If the method returns true then the
        property will be serialized, if it returns false then the property will be skipped.</para>

<code lang="cs" source="..\Src\Tests\Documentation\ConditionalPropertiesTests.cs" region="EmployeeShouldSerializeExample" title="Employee class with a ShouldSerialize method" />
<code lang="cs" source="..\Src\Tests\Documentation\ConditionalPropertiesTests.cs" region="ShouldSerializeClassTest" title="ShouldSerialize output" />
        
      </content>
    </section>
    <section address="IContractResolver">
      <title>IContractResolver</title>
      <content>
        <para>ShouldSerialize can also be set using an <codeEntityReference>T:Argon.Serialization.IContractResolver</codeEntityReference>.
        Conditionally serializing a property using an IContractResolver is useful avoid placing a ShouldSerialize method on a class
        or are unable to.</para>

<code lang="cs" source="..\Src\Tests\Documentation\ConditionalPropertiesTests.cs" region="ShouldSerializeContractResolver" title="Conditional properties with IContractResolver" />
        
      </content>
    </section>
    <relatedTopics>
      <codeEntityReference>T:Argon.JsonSerializer</codeEntityReference>
      <codeEntityReference>T:Argon.Serialization.IContractResolver</codeEntityReference>
      <codeEntityReference>P:Argon.Serialization.JsonProperty.ShouldSerialize</codeEntityReference>
    </relatedTopics>
  </developerConceptualDocument>
</topic>