// Copyright (c) 2007 James Newton-King. All rights reserved.
// Use of this source code is governed by The MIT License,
// as found in the license.md file.

namespace Argon;

public partial class JsonTextWriter
{
    // It's not safe to perform the async methods here in a derived class as if the synchronous equivalent
    // has been overriden then the asynchronous method will no longer be doing the same operation.
    readonly bool safeAsync;

    /// <summary>
    /// Asynchronously flushes whatever is in the buffer to the destination and also flushes the destination.
    /// </summary>
    /// <remarks>
    /// Derived classes must override this method to get asynchronous behaviour. Otherwise it will
    /// execute synchronously, returning an already-completed task.
    /// </remarks>
    public override Task FlushAsync(Cancellation cancellation = default)
    {
        if (safeAsync)
        {
            return DoFlushAsync(cancellation);
        }

        return base.FlushAsync(cancellation);
    }

    Task DoFlushAsync(Cancellation cancellation) =>
        cancellation.CancelIfRequestedAsync() ?? writer.FlushAsync();

    /// <summary>
    /// Asynchronously writes the JSON value delimiter.
    /// </summary>
    /// <remarks>
    /// Derived classes must override this method to get asynchronous behaviour. Otherwise it will
    /// execute synchronously, returning an already-completed task.
    /// </remarks>
    protected override Task WriteValueDelimiterAsync(Cancellation cancellation)
    {
        if (safeAsync)
        {
            return DoWriteValueDelimiterAsync(cancellation);
        }

        return base.WriteValueDelimiterAsync(cancellation);
    }

    Task DoWriteValueDelimiterAsync(Cancellation cancellation) =>
        writer.WriteAsync(',', cancellation);

    /// <summary>
    /// Asynchronously writes the specified end token.
    /// </summary>
    /// <param name="token">The end token to write.</param>
    /// <remarks>
    /// Derived classes must override this method to get asynchronous behaviour. Otherwise it will
    /// execute synchronously, returning an already-completed task.
    /// </remarks>
    protected override Task WriteEndAsync(JsonToken token, Cancellation cancellation)
    {
        if (safeAsync)
        {
            return DoWriteEndAsync(token, cancellation);
        }

        return base.WriteEndAsync(token, cancellation);
    }

    Task DoWriteEndAsync(JsonToken token, Cancellation cancellation)
    {
        switch (token)
        {
            case JsonToken.EndObject:
                return writer.WriteAsync('}', cancellation);
            case JsonToken.EndArray:
                return writer.WriteAsync(']', cancellation);
            default:
                throw JsonWriterException.Create(this, $"Invalid JsonToken: {token}");
        }
    }

    /// <summary>
    /// Asynchronously closes this writer.
    /// If <see cref="JsonWriter.CloseOutput" /> is set to <c>true</c>, the destination is also closed.
    /// </summary>
    /// <remarks>
    /// Derived classes must override this method to get asynchronous behaviour. Otherwise it will
    /// execute synchronously, returning an already-completed task.
    /// </remarks>
    public override Task CloseAsync(Cancellation cancellation = default)
    {
        if (safeAsync)
        {
            return DoCloseAsync(cancellation);
        }

        return base.CloseAsync(cancellation);
    }

    async Task DoCloseAsync(Cancellation cancellation)
    {
        if (Top == 0) // otherwise will happen in calls to WriteEndAsync
        {
            cancellation.ThrowIfCancellationRequested();
        }

        while (Top > 0)
        {
            await WriteEndAsync(cancellation).ConfigureAwait(false);
        }

        await CloseBufferAndWriterAsync().ConfigureAwait(false);
    }

    private async Task CloseBufferAndWriterAsync()
    {
        if (writeBuffer != null)
        {
            BufferUtils.ReturnBuffer(writeBuffer);
            writeBuffer = null;
        }

        if (CloseOutput && writer != null)
        {
#if HAVE_ASYNC_DISPOABLE
            await _writer.DisposeAsync().ConfigureAwait(false);
#else
            // DisposeAsync isn't available. Instead, flush any remaining content with FlushAsync
            // to prevent Close/Dispose from making a blocking flush.
            //
            // No cancellation token on TextWriter.FlushAsync?!
            await writer.FlushAsync().ConfigureAwait(false);
#if HAVE_STREAM_READER_WRITER_CLOSE
            writer.Close();
#else
            writer.Dispose();
#endif
#endif
        }
    }

    /// <summary>
    /// Asynchronously writes the end of the current JSON object or array.
    /// </summary>
    /// <remarks>
    /// Derived classes must override this method to get asynchronous behaviour. Otherwise it will
    /// execute synchronously, returning an already-completed task.
    /// </remarks>
    public override Task WriteEndAsync(Cancellation cancellation = default)
    {
        if (safeAsync)
        {
            return WriteEndInternalAsync(cancellation);
        }

        return base.WriteEndAsync(cancellation);
    }

    /// <summary>
    /// Asynchronously writes indent characters.
    /// </summary>
    /// <remarks>
    /// Derived classes must override this method to get asynchronous behaviour. Otherwise it will
    /// execute synchronously, returning an already-completed task.
    /// </remarks>
    protected override Task WriteIndentAsync(Cancellation cancellation)
    {
        if (safeAsync)
        {
            return DoWriteIndentAsync(cancellation);
        }

        return base.WriteIndentAsync(cancellation);
    }

    Task DoWriteIndentAsync(Cancellation cancellation)
    {
        // levels of indentation multiplied by the indent count
        var currentIndentCount = Top * indentation;

        var newLineLen = SetIndentChars();
        MiscellaneousUtils.Assert(indentChars != null);

        if (currentIndentCount <= indentCharBufferSize)
        {
            return writer.WriteAsync(indentChars, 0, newLineLen + currentIndentCount, cancellation);
        }

        return WriteIndentAsync(currentIndentCount, newLineLen, cancellation);
    }

    async Task WriteIndentAsync(int currentIndentCount, int newLineLen, Cancellation cancellation)
    {
        MiscellaneousUtils.Assert(indentChars != null);

        await writer.WriteAsync(indentChars, 0, newLineLen + Math.Min(currentIndentCount, indentCharBufferSize), cancellation).ConfigureAwait(false);

        while ((currentIndentCount -= indentCharBufferSize) > 0)
        {
            await writer.WriteAsync(indentChars, newLineLen, Math.Min(currentIndentCount, indentCharBufferSize), cancellation).ConfigureAwait(false);
        }
    }

    Task WriteValueInternalAsync(JsonToken token, string value, Cancellation cancellation)
    {
        var task = InternalWriteValueAsync(token, cancellation);
        if (task.IsCompletedSuccessfully())
        {
            return writer.WriteAsync(value, cancellation);
        }

        return WriteValueInternalAsync(task, value, cancellation);
    }

    async Task WriteValueInternalAsync(Task task, string value, Cancellation cancellation)
    {
        await task.ConfigureAwait(false);
        await writer.WriteAsync(value, cancellation).ConfigureAwait(false);
    }

    /// <summary>
    /// Asynchronously writes an indent space.
    /// </summary>
    /// <remarks>
    /// Derived classes must override this method to get asynchronous behaviour. Otherwise it will
    /// execute synchronously, returning an already-completed task.
    /// </remarks>
    protected override Task WriteIndentSpaceAsync(Cancellation cancellation)
    {
        if (safeAsync)
        {
            return DoWriteIndentSpaceAsync(cancellation);
        }

        return base.WriteIndentSpaceAsync(cancellation);
    }

    Task DoWriteIndentSpaceAsync(Cancellation cancellation) =>
        writer.WriteAsync(' ', cancellation);

    /// <summary>
    /// Asynchronously writes raw JSON without changing the writer's state.
    /// </summary>
    /// <remarks>
    /// Derived classes must override this method to get asynchronous behaviour. Otherwise it will
    /// execute synchronously, returning an already-completed task.
    /// </remarks>
    public override Task WriteRawAsync(string? json, Cancellation cancellation = default)
    {
        if (safeAsync)
        {
            return DoWriteRawAsync(json, cancellation);
        }

        return base.WriteRawAsync(json, cancellation);
    }

    Task DoWriteRawAsync(string? json, Cancellation cancellation) =>
        writer.WriteAsync(json, cancellation);

    /// <summary>
    /// Asynchronously writes a null value.
    /// </summary>
    /// <remarks>
    /// Derived classes must override this method to get asynchronous behaviour. Otherwise it will
    /// execute synchronously, returning an already-completed task.
    /// </remarks>
    public override Task WriteNullAsync(Cancellation cancellation = default)
    {
        if (safeAsync)
        {
            return DoWriteNullAsync(cancellation);
        }

        return base.WriteNullAsync(cancellation);
    }

    Task DoWriteNullAsync(Cancellation cancellation) =>
        WriteValueInternalAsync(JsonToken.Null, JsonConvert.Null, cancellation);

    Task WriteDigitsAsync(ulong uvalue, bool negative, Cancellation cancellation)
    {
        if ((uvalue <= 9) & !negative)
        {
            return writer.WriteAsync((char) ('0' + uvalue), cancellation);
        }

        var length = WriteNumberToBuffer(uvalue, negative);
        return writer.WriteAsync(writeBuffer!, 0, length, cancellation);
    }

    Task WriteIntegerValueAsync(ulong uvalue, bool negative, Cancellation cancellation)
    {
        var task = InternalWriteValueAsync(JsonToken.Integer, cancellation);
        if (task.IsCompletedSuccessfully())
        {
            return WriteDigitsAsync(uvalue, negative, cancellation);
        }

        return WriteIntegerValueAsync(task, uvalue, negative, cancellation);
    }

    async Task WriteIntegerValueAsync(Task task, ulong uvalue, bool negative, Cancellation cancellation)
    {
        await task.ConfigureAwait(false);
        await WriteDigitsAsync(uvalue, negative, cancellation).ConfigureAwait(false);
    }

    Task WriteIntegerValueAsync(long value, Cancellation cancellation)
    {
        var negative = value < 0;
        if (negative)
        {
            value = -value;
        }

        return WriteIntegerValueAsync((ulong) value, negative, cancellation);
    }

    Task WriteIntegerValueAsync(ulong uvalue, Cancellation cancellation) =>
        WriteIntegerValueAsync(uvalue, false, cancellation);

    Task WriteEscapedStringAsync(string value, bool quote, Cancellation cancellation) =>
        JavaScriptUtils.WriteEscapedJavaScriptStringAsync(writer, value, quoteChar, quote, charEscapeFlags!, EscapeHandling, this, writeBuffer!, cancellation);

    /// <summary>
    /// Asynchronously writes the property name of a name/value pair of a JSON object.
    /// </summary>
    /// <remarks>
    /// Derived classes must override this method to get asynchronous behaviour. Otherwise it will
    /// execute synchronously, returning an already-completed task.
    /// </remarks>
    public override Task WritePropertyNameAsync(string name, Cancellation cancellation = default)
    {
        if (safeAsync)
        {
            return DoWritePropertyNameAsync(name, cancellation);
        }

        return base.WritePropertyNameAsync(name, cancellation);
    }

    Task DoWritePropertyNameAsync(string name, Cancellation cancellation)
    {
        var task = InternalWritePropertyNameAsync(name, cancellation);
        if (!task.IsCompletedSuccessfully())
        {
            return DoWritePropertyNameAsync(task, name, cancellation);
        }

        task = WriteEscapedStringAsync(name, QuoteName, cancellation);
        if (task.IsCompletedSuccessfully())
        {
            return writer.WriteAsync(':', cancellation);
        }

        return JavaScriptUtils.WriteCharAsync(task, writer, ':', cancellation);
    }

    async Task DoWritePropertyNameAsync(Task task, string name, Cancellation cancellation)
    {
        await task.ConfigureAwait(false);

        await WriteEscapedStringAsync(name, QuoteName, cancellation).ConfigureAwait(false);

        await writer.WriteAsync(':').ConfigureAwait(false);
    }

    /// <summary>
    /// Asynchronously writes the property name of a name/value pair of a JSON object.
    /// </summary>
    /// <param name="escape">A flag to indicate whether the text should be escaped when it is written as a JSON property name.</param>
    /// <remarks>
    /// Derived classes must override this method to get asynchronous behaviour. Otherwise it will
    /// execute synchronously, returning an already-completed task.
    /// </remarks>
    public override Task WritePropertyNameAsync(string name, bool escape, Cancellation cancellation = default)
    {
        if (safeAsync)
        {
            return DoWritePropertyNameAsync(name, escape, cancellation);
        }

        return base.WritePropertyNameAsync(name, escape, cancellation);
    }

    async Task DoWritePropertyNameAsync(string name, bool escape, Cancellation cancellation)
    {
        await InternalWritePropertyNameAsync(name, cancellation).ConfigureAwait(false);

        if (escape)
        {
            await WriteEscapedStringAsync(name, QuoteName, cancellation).ConfigureAwait(false);
        }
        else
        {
            if (QuoteName)
            {
                await writer.WriteAsync(quoteChar).ConfigureAwait(false);
            }

            await writer.WriteAsync(name, cancellation).ConfigureAwait(false);

            if (QuoteName)
            {
                await writer.WriteAsync(quoteChar).ConfigureAwait(false);
            }
        }

        await writer.WriteAsync(':').ConfigureAwait(false);
    }

    /// <summary>
    /// Asynchronously writes the beginning of a JSON array.
    /// </summary>
    /// <remarks>
    /// Derived classes must override this method to get asynchronous behaviour. Otherwise it will
    /// execute synchronously, returning an already-completed task.
    /// </remarks>
    public override Task WriteStartArrayAsync(Cancellation cancellation = default)
    {
        if (safeAsync)
        {
            return DoWriteStartArrayAsync(cancellation);
        }

        return base.WriteStartArrayAsync(cancellation);
    }

    Task DoWriteStartArrayAsync(Cancellation cancellation)
    {
        var task = InternalWriteStartAsync(JsonToken.StartArray, JsonContainerType.Array, cancellation);
        if (task.IsCompletedSuccessfully())
        {
            return writer.WriteAsync('[', cancellation);
        }

        return DoWriteStartArrayAsync(task, cancellation);
    }

    async Task DoWriteStartArrayAsync(Task task, Cancellation cancellation)
    {
        await task.ConfigureAwait(false);

        await writer.WriteAsync('[', cancellation).ConfigureAwait(false);
    }

    /// <summary>
    /// Asynchronously writes the beginning of a JSON object.
    /// </summary>
    /// <remarks>
    /// Derived classes must override this method to get asynchronous behaviour. Otherwise it will
    /// execute synchronously, returning an already-completed task.
    /// </remarks>
    public override Task WriteStartObjectAsync(Cancellation cancellation = default)
    {
        if (safeAsync)
        {
            return DoWriteStartObjectAsync(cancellation);
        }

        return base.WriteStartObjectAsync(cancellation);
    }

    Task DoWriteStartObjectAsync(Cancellation cancellation)
    {
        var task = InternalWriteStartAsync(JsonToken.StartObject, JsonContainerType.Object, cancellation);
        if (task.IsCompletedSuccessfully())
        {
            return writer.WriteAsync('{', cancellation);
        }

        return DoWriteStartObjectAsync(task, cancellation);
    }

    async Task DoWriteStartObjectAsync(Task task, Cancellation cancellation)
    {
        await task.ConfigureAwait(false);

        await writer.WriteAsync('{', cancellation).ConfigureAwait(false);
    }

    /// <summary>
    /// Asynchronously writes an undefined value.
    /// </summary>
    /// <remarks>
    /// Derived classes must override this method to get asynchronous behaviour. Otherwise it will
    /// execute synchronously, returning an already-completed task.
    /// </remarks>
    public override Task WriteUndefinedAsync(Cancellation cancellation = default)
    {
        if (safeAsync)
        {
            return DoWriteUndefinedAsync(cancellation);
        }

        return base.WriteUndefinedAsync(cancellation);
    }

    Task DoWriteUndefinedAsync(Cancellation cancellation)
    {
        var task = InternalWriteValueAsync(JsonToken.Undefined, cancellation);
        if (task.IsCompletedSuccessfully())
        {
            return writer.WriteAsync(JsonConvert.Undefined, cancellation);
        }

        return DoWriteUndefinedAsync(task, cancellation);
    }

    async Task DoWriteUndefinedAsync(Task task, Cancellation cancellation)
    {
        await task.ConfigureAwait(false);
        await writer.WriteAsync(JsonConvert.Undefined, cancellation).ConfigureAwait(false);
    }

    /// <summary>
    /// Asynchronously writes the given white space.
    /// </summary>
    /// <remarks>
    /// Derived classes must override this method to get asynchronous behaviour. Otherwise it will
    /// execute synchronously, returning an already-completed task.
    /// </remarks>
    public override Task WriteWhitespaceAsync(string ws, Cancellation cancellation = default)
    {
        if (safeAsync)
        {
            return DoWriteWhitespaceAsync(ws, cancellation);
        }

        return base.WriteWhitespaceAsync(ws, cancellation);
    }

    Task DoWriteWhitespaceAsync(string ws, Cancellation cancellation)
    {
        InternalWriteWhitespace(ws);
        return writer.WriteAsync(ws, cancellation);
    }

    /// <summary>
    /// Asynchronously writes a <see cref="Nullable{T}" /> of <see cref="bool" /> value.
    /// </summary>
    /// <remarks>
    /// Derived classes must override this method to get asynchronous behaviour. Otherwise it will
    /// execute synchronously, returning an already-completed task.
    /// </remarks>
    public override Task WriteValueAsync(bool value, Cancellation cancellation = default)
    {
        if (safeAsync)
        {
            return DoWriteValueAsync(value, cancellation);
        }

        return base.WriteValueAsync(value, cancellation);
    }

    Task DoWriteValueAsync(bool value, Cancellation cancellation) =>
        WriteValueInternalAsync(JsonToken.Boolean, JsonConvert.ToString(value), cancellation);

    /// <summary>
    /// Asynchronously writes a <see cref="bool" /> value.
    /// </summary>
    /// <remarks>
    /// Derived classes must override this method to get asynchronous behaviour. Otherwise it will
    /// execute synchronously, returning an already-completed task.
    /// </remarks>
    public override Task WriteValueAsync(bool? value, Cancellation cancellation = default)
    {
        if (safeAsync)
        {
            return DoWriteValueAsync(value, cancellation);
        }

        return base.WriteValueAsync(value, cancellation);
    }

    Task DoWriteValueAsync(bool? value, Cancellation cancellation)
    {
        if (value == null)
        {
            return DoWriteNullAsync(cancellation);
        }

        return DoWriteValueAsync(value.GetValueOrDefault(), cancellation);
    }

    /// <summary>
    /// Asynchronously writes a <see cref="byte" /> value.
    /// </summary>
    /// <remarks>
    /// Derived classes must override this method to get asynchronous behaviour. Otherwise it will
    /// execute synchronously, returning an already-completed task.
    /// </remarks>
    public override Task WriteValueAsync(byte value, Cancellation cancellation = default)
    {
        if (safeAsync)
        {
            return WriteIntegerValueAsync(value, cancellation);
        }

        return base.WriteValueAsync(value, cancellation);
    }

    /// <summary>
    /// Asynchronously writes a <see cref="Nullable{T}" /> of <see cref="byte" /> value.
    /// </summary>
    /// <param name="value">The <see cref="Nullable{T}" /> of <see cref="byte" /> value to write.</param>
    /// <remarks>
    /// Derived classes must override this method to get asynchronous behaviour. Otherwise it will
    /// execute synchronously, returning an already-completed task.
    /// </remarks>
    public override Task WriteValueAsync(byte? value, Cancellation cancellation = default)
    {
        if (safeAsync)
        {
            return DoWriteValueAsync(value, cancellation);
        }

        return base.WriteValueAsync(value, cancellation);
    }

    Task DoWriteValueAsync(byte? value, Cancellation cancellation)
    {
        if (value == null)
        {
            return DoWriteNullAsync(cancellation);
        }

        return WriteIntegerValueAsync(value.GetValueOrDefault(), cancellation);
    }

    /// <summary>
    /// Asynchronously writes a <see cref="byte" />[] value.
    /// </summary>
    /// <remarks>
    /// Derived classes must override this method to get asynchronous behaviour. Otherwise it will
    /// execute synchronously, returning an already-completed task.
    /// </remarks>
    public override Task WriteValueAsync(byte[]? value, Cancellation cancellation = default)
    {
        if (safeAsync)
        {
            return value == null ? WriteNullAsync(cancellation) : WriteValueNonNullAsync(value, cancellation);
        }

        return base.WriteValueAsync(value, cancellation);
    }

    async Task WriteValueNonNullAsync(byte[] value, Cancellation cancellation)
    {
        await InternalWriteValueAsync(JsonToken.Bytes, cancellation).ConfigureAwait(false);
        if (QuoteValue)
        {
            await writer.WriteAsync(quoteChar).ConfigureAwait(false);
        }
        await Base64Encoder.EncodeAsync(value, 0, value.Length, cancellation).ConfigureAwait(false);
        await Base64Encoder.FlushAsync(cancellation).ConfigureAwait(false);
        if (QuoteValue)
        {
            await writer.WriteAsync(quoteChar).ConfigureAwait(false);
        }
    }

    /// <summary>
    /// Asynchronously writes a <see cref="char" /> value.
    /// </summary>
    /// <remarks>
    /// Derived classes must override this method to get asynchronous behaviour. Otherwise it will
    /// execute synchronously, returning an already-completed task.
    /// </remarks>
    public override Task WriteValueAsync(char value, Cancellation cancellation = default)
    {
        if (safeAsync)
        {
            return DoWriteValueAsync(value, cancellation);
        }

        return base.WriteValueAsync(value, cancellation);
    }

    Task DoWriteValueAsync(char value, Cancellation cancellation) =>
        WriteValueInternalAsync(JsonToken.String, JsonConvert.ToString(value), cancellation);

    /// <summary>
    /// Asynchronously writes a <see cref="Nullable{T}" /> of <see cref="char" /> value.
    /// </summary>
    /// <remarks>
    /// Derived classes must override this method to get asynchronous behaviour. Otherwise it will
    /// execute synchronously, returning an already-completed task.
    /// </remarks>
    public override Task WriteValueAsync(char? value, Cancellation cancellation = default)
    {
        if (safeAsync)
        {
            return DoWriteValueAsync(value, cancellation);
        }

        return base.WriteValueAsync(value, cancellation);
    }

    Task DoWriteValueAsync(char? value, Cancellation cancellation)
    {
        if (value == null)
        {
            return DoWriteNullAsync(cancellation);
        }

        return DoWriteValueAsync(value.GetValueOrDefault(), cancellation);
    }

    /// <summary>
    /// Asynchronously writes a <see cref="DateTime" /> value.
    /// </summary>
    /// <remarks>
    /// Derived classes must override this method to get asynchronous behaviour. Otherwise it will
    /// execute synchronously, returning an already-completed task.
    /// </remarks>
    public override Task WriteValueAsync(DateTime value, Cancellation cancellation = default)
    {
        if (safeAsync)
        {
            return DoWriteValueAsync(value, cancellation);
        }

        return base.WriteValueAsync(value, cancellation);
    }

    async Task DoWriteValueAsync(DateTime value, Cancellation cancellation)
    {
        await InternalWriteValueAsync(JsonToken.Date, cancellation).ConfigureAwait(false);

        var length = WriteValueToBuffer(value);

        await writer.WriteAsync(writeBuffer!, 0, length, cancellation).ConfigureAwait(false);
    }

    /// <summary>
    /// Asynchronously writes a <see cref="Nullable{T}" /> of <see cref="DateTime" /> value.
    /// </summary>
    /// <remarks>
    /// Derived classes must override this method to get asynchronous behaviour. Otherwise it will
    /// execute synchronously, returning an already-completed task.
    /// </remarks>
    public override Task WriteValueAsync(DateTime? value, Cancellation cancellation = default)
    {
        if (safeAsync)
        {
            return DoWriteValueAsync(value, cancellation);
        }

        return base.WriteValueAsync(value, cancellation);
    }

    Task DoWriteValueAsync(DateTime? value, Cancellation cancellation)
    {
        if (value == null)
        {
            return DoWriteNullAsync(cancellation);
        }

        return DoWriteValueAsync(value.GetValueOrDefault(), cancellation);
    }

    /// <summary>
    /// Asynchronously writes a <see cref="DateTimeOffset" /> value.
    /// </summary>
    /// <remarks>
    /// Derived classes must override this method to get asynchronous behaviour. Otherwise it will
    /// execute synchronously, returning an already-completed task.
    /// </remarks>
    public override Task WriteValueAsync(DateTimeOffset value, Cancellation cancellation = default)
    {
        if (safeAsync)
        {
            return DoWriteValueAsync(value, cancellation);
        }

        return base.WriteValueAsync(value, cancellation);
    }

    async Task DoWriteValueAsync(DateTimeOffset value, Cancellation cancellation)
    {
        await InternalWriteValueAsync(JsonToken.Date, cancellation).ConfigureAwait(false);

        var length = WriteValueToBuffer(value);

        await writer.WriteAsync(writeBuffer!, 0, length, cancellation).ConfigureAwait(false);
    }

    /// <summary>
    /// Asynchronously writes a <see cref="Nullable{T}" /> of <see cref="DateTimeOffset" /> value.
    /// </summary>
    /// <remarks>
    /// Derived classes must override this method to get asynchronous behaviour. Otherwise it will
    /// execute synchronously, returning an already-completed task.
    /// </remarks>
    public override Task WriteValueAsync(DateTimeOffset? value, Cancellation cancellation = default)
    {
        if (safeAsync)
        {
            return DoWriteValueAsync(value, cancellation);
        }

        return base.WriteValueAsync(value, cancellation);
    }

    Task DoWriteValueAsync(DateTimeOffset? value, Cancellation cancellation)
    {
        if (value == null)
        {
            return DoWriteNullAsync(cancellation);
        }

        return DoWriteValueAsync(value.GetValueOrDefault(), cancellation);
    }

    /// <summary>
    /// Asynchronously writes a <see cref="decimal" /> value.
    /// </summary>
    /// <remarks>
    /// Derived classes must override this method to get asynchronous behaviour. Otherwise it will
    /// execute synchronously, returning an already-completed task.
    /// </remarks>
    public override Task WriteValueAsync(decimal value, Cancellation cancellation = default)
    {
        if (safeAsync)
        {
            return DoWriteValueAsync(value, cancellation);
        }

        return base.WriteValueAsync(value, cancellation);
    }

    Task DoWriteValueAsync(decimal value, Cancellation cancellation) =>
        WriteValueInternalAsync(JsonToken.Float, JsonConvert.ToString(value), cancellation);

    /// <summary>
    /// Asynchronously writes a <see cref="Nullable{T}" /> of <see cref="decimal" /> value.
    /// </summary>
    /// <remarks>
    /// Derived classes must override this method to get asynchronous behaviour. Otherwise it will
    /// execute synchronously, returning an already-completed task.
    /// </remarks>
    public override Task WriteValueAsync(decimal? value, Cancellation cancellation = default)
    {
        if (safeAsync)
        {
            return DoWriteValueAsync(value, cancellation);
        }

        return base.WriteValueAsync(value, cancellation);
    }

    Task DoWriteValueAsync(decimal? value, Cancellation cancellation)
    {
        if (value == null)
        {
            return DoWriteNullAsync(cancellation);
        }

        return DoWriteValueAsync(value.GetValueOrDefault(), cancellation);
    }

    /// <summary>
    /// Asynchronously writes a <see cref="double" /> value.
    /// </summary>
    /// <remarks>
    /// Derived classes must override this method to get asynchronous behaviour. Otherwise it will
    /// execute synchronously, returning an already-completed task.
    /// </remarks>
    public override Task WriteValueAsync(double value, Cancellation cancellation = default)
    {
        if (safeAsync)
        {
            return WriteValueAsync(value, false, cancellation);
        }

        return base.WriteValueAsync(value, cancellation);
    }

    Task WriteValueAsync(double value, bool nullable, Cancellation cancellation)
    {
        var convertedValue = JsonConvert.ToString(value, FloatFormatHandling, QuoteChar, nullable);
        return WriteValueInternalAsync(JsonToken.Float, convertedValue, cancellation);
    }

    /// <summary>
    /// Asynchronously writes a <see cref="Nullable{T}" /> of <see cref="double" /> value.
    /// </summary>
    /// <remarks>
    /// Derived classes must override this method to get asynchronous behaviour. Otherwise it will
    /// execute synchronously, returning an already-completed task.
    /// </remarks>
    public override Task WriteValueAsync(double? value, Cancellation cancellation = default)
    {
        if (safeAsync)
        {
            if (value.HasValue)
            {
                return WriteValueAsync(value.GetValueOrDefault(), true, cancellation);
            }

            return WriteNullAsync(cancellation);
        }

        return base.WriteValueAsync(value, cancellation);
    }

    /// <summary>
    /// Asynchronously writes a <see cref="float" /> value.
    /// </summary>
    /// <remarks>
    /// Derived classes must override this method to get asynchronous behaviour. Otherwise it will
    /// execute synchronously, returning an already-completed task.
    /// </remarks>
    public override Task WriteValueAsync(float value, Cancellation cancellation = default)
    {
        if (safeAsync)
        {
            return WriteValueAsync(value, false, cancellation);
        }

        return base.WriteValueAsync(value, cancellation);
    }

    Task WriteValueAsync(float value, bool nullable, Cancellation cancellation) =>
        WriteValueInternalAsync(JsonToken.Float, JsonConvert.ToString(value, FloatFormatHandling, QuoteChar, nullable), cancellation);

    /// <summary>
    /// Asynchronously writes a <see cref="Nullable{T}" /> of <see cref="float" /> value.
    /// </summary>
    /// <remarks>
    /// Derived classes must override this method to get asynchronous behaviour. Otherwise it will
    /// execute synchronously, returning an already-completed task.
    /// </remarks>
    public override Task WriteValueAsync(float? value, Cancellation cancellation = default)
    {
        if (safeAsync)
        {
            if (value.HasValue)
            {
                return WriteValueAsync(value.GetValueOrDefault(), true, cancellation);
            }

            return WriteNullAsync(cancellation);
        }

        return base.WriteValueAsync(value, cancellation);
    }

    /// <summary>
    /// Asynchronously writes a <see cref="Guid" /> value.
    /// </summary>
    /// <remarks>
    /// Derived classes must override this method to get asynchronous behaviour. Otherwise it will
    /// execute synchronously, returning an already-completed task.
    /// </remarks>
    public override Task WriteValueAsync(Guid value, Cancellation cancellation = default)
    {
        if (safeAsync)
        {
            return DoWriteValueAsync(value, cancellation);
        }

        return base.WriteValueAsync(value, cancellation);
    }

    async Task DoWriteValueAsync(Guid value, Cancellation cancellation)
    {
        await InternalWriteValueAsync(JsonToken.String, cancellation).ConfigureAwait(false);

        if (QuoteValue)
        {
            await writer.WriteAsync(quoteChar).ConfigureAwait(false);
        }

        await writer.WriteAsync(value.ToString("D", InvariantCulture), cancellation).ConfigureAwait(false);
        if (QuoteValue)
        {
            await writer.WriteAsync(quoteChar).ConfigureAwait(false);
        }
    }

    /// <summary>
    /// Asynchronously writes a <see cref="Nullable{T}" /> of <see cref="Guid" /> value.
    /// </summary>
    /// <remarks>
    /// Derived classes must override this method to get asynchronous behaviour. Otherwise it will
    /// execute synchronously, returning an already-completed task.
    /// </remarks>
    public override Task WriteValueAsync(Guid? value, Cancellation cancellation = default)
    {
        if (safeAsync)
        {
            return DoWriteValueAsync(value, cancellation);
        }

        return base.WriteValueAsync(value, cancellation);
    }

    Task DoWriteValueAsync(Guid? value, Cancellation cancellation)
    {
        if (value == null)
        {
            return DoWriteNullAsync(cancellation);
        }

        return DoWriteValueAsync(value.GetValueOrDefault(), cancellation);
    }

    /// <summary>
    /// Asynchronously writes a <see cref="int" /> value.
    /// </summary>
    /// <remarks>
    /// Derived classes must override this method to get asynchronous behaviour. Otherwise it will
    /// execute synchronously, returning an already-completed task.
    /// </remarks>
    public override Task WriteValueAsync(int value, Cancellation cancellation = default)
    {
        if (safeAsync)
        {
            return WriteIntegerValueAsync(value, cancellation);
        }

        return base.WriteValueAsync(value, cancellation);
    }

    /// <summary>
    /// Asynchronously writes a <see cref="Nullable{T}" /> of <see cref="int" /> value.
    /// </summary>
    /// <remarks>
    /// Derived classes must override this method to get asynchronous behaviour. Otherwise it will
    /// execute synchronously, returning an already-completed task.
    /// </remarks>
    public override Task WriteValueAsync(int? value, Cancellation cancellation = default)
    {
        if (safeAsync)
        {
            return DoWriteValueAsync(value, cancellation);
        }

        return base.WriteValueAsync(value, cancellation);
    }

    Task DoWriteValueAsync(int? value, Cancellation cancellation)
    {
        if (value == null)
        {
            return DoWriteNullAsync(cancellation);
        }

        return WriteIntegerValueAsync(value.GetValueOrDefault(), cancellation);
    }

    /// <summary>
    /// Asynchronously writes a <see cref="long" /> value.
    /// </summary>
    /// <remarks>
    /// Derived classes must override this method to get asynchronous behaviour. Otherwise it will
    /// execute synchronously, returning an already-completed task.
    /// </remarks>
    public override Task WriteValueAsync(long value, Cancellation cancellation = default)
    {
        if (safeAsync)
        {
            return WriteIntegerValueAsync(value, cancellation);
        }

        return base.WriteValueAsync(value, cancellation);
    }

    /// <summary>
    /// Asynchronously writes a <see cref="Nullable{T}" /> of <see cref="long" /> value.
    /// </summary>
    /// <remarks>
    /// Derived classes must override this method to get asynchronous behaviour. Otherwise it will
    /// execute synchronously, returning an already-completed task.
    /// </remarks>
    public override Task WriteValueAsync(long? value, Cancellation cancellation = default)
    {
        if (safeAsync)
        {
            return DoWriteValueAsync(value, cancellation);
        }

        return base.WriteValueAsync(value, cancellation);
    }

    Task DoWriteValueAsync(long? value, Cancellation cancellation) =>
        value == null ? DoWriteNullAsync(cancellation) : WriteIntegerValueAsync(value.GetValueOrDefault(), cancellation);

    internal Task WriteValueAsync(BigInteger value, Cancellation cancellation) =>
        WriteValueInternalAsync(JsonToken.Integer, value.ToString(InvariantCulture), cancellation);

    /// <summary>
    /// Asynchronously writes a <see cref="object" /> value.
    /// </summary>
    /// <remarks>
    /// Derived classes must override this method to get asynchronous behaviour. Otherwise it will
    /// execute synchronously, returning an already-completed task.
    /// </remarks>
    public override Task WriteValueAsync(object? value, Cancellation cancellation = default)
    {
        if (safeAsync)
        {
            if (value == null)
            {
                return WriteNullAsync(cancellation);
            }

            if (value is BigInteger i)
            {
                return WriteValueAsync(i, cancellation);
            }

            return WriteValueAsync(this, ConvertUtils.GetTypeCode(value.GetType()), value, cancellation);
        }

        return base.WriteValueAsync(value, cancellation);
    }

    /// <summary>
    /// Asynchronously writes a <see cref="sbyte" /> value.
    /// </summary>
    /// <remarks>
    /// Derived classes must override this method to get asynchronous behaviour. Otherwise it will
    /// execute synchronously, returning an already-completed task.
    /// </remarks>
    public override Task WriteValueAsync(sbyte value, Cancellation cancellation = default)
    {
        if (safeAsync)
        {
            return WriteIntegerValueAsync(value, cancellation);
        }

        return base.WriteValueAsync(value, cancellation);
    }

    /// <summary>
    /// Asynchronously writes a <see cref="Nullable{T}" /> of <see cref="sbyte" /> value.
    /// </summary>
    /// <remarks>
    /// Derived classes must override this method to get asynchronous behaviour. Otherwise it will
    /// execute synchronously, returning an already-completed task.
    /// </remarks>
    public override Task WriteValueAsync(sbyte? value, Cancellation cancellation = default)
    {
        if (safeAsync)
        {
            return DoWriteValueAsync(value, cancellation);
        }

        return base.WriteValueAsync(value, cancellation);
    }

    Task DoWriteValueAsync(sbyte? value, Cancellation cancellation)
    {
        if (value == null)
        {
            return DoWriteNullAsync(cancellation);
        }

        return WriteIntegerValueAsync(value.GetValueOrDefault(), cancellation);
    }

    /// <summary>
    /// Asynchronously writes a <see cref="short" /> value.
    /// </summary>
    /// <remarks>
    /// Derived classes must override this method to get asynchronous behaviour. Otherwise it will
    /// execute synchronously, returning an already-completed task.
    /// </remarks>
    public override Task WriteValueAsync(short value, Cancellation cancellation = default)
    {
        if (safeAsync)
        {
            return WriteIntegerValueAsync(value, cancellation);
        }

        return base.WriteValueAsync(value, cancellation);
    }

    /// <summary>
    /// Asynchronously writes a <see cref="Nullable{T}" /> of <see cref="short" /> value.
    /// </summary>
    /// <remarks>
    /// Derived classes must override this method to get asynchronous behaviour. Otherwise it will
    /// execute synchronously, returning an already-completed task.
    /// </remarks>
    public override Task WriteValueAsync(short? value, Cancellation cancellation = default)
    {
        if (safeAsync)
        {
            return DoWriteValueAsync(value, cancellation);
        }

        return base.WriteValueAsync(value, cancellation);
    }

    Task DoWriteValueAsync(short? value, Cancellation cancellation)
    {
        if (value == null)
        {
            return DoWriteNullAsync(cancellation);
        }

        return WriteIntegerValueAsync(value.GetValueOrDefault(), cancellation);
    }

    /// <summary>
    /// Asynchronously writes a <see cref="string" /> value.
    /// </summary>
    /// <remarks>
    /// Derived classes must override this method to get asynchronous behaviour. Otherwise it will
    /// execute synchronously, returning an already-completed task.
    /// </remarks>
    public override Task WriteValueAsync(string? value, Cancellation cancellation = default)
    {
        if (safeAsync)
        {
            return DoWriteValueAsync(value, cancellation);
        }

        return base.WriteValueAsync(value, cancellation);
    }

    Task DoWriteValueAsync(string? value, Cancellation cancellation)
    {
        var task = InternalWriteValueAsync(JsonToken.String, cancellation);
        if (task.IsCompletedSuccessfully())
        {
            if (value == null)
            {
                return writer.WriteAsync(JsonConvert.Null, cancellation);
            }

            return WriteEscapedStringAsync(value, QuoteValue, cancellation);
        }

        return DoWriteValueAsync(task, value, cancellation);
    }

    async Task DoWriteValueAsync(Task task, string? value, Cancellation cancellation)
    {
        await task.ConfigureAwait(false);

        if (value == null)
        {
            await writer.WriteAsync(JsonConvert.Null, cancellation).ConfigureAwait(false);
            return;
        }

        await WriteEscapedStringAsync(value, QuoteValue, cancellation).ConfigureAwait(false);
    }

    /// <summary>
    /// Asynchronously writes a <see cref="TimeSpan" /> value.
    /// </summary>
    /// <remarks>
    /// Derived classes must override this method to get asynchronous behaviour. Otherwise it will
    /// execute synchronously, returning an already-completed task.
    /// </remarks>
    public override Task WriteValueAsync(TimeSpan value, Cancellation cancellation = default)
    {
        if (safeAsync)
        {
            return DoWriteValueAsync(value, cancellation);
        }

        return base.WriteValueAsync(value, cancellation);
    }

    async Task DoWriteValueAsync(TimeSpan value, Cancellation cancellation)
    {
        await InternalWriteValueAsync(JsonToken.String, cancellation).ConfigureAwait(false);
        if (QuoteValue)
        {
            await writer.WriteAsync(quoteChar, cancellation).ConfigureAwait(false);
        }

        await writer.WriteAsync(value.ToString(null, InvariantCulture), cancellation).ConfigureAwait(false);
        if (QuoteValue)
        {
            await writer.WriteAsync(quoteChar, cancellation).ConfigureAwait(false);
        }
    }

    /// <summary>
    /// Asynchronously writes a <see cref="Nullable{T}" /> of <see cref="TimeSpan" /> value.
    /// </summary>
    /// <remarks>
    /// Derived classes must override this method to get asynchronous behaviour. Otherwise it will
    /// execute synchronously, returning an already-completed task.
    /// </remarks>
    public override Task WriteValueAsync(TimeSpan? value, Cancellation cancellation = default)
    {
        if (safeAsync)
        {
            return DoWriteValueAsync(value, cancellation);
        }

        return base.WriteValueAsync(value, cancellation);
    }

    Task DoWriteValueAsync(TimeSpan? value, Cancellation cancellation) =>
        value == null ? DoWriteNullAsync(cancellation) : DoWriteValueAsync(value.GetValueOrDefault(), cancellation);

    /// <summary>
    /// Asynchronously writes a <see cref="uint" /> value.
    /// </summary>
    /// <remarks>
    /// Derived classes must override this method to get asynchronous behaviour. Otherwise it will
    /// execute synchronously, returning an already-completed task.
    /// </remarks>
    public override Task WriteValueAsync(uint value, Cancellation cancellation = default)
    {
        if (safeAsync)
        {
            return WriteIntegerValueAsync(value, cancellation);
        }

        return base.WriteValueAsync(value, cancellation);
    }

    /// <summary>
    /// Asynchronously writes a <see cref="Nullable{T}" /> of <see cref="uint" /> value.
    /// </summary>
    /// <remarks>
    /// Derived classes must override this method to get asynchronous behaviour. Otherwise it will
    /// execute synchronously, returning an already-completed task.
    /// </remarks>
    public override Task WriteValueAsync(uint? value, Cancellation cancellation = default)
    {
        if (safeAsync)
        {
            return DoWriteValueAsync(value, cancellation);
        }

        return base.WriteValueAsync(value, cancellation);
    }

    Task DoWriteValueAsync(uint? value, Cancellation cancellation)
    {
        if (value == null)
        {
            return DoWriteNullAsync(cancellation);
        }

        return WriteIntegerValueAsync(value.GetValueOrDefault(), cancellation);
    }

    /// <summary>
    /// Asynchronously writes a <see cref="ulong" /> value.
    /// </summary>
    /// <remarks>
    /// Derived classes must override this method to get asynchronous behaviour. Otherwise it will
    /// execute synchronously, returning an already-completed task.
    /// </remarks>
    public override Task WriteValueAsync(ulong value, Cancellation cancellation = default)
    {
        if (safeAsync)
        {
            return WriteIntegerValueAsync(value, cancellation);
        }

        return base.WriteValueAsync(value, cancellation);
    }

    /// <summary>
    /// Asynchronously writes a <see cref="Nullable{T}" /> of <see cref="ulong" /> value.
    /// </summary>
    /// <remarks>
    /// Derived classes must override this method to get asynchronous behaviour. Otherwise it will
    /// execute synchronously, returning an already-completed task.
    /// </remarks>
    public override Task WriteValueAsync(ulong? value, Cancellation cancellation = default)
    {
        if (safeAsync)
        {
            return DoWriteValueAsync(value, cancellation);
        }

        return base.WriteValueAsync(value, cancellation);
    }

    Task DoWriteValueAsync(ulong? value, Cancellation cancellation)
    {
        if (value == null)
        {
            return DoWriteNullAsync(cancellation);
        }

        return WriteIntegerValueAsync(value.GetValueOrDefault(), cancellation);
    }

    /// <summary>
    /// Asynchronously writes a <see cref="Uri" /> value.
    /// </summary>
    /// <remarks>
    /// Derived classes must override this method to get asynchronous behaviour. Otherwise it will
    /// execute synchronously, returning an already-completed task.
    /// </remarks>
    public override Task WriteValueAsync(Uri? value, Cancellation cancellation = default)
    {
        if (safeAsync)
        {
            if (value == null)
            {
                return WriteNullAsync(cancellation);
            }

            return WriteValueNotNullAsync(value, cancellation);
        }

        return base.WriteValueAsync(value, cancellation);
    }

    Task WriteValueNotNullAsync(Uri value, Cancellation cancellation)
    {
        var task = InternalWriteValueAsync(JsonToken.String, cancellation);
        if (task.IsCompletedSuccessfully())
        {
            return WriteEscapedStringAsync(value.OriginalString, QuoteValue, cancellation);
        }

        return WriteValueNotNullAsync(task, value, cancellation);
    }

    async Task WriteValueNotNullAsync(Task task, Uri value, Cancellation cancellation)
    {
        await task.ConfigureAwait(false);
        await WriteEscapedStringAsync(value.OriginalString, QuoteValue, cancellation).ConfigureAwait(false);
    }

    /// <summary>
    /// Asynchronously writes a <see cref="ushort" /> value.
    /// </summary>
    /// <remarks>
    /// Derived classes must override this method to get asynchronous behaviour. Otherwise it will
    /// execute synchronously, returning an already-completed task.
    /// </remarks>
    public override Task WriteValueAsync(ushort value, Cancellation cancellation = default)
    {
        if (safeAsync)
        {
            return WriteIntegerValueAsync(value, cancellation);
        }

        return base.WriteValueAsync(value, cancellation);
    }

    /// <summary>
    /// Asynchronously writes a <see cref="Nullable{T}" /> of <see cref="ushort" /> value.
    /// </summary>
    /// <remarks>
    /// Derived classes must override this method to get asynchronous behaviour. Otherwise it will
    /// execute synchronously, returning an already-completed task.
    /// </remarks>
    public override Task WriteValueAsync(ushort? value, Cancellation cancellation = default)
    {
        if (safeAsync)
        {
            return DoWriteValueAsync(value, cancellation);
        }

        return base.WriteValueAsync(value, cancellation);
    }

    Task DoWriteValueAsync(ushort? value, Cancellation cancellation)
    {
        if (value == null)
        {
            return DoWriteNullAsync(cancellation);
        }

        return WriteIntegerValueAsync(value.GetValueOrDefault(), cancellation);
    }

    /// <summary>
    /// Asynchronously writes a comment <c>/*...*/</c> containing the specified text.
    /// </summary>
    /// <remarks>
    /// Derived classes must override this method to get asynchronous behaviour. Otherwise it will
    /// execute synchronously, returning an already-completed task.
    /// </remarks>
    public override Task WriteCommentAsync(string? text, Cancellation cancellation = default)
    {
        if (safeAsync)
        {
            return DoWriteCommentAsync(text, cancellation);
        }

        return base.WriteCommentAsync(text, cancellation);
    }

    async Task DoWriteCommentAsync(string? text, Cancellation cancellation)
    {
        await InternalWriteCommentAsync(cancellation).ConfigureAwait(false);
        await writer.WriteAsync("/*", cancellation).ConfigureAwait(false);
        await writer.WriteAsync(text ?? string.Empty, cancellation).ConfigureAwait(false);
        await writer.WriteAsync("*/", cancellation).ConfigureAwait(false);
    }

    /// <summary>
    /// Asynchronously writes the end of an array.
    /// </summary>
    /// <remarks>
    /// Derived classes must override this method to get asynchronous behaviour. Otherwise it will
    /// execute synchronously, returning an already-completed task.
    /// </remarks>
    public override Task WriteEndArrayAsync(Cancellation cancellation = default)
    {
        if (safeAsync)
        {
            return InternalWriteEndAsync(JsonContainerType.Array, cancellation);
        }

        return base.WriteEndArrayAsync(cancellation);
    }

    /// <summary>
    /// Asynchronously writes the end of a JSON object.
    /// </summary>
    /// <remarks>
    /// Derived classes must override this method to get asynchronous behaviour. Otherwise it will
    /// execute synchronously, returning an already-completed task.
    /// </remarks>
    public override Task WriteEndObjectAsync(Cancellation cancellation = default)
    {
        if (safeAsync)
        {
            return InternalWriteEndAsync(JsonContainerType.Object, cancellation);
        }

        return base.WriteEndObjectAsync(cancellation);
    }

    /// <summary>
    /// Asynchronously writes raw JSON where a value is expected and updates the writer's state.
    /// </summary>
    /// <remarks>
    /// Derived classes must override this method to get asynchronous behaviour. Otherwise it will
    /// execute synchronously, returning an already-completed task.
    /// </remarks>
    public override Task WriteRawValueAsync(string? json, Cancellation cancellation = default)
    {
        if (safeAsync)
        {
            return DoWriteRawValueAsync(json, cancellation);
        }

        return base.WriteRawValueAsync(json, cancellation);
    }

    Task DoWriteRawValueAsync(string? json, Cancellation cancellation)
    {
        UpdateScopeWithFinishedValue();
        var task = AutoCompleteAsync(JsonToken.Undefined, cancellation);
        if (task.IsCompletedSuccessfully())
        {
            return WriteRawAsync(json, cancellation);
        }

        return DoWriteRawValueAsync(task, json, cancellation);
    }

    async Task DoWriteRawValueAsync(Task task, string? json, Cancellation cancellation)
    {
        await task.ConfigureAwait(false);
        await WriteRawAsync(json, cancellation).ConfigureAwait(false);
    }

}