// Copyright (c) 2007 James Newton-King. All rights reserved.
// Use of this source code is governed by The MIT License,
// as found in the license.md file.

namespace TestObjects;

public class ChildClassWithProtectedOverridePlusJsonProperty : BaseClassWithProtectedVirtual
{
    [JsonProperty]
    protected override string MyProperty { get; set; }

    public string GetPropertyValue() =>
        MyProperty;
}