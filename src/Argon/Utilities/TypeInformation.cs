﻿class TypeInformation
{
    public Type Type { get; }
    public PrimitiveTypeCode TypeCode { get; }

    public TypeInformation(Type type, PrimitiveTypeCode typeCode)
    {
        Type = type;
        TypeCode = typeCode;
    }
}