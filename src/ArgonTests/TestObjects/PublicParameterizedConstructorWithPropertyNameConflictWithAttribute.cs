﻿// Copyright (c) 2007 James Newton-King. All rights reserved.
// Use of this source code is governed by The MIT License,
// as found in the license.md file.

namespace TestObjects;

public class PublicParameterizedConstructorWithPropertyNameConflictWithAttribute
{
    public PublicParameterizedConstructorWithPropertyNameConflictWithAttribute([JsonProperty("name")] string nameParameter) =>
        Name = Convert.ToInt32(nameParameter);

    public int Name { get; }
}