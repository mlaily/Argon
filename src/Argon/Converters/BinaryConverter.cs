﻿#region License
// Copyright (c) 2007 James Newton-King
//
// Permission is hereby granted, free of charge, to any person
// obtaining a copy of this software and associated documentation
// files (the "Software"), to deal in the Software without
// restriction, including without limitation the rights to use,
// copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the
// Software is furnished to do so, subject to the following
// conditions:
//
// The above copyright notice and this permission notice shall be
// included in all copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND,
// EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES
// OF MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
// NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT
// HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY,
// WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING
// FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR
// OTHER DEALINGS IN THE SOFTWARE.
#endregion

using System.Data.SqlTypes;

namespace Argon;

/// <summary>
/// Converts a binary value to and from a base 64 string value.
/// </summary>
public class BinaryConverter : JsonConverter
{
    const string binaryTypeName = "System.Data.Linq.Binary";
    const string binaryToArrayName = "ToArray";
    static ReflectionObject? reflectionObject;

    /// <summary>
    /// Writes the JSON representation of the object.
    /// </summary>
    public override void WriteJson(JsonWriter writer, object? value, JsonSerializer serializer)
    {
        if (value == null)
        {
            writer.WriteNull();
            return;
        }

        var data = GetByteArray(value);

        writer.WriteValue(data);
    }

    static byte[] GetByteArray(object value)
    {
        if (value.GetType().FullName == binaryTypeName)
        {
            EnsureReflectionObject(value.GetType());
            MiscellaneousUtils.Assert(reflectionObject != null);

            return (byte[])reflectionObject.GetValue(value, binaryToArrayName)!;
        }
        if (value is SqlBinary binary)
        {
            return binary.Value;
        }

        throw new JsonSerializationException($"Unexpected value type when writing binary: {value.GetType()}");
    }

    static void EnsureReflectionObject(Type type)
    {
        reflectionObject ??= ReflectionObject.Create(type, type.GetConstructor(new[] {typeof(byte[])}), binaryToArrayName);
    }

    /// <summary>
    /// Reads the JSON representation of the object.
    /// </summary>
    public override object? ReadJson(JsonReader reader, Type type, object? existingValue, JsonSerializer serializer)
    {
        if (reader.TokenType == JsonToken.Null)
        {
            if (!ReflectionUtils.IsNullable(type))
            {
                throw JsonSerializationException.Create(reader, $"Cannot convert null value to {type}.");
            }

            return null;
        }

        byte[] data;

        if (reader.TokenType == JsonToken.StartArray)
        {
            data = ReadByteArray(reader);
        }
        else if (reader.TokenType == JsonToken.String)
        {
            // current token is already at base64 string
            // unable to call ReadAsBytes so do it the old fashion way
            var encodedData = reader.Value!.ToString();
            data = Convert.FromBase64String(encodedData);
        }
        else
        {
            throw JsonSerializationException.Create(reader, $"Unexpected token parsing binary. Expected String or StartArray, got {reader.TokenType}.");
        }

        var underlyingType = ReflectionUtils.IsNullableType(type)
            ? Nullable.GetUnderlyingType(type)
            : type;

        if (underlyingType.FullName == binaryTypeName)
        {
            EnsureReflectionObject(underlyingType);
            MiscellaneousUtils.Assert(reflectionObject != null);

            return reflectionObject.Creator!(data);
        }

        if (underlyingType == typeof(SqlBinary))
        {
            return new SqlBinary(data);
        }

        throw JsonSerializationException.Create(reader, $"Unexpected object type when writing binary: {type}");
    }

    static byte[] ReadByteArray(JsonReader reader)
    {
        var byteList = new List<byte>();

        while (reader.Read())
        {
            switch (reader.TokenType)
            {
                case JsonToken.Integer:
                    byteList.Add(Convert.ToByte(reader.Value, CultureInfo.InvariantCulture));
                    break;
                case JsonToken.EndArray:
                    return byteList.ToArray();
                case JsonToken.Comment:
                    // skip
                    break;
                default:
                    throw JsonSerializationException.Create(reader, $"Unexpected token when reading bytes: {reader.TokenType}");
            }
        }

        throw JsonSerializationException.Create(reader, "Unexpected end when reading bytes.");
    }

    /// <summary>
    /// Determines whether this instance can convert the specified object type.
    /// </summary>
    /// <returns>
    /// 	<c>true</c> if this instance can convert the specified object type; otherwise, <c>false</c>.
    /// </returns>
    public override bool CanConvert(Type type)
    {
        return type.FullName == binaryTypeName ||
               type == typeof(SqlBinary) ||
               type == typeof(SqlBinary?);
    }
}