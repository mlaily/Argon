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

using System.Data;

namespace Argon.DataSetConverters;

/// <summary>
/// Converts a <see cref="DataSet"/> to and from JSON.
/// </summary>
public class DataSetConverter : JsonConverter
{
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

        var dataSet = (DataSet)value;
        var resolver = serializer.ContractResolver as DefaultContractResolver;

        var converter = new DataTableConverter();

        writer.WriteStartObject();

        foreach (DataTable table in dataSet.Tables)
        {
            writer.WritePropertyName(resolver != null ? resolver.GetResolvedPropertyName(table.TableName) : table.TableName);

            converter.WriteJson(writer, table, serializer);
        }

        writer.WriteEndObject();
    }

    /// <summary>
    /// Reads the JSON representation of the object.
    /// </summary>
    public override object? ReadJson(JsonReader reader, Type type, object? existingValue, JsonSerializer serializer)
    {
        if (reader.TokenType == JsonToken.Null)
        {
            return null;
        }

        var set = GetDataSet(type);

        var converter = new DataTableConverter();

        reader.ReadAndAssert();

        while (reader.TokenType == JsonToken.PropertyName)
        {
            var table = set.Tables[(string)reader.Value!];
            var exists = table != null;

            table = (DataTable)converter.ReadJson(reader, typeof(DataTable), table, serializer)!;

            if (!exists)
            {
                set.Tables.Add(table);
            }

            reader.ReadAndAssert();
        }

        return set;
    }

    static DataSet GetDataSet(Type type)
    {
        // handle typed datasets
        if (type == typeof(DataSet))
        {
            return new DataSet();
        }

        return (DataSet) Activator.CreateInstance(type);
    }

    /// <summary>
    /// Determines whether this instance can convert the specified value type.
    /// </summary>
    /// <param name="valueType">Type of the value.</param>
    /// <returns>
    ///   <c>true</c> if this instance can convert the specified value type; otherwise, <c>false</c>.
    /// </returns>
    public override bool CanConvert(Type valueType)
    {
        return typeof(DataSet).IsAssignableFrom(valueType);
    }
}