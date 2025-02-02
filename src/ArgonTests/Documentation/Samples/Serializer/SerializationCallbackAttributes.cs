﻿// Copyright (c) 2007 James Newton-King. All rights reserved.
// Use of this source code is governed by The MIT License,
// as found in the license.md file.

public class SerializationCallbackAttributes : TestFixtureBase
{
    #region SerializationCallbackAttributesTypes

    public class SerializationEventTestObject :
        IJsonOnSerializing,
        IJsonOnSerialized,
        IJsonOnDeserializing,
        IJsonOnDeserialized
    {
        // 2222
        // This member is serialized and deserialized with no change.
        public int Member1 { get; set; }

        // The value of this field is set and reset during and
        // after serialization.
        public string Member2 { get; set; }

        // This field is not serialized. The OnDeserializedAttribute
        // is used to set the member value after serialization.
        [JsonIgnore]
        public string Member3 { get; set; }

        // This field is set to null, but populated after deserialization.
        public string Member4 { get; set; }

        public SerializationEventTestObject()
        {
            Member1 = 11;
            Member2 = "Hello World!";
            Member3 = "This is a nonserialized value";
            Member4 = null;
        }

        public void OnSerializing() =>
            Member2 = "This value went into the data file during serialization.";

        public void OnSerialized() =>
            Member2 = "This value was reset after serialization.";

        public void OnDeserializing() =>
            Member3 = "This value was set during deserialization";

        public void OnDeserialized() =>
            Member4 = "This value was set after deserialization.";
    }

    #endregion

    [Fact]
    public void Example()
    {
        #region SerializationCallbackAttributesUsage

        var obj = new SerializationEventTestObject();

        Console.WriteLine(obj.Member1);
        // 11
        Console.WriteLine(obj.Member2);
        // Hello World!
        Console.WriteLine(obj.Member3);
        // This is a nonserialized value
        Console.WriteLine(obj.Member4);
        // null

        var json = JsonConvert.SerializeObject(obj, Formatting.Indented);
        // {
        //   "Member1": 11,
        //   "Member2": "This value went into the data file during serialization.",
        //   "Member4": null
        // }

        Console.WriteLine(obj.Member1);
        // 11
        Console.WriteLine(obj.Member2);
        // This value was reset after serialization.
        Console.WriteLine(obj.Member3);
        // This is a nonserialized value
        Console.WriteLine(obj.Member4);
        // null

        obj = JsonConvert.DeserializeObject<SerializationEventTestObject>(json);

        Console.WriteLine(obj.Member1);
        // 11
        Console.WriteLine(obj.Member2);
        // This value went into the data file during serialization.
        Console.WriteLine(obj.Member3);
        // This value was set during deserialization
        Console.WriteLine(obj.Member4);
        // This value was set after deserialization.

        #endregion

        Assert.Equal("This value was set after deserialization.", obj.Member4);
    }
}