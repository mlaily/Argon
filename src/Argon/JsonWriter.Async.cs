// Copyright (c) 2007 James Newton-King. All rights reserved.
// Use of this source code is governed by The MIT License,
// as found in the license.md file.

// ReSharper disable NullableWarningSuppressionIsUsed
// ReSharper disable RedundantSuppressNullableWarningExpression
namespace Argon;

public abstract partial class JsonWriter
#if NET5_0_OR_GREATER
        : IAsyncDisposable
{
    async ValueTask IAsyncDisposable.DisposeAsync()
    {
        if (currentState != State.Closed)
        {
            await CloseAsync().ConfigureAwait(false);
        }
    }
#else
{
#endif

    internal Task AutoCompleteAsync(JsonToken tokenBeingWritten, Cancellation cancellation)
    {
        var oldState = currentState;

        // gets new state based on the current state and what is being written
        var newState = stateArray[(int) tokenBeingWritten][(int) oldState];

        if (newState == State.Error)
        {
            throw JsonWriterException.Create(this, $"Token {tokenBeingWritten} in state {oldState} would result in an invalid JSON object.");
        }

        currentState = newState;

        if (Formatting == Formatting.Indented)
        {
            switch (oldState)
            {
                case State.Start:
                    break;
                case State.Property:
                    return WriteIndentSpaceAsync(cancellation);
                case State.ArrayStart:
                    return WriteIndentAsync(cancellation);
                case State.Array:
                    return tokenBeingWritten == JsonToken.Comment ? WriteIndentAsync(cancellation) : AutoCompleteAsync(cancellation);
                case State.Object:
                    switch (tokenBeingWritten)
                    {
                        case JsonToken.Comment:
                            break;
                        case JsonToken.PropertyName:
                            return AutoCompleteAsync(cancellation);
                        default:
                            return WriteValueDelimiterAsync(cancellation);
                    }

                    break;
                default:
                    if (tokenBeingWritten == JsonToken.PropertyName)
                    {
                        return WriteIndentAsync(cancellation);
                    }

                    break;
            }
        }
        else if (tokenBeingWritten != JsonToken.Comment)
        {
            switch (oldState)
            {
                case State.Object:
                case State.Array:
                    return WriteValueDelimiterAsync(cancellation);
            }
        }

        return Task.CompletedTask;
    }

    async Task AutoCompleteAsync(Cancellation cancellation)
    {
        await WriteValueDelimiterAsync(cancellation).ConfigureAwait(false);
        await WriteIndentAsync(cancellation).ConfigureAwait(false);
    }

    /// <summary>
    /// Asynchronously closes this writer.
    /// If <see cref="JsonWriter.CloseOutput" /> is set to <c>true</c>, the destination is also closed.
    /// </summary>
    public virtual Task CloseAsync(Cancellation cancellation = default)
    {
        if (cancellation.IsCancellationRequested)
        {
            return cancellation.FromCanceled();
        }

        Close();
        return Task.CompletedTask;
    }

    /// <summary>
    /// Asynchronously flushes whatever is in the buffer to the destination and also flushes the destination.
    /// </summary>
    public virtual Task FlushAsync(Cancellation cancellation = default)
    {
        if (cancellation.IsCancellationRequested)
        {
            return cancellation.FromCanceled();
        }

        Flush();
        return Task.CompletedTask;
    }

    /// <summary>
    /// Asynchronously writes the specified end token.
    /// </summary>
    protected virtual Task WriteEndAsync(JsonToken token, Cancellation cancellation)
    {
        if (cancellation.IsCancellationRequested)
        {
            return cancellation.FromCanceled();
        }

        WriteEnd(token);
        return Task.CompletedTask;
    }

    /// <summary>
    /// Asynchronously writes indent characters.
    /// </summary>
    protected virtual Task WriteIndentAsync(Cancellation cancellation)
    {
        if (cancellation.IsCancellationRequested)
        {
            return cancellation.FromCanceled();
        }

        WriteIndent();
        return Task.CompletedTask;
    }

    /// <summary>
    /// Asynchronously writes the JSON value delimiter.
    /// </summary>
    protected virtual Task WriteValueDelimiterAsync(Cancellation cancellation)
    {
        if (cancellation.IsCancellationRequested)
        {
            return cancellation.FromCanceled();
        }

        WriteValueDelimiter();
        return Task.CompletedTask;
    }

    /// <summary>
    /// Asynchronously writes an indent space.
    /// </summary>
    protected virtual Task WriteIndentSpaceAsync(Cancellation cancellation)
    {
        if (cancellation.IsCancellationRequested)
        {
            return cancellation.FromCanceled();
        }

        WriteIndentSpace();
        return Task.CompletedTask;
    }

    /// <summary>
    /// Asynchronously writes raw JSON without changing the writer's state.
    /// </summary>
    public virtual Task WriteRawAsync(string? json, Cancellation cancellation = default)
    {
        if (cancellation.IsCancellationRequested)
        {
            return cancellation.FromCanceled();
        }

        WriteRaw(json);
        return Task.CompletedTask;
    }

    /// <summary>
    /// Asynchronously writes the end of the current JSON object or array.
    /// </summary>
    public virtual Task WriteEndAsync(Cancellation cancellation = default)
    {
        if (cancellation.IsCancellationRequested)
        {
            return cancellation.FromCanceled();
        }

        WriteEnd();
        return Task.CompletedTask;
    }

    internal Task WriteEndInternalAsync(Cancellation cancellation)
    {
        var type = Peek();
        switch (type)
        {
            case JsonContainerType.Object:
                return WriteEndObjectAsync(cancellation);
            case JsonContainerType.Array:
                return WriteEndArrayAsync(cancellation);
            default:
                if (cancellation.IsCancellationRequested)
                {
                    return cancellation.FromCanceled();
                }

                throw JsonWriterException.Create(this, $"Unexpected type when writing end: {type}");
        }
    }

    internal Task InternalWriteEndAsync(JsonContainerType type, Cancellation cancellation)
    {
        if (cancellation.IsCancellationRequested)
        {
            return cancellation.FromCanceled();
        }

        var levelsToComplete = CalculateLevelsToComplete(type);
        while (levelsToComplete-- > 0)
        {
            var token = GetCloseTokenForType(Pop());

            Task t;
            if (currentState == State.Property)
            {
                t = WriteNullAsync(cancellation);
                if (!t.IsCompletedSuccessfully())
                {
                    return AwaitProperty(t, levelsToComplete, token, cancellation);
                }
            }

            if (Formatting == Formatting.Indented)
            {
                if (currentState != State.ObjectStart && currentState != State.ArrayStart)
                {
                    t = WriteIndentAsync(cancellation);
                    if (!t.IsCompletedSuccessfully())
                    {
                        return AwaitIndent(t, levelsToComplete, token, cancellation);
                    }
                }
            }

            t = WriteEndAsync(token, cancellation);
            if (!t.IsCompletedSuccessfully())
            {
                return AwaitEnd(t, levelsToComplete, cancellation);
            }

            UpdateCurrentState();
        }

        return Task.CompletedTask;
    }

    async Task AwaitIndent(Task task, int levelsToComplete, JsonToken token, Cancellation cancellation)
    {
        await task.ConfigureAwait(false);

        //  Finish current loop

        await WriteEndAsync(token, cancellation).ConfigureAwait(false);

        UpdateCurrentState();

        await AwaitRemaining(levelsToComplete, cancellation).ConfigureAwait(false);
    }

    async Task AwaitProperty(Task task, int levelsToComplete, JsonToken token, Cancellation cancellation)
    {
        await task.ConfigureAwait(false);

        //  Finish current loop
        if (Formatting == Formatting.Indented)
        {
            if (currentState != State.ObjectStart && currentState != State.ArrayStart)
            {
                await WriteIndentAsync(cancellation).ConfigureAwait(false);
            }
        }

        await WriteEndAsync(token, cancellation).ConfigureAwait(false);

        UpdateCurrentState();

        await AwaitRemaining(levelsToComplete, cancellation).ConfigureAwait(false);
    }

    async Task AwaitEnd(Task task, int levelsToComplete, Cancellation cancellation)
    {
        await task.ConfigureAwait(false);

        //  Finish current loop

        UpdateCurrentState();

        await AwaitRemaining(levelsToComplete, cancellation).ConfigureAwait(false);
    }

    async Task AwaitRemaining(int levelsToComplete, Cancellation cancellation)
    {
        while (levelsToComplete-- > 0)
        {
            var token = GetCloseTokenForType(Pop());

            if (currentState == State.Property)
            {
                await WriteNullAsync(cancellation).ConfigureAwait(false);
            }

            if (Formatting == Formatting.Indented)
            {
                if (currentState != State.ObjectStart && currentState != State.ArrayStart)
                {
                    await WriteIndentAsync(cancellation).ConfigureAwait(false);
                }
            }

            await WriteEndAsync(token, cancellation).ConfigureAwait(false);

            UpdateCurrentState();
        }
    }

    /// <summary>
    /// Asynchronously writes the end of an array.
    /// </summary>
    public virtual Task WriteEndArrayAsync(Cancellation cancellation = default)
    {
        if (cancellation.IsCancellationRequested)
        {
            return cancellation.FromCanceled();
        }

        WriteEndArray();
        return Task.CompletedTask;
    }

    /// <summary>
    /// Asynchronously writes the end of a JSON object.
    /// </summary>
    public virtual Task WriteEndObjectAsync(Cancellation cancellation = default)
    {
        if (cancellation.IsCancellationRequested)
        {
            return cancellation.FromCanceled();
        }

        WriteEndObject();
        return Task.CompletedTask;
    }

    /// <summary>
    /// Asynchronously writes a null value.
    /// </summary>
    public virtual Task WriteNullAsync(Cancellation cancellation = default)
    {
        if (cancellation.IsCancellationRequested)
        {
            return cancellation.FromCanceled();
        }

        WriteNull();
        return Task.CompletedTask;
    }

    /// <summary>
    /// Asynchronously writes the property name of a name/value pair of a JSON object.
    /// </summary>
    public virtual Task WritePropertyNameAsync(string name, Cancellation cancellation = default)
    {
        if (cancellation.IsCancellationRequested)
        {
            return cancellation.FromCanceled();
        }

        WritePropertyName(name);
        return Task.CompletedTask;
    }

    /// <summary>
    /// Asynchronously writes the property name of a name/value pair of a JSON object.
    /// </summary>
    /// <param name="escape">A flag to indicate whether the text should be escaped when it is written as a JSON property name.</param>
    public virtual Task WritePropertyNameAsync(string name, bool escape, Cancellation cancellation = default)
    {
        if (cancellation.IsCancellationRequested)
        {
            return cancellation.FromCanceled();
        }

        WritePropertyName(name, escape);
        return Task.CompletedTask;
    }

    internal Task InternalWritePropertyNameAsync(string name, Cancellation cancellation)
    {
        if (cancellation.IsCancellationRequested)
        {
            return cancellation.FromCanceled();
        }

        currentPosition.PropertyName = name;
        return AutoCompleteAsync(JsonToken.PropertyName, cancellation);
    }

    /// <summary>
    /// Asynchronously writes the beginning of a JSON array.
    /// </summary>
    public virtual Task WriteStartArrayAsync(Cancellation cancellation = default)
    {
        if (cancellation.IsCancellationRequested)
        {
            return cancellation.FromCanceled();
        }

        WriteStartArray();
        return Task.CompletedTask;
    }

    internal async Task InternalWriteStartAsync(JsonToken token, JsonContainerType container, Cancellation cancellation)
    {
        UpdateScopeWithFinishedValue();
        await AutoCompleteAsync(token, cancellation).ConfigureAwait(false);
        Push(container);
    }

    /// <summary>
    /// Asynchronously writes a comment <c>/*...*/</c> containing the specified text.
    /// </summary>
    public virtual Task WriteCommentAsync(string? text, Cancellation cancellation = default)
    {
        if (cancellation.IsCancellationRequested)
        {
            return cancellation.FromCanceled();
        }

        WriteComment(text);
        return Task.CompletedTask;
    }

    internal Task InternalWriteCommentAsync(Cancellation cancellation) =>
        AutoCompleteAsync(JsonToken.Comment, cancellation);

    /// <summary>
    /// Asynchronously writes raw JSON where a value is expected and updates the writer's state.
    /// </summary>
    public virtual Task WriteRawValueAsync(string? json, Cancellation cancellation = default)
    {
        if (cancellation.IsCancellationRequested)
        {
            return cancellation.FromCanceled();
        }

        WriteRawValue(json);
        return Task.CompletedTask;
    }

    /// <summary>
    /// Asynchronously writes the beginning of a JSON object.
    /// </summary>
    public virtual Task WriteStartObjectAsync(Cancellation cancellation = default)
    {
        if (cancellation.IsCancellationRequested)
        {
            return cancellation.FromCanceled();
        }

        WriteStartObject();
        return Task.CompletedTask;
    }

    /// <summary>
    /// Asynchronously writes the current <see cref="JsonReader" /> token.
    /// </summary>
    public Task WriteTokenAsync(JsonReader reader, Cancellation cancellation = default) =>
        WriteTokenAsync(reader, true, cancellation);

    /// <summary>
    /// Asynchronously writes the current <see cref="JsonReader" /> token.
    /// </summary>
    /// <param name="writeChildren">A flag indicating whether the current token's children should be written.</param>
    public Task WriteTokenAsync(JsonReader reader, bool writeChildren, Cancellation cancellation = default) =>
        WriteTokenAsync(reader, writeChildren, true, cancellation);

    /// <summary>
    /// Asynchronously writes the <see cref="JsonToken" /> token and its value.
    /// </summary>
    public Task WriteTokenAsync(JsonToken token, Cancellation cancellation = default) =>
        WriteTokenAsync(token, null, cancellation);

    /// <summary>
    /// Asynchronously writes the <see cref="JsonToken" /> token and its value.
    /// </summary>
    /// <param name="value">
    /// The value to write.
    /// A value is only required for tokens that have an associated value, e.g. the <see cref="String" /> property name for <see cref="JsonToken.PropertyName" />.
    /// <c>null</c> can be passed to the method for tokens that don't have a value, e.g. <see cref="JsonToken.StartObject" />.
    /// </param>
    public Task WriteTokenAsync(JsonToken token, object? value, Cancellation cancellation = default)
    {
        if (cancellation.IsCancellationRequested)
        {
            return cancellation.FromCanceled();
        }

        switch (token)
        {
            case JsonToken.None:
                // read to next
                return Task.CompletedTask;
            case JsonToken.StartObject:
                return WriteStartObjectAsync(cancellation);
            case JsonToken.StartArray:
                return WriteStartArrayAsync(cancellation);
            case JsonToken.PropertyName:
                return WritePropertyNameAsync(value!.ToString()!, cancellation);
            case JsonToken.Comment:
                return WriteCommentAsync(value?.ToString(), cancellation);
            case JsonToken.Integer:
                return value is BigInteger integer ? WriteValueAsync(integer, cancellation) : WriteValueAsync(Convert.ToInt64(value, InvariantCulture), cancellation);
            case JsonToken.Float:
                if (value is decimal dec)
                {
                    return WriteValueAsync(dec, cancellation);
                }

                if (value is double doub)
                {
                    return WriteValueAsync(doub, cancellation);
                }

                if (value is float f)
                {
                    return WriteValueAsync(f, cancellation);
                }

                return WriteValueAsync(Convert.ToDouble(value, InvariantCulture), cancellation);
            case JsonToken.String:
                return WriteValueAsync(value!.ToString(), cancellation);
            case JsonToken.Boolean:
                return WriteValueAsync(Convert.ToBoolean(value, InvariantCulture), cancellation);
            case JsonToken.Null:
                return WriteNullAsync(cancellation);
            case JsonToken.Undefined:
                return WriteUndefinedAsync(cancellation);
            case JsonToken.EndObject:
                return WriteEndObjectAsync(cancellation);
            case JsonToken.EndArray:
                return WriteEndArrayAsync(cancellation);
            case JsonToken.Date:
                if (value is DateTimeOffset offset)
                {
                    return WriteValueAsync(offset, cancellation);
                }

                return WriteValueAsync(Convert.ToDateTime(value, InvariantCulture), cancellation);
            case JsonToken.Raw:
                return WriteRawValueAsync(value?.ToString(), cancellation);
            case JsonToken.Bytes:
                if (value is Guid guid)
                {
                    return WriteValueAsync(guid, cancellation);
                }

                return WriteValueAsync((byte[]?) value, cancellation);
            default:
                throw MiscellaneousUtils.CreateArgumentOutOfRangeException(nameof(token), token, "Unexpected token type.");
        }
    }

    internal virtual async Task WriteTokenAsync(JsonReader reader, bool writeChildren, bool writeComments, Cancellation cancellation)
    {
        var initialDepth = CalculateWriteTokenInitialDepth(reader);

        var initialDepthOffset = initialDepth - 1;
        do
        {
            if (writeComments || reader.TokenType != JsonToken.Comment)
            {
                await WriteTokenAsync(reader.TokenType, reader.Value, cancellation).ConfigureAwait(false);
            }
        } while (
            // stop if we have reached the end of the token being read
            initialDepthOffset < reader.Depth - reader.TokenType.EndTokenOffset()
            && writeChildren
            && await reader.ReadAsync(cancellation).ConfigureAwait(false));

        if (IsWriteTokenIncomplete(reader, writeChildren, initialDepth))
        {
            throw JsonWriterException.Create(this, "Unexpected end when reading token.");
        }
    }

    // For internal use, when we know the writer does not offer true async support (e.g. when backed
    // by a StringWriter) and therefore async write methods are always in practice just a less efficient
    // path through the sync version.
    internal async Task WriteTokenSyncReadingAsync(JsonReader reader, Cancellation cancellation)
    {
        var initialDepth = CalculateWriteTokenInitialDepth(reader);

        var initialDepthOffset = initialDepth - 1;
        do
        {
            WriteToken(reader.TokenType, reader.Value);
        } while (
            // stop if we have reached the end of the token being read
            initialDepthOffset < reader.Depth - reader.TokenType.EndTokenOffset()
            && await reader.ReadAsync(cancellation).ConfigureAwait(false));

        if (initialDepth < CalculateWriteTokenFinalDepth(reader))
        {
            throw JsonWriterException.Create(this, "Unexpected end when reading token.");
        }
    }

    /// <summary>
    /// Asynchronously writes a <see cref="Nullable{T}" /> of <see cref="bool" /> value.
    /// </summary>
    public virtual Task WriteValueAsync(bool value, Cancellation cancellation = default)
    {
        if (cancellation.IsCancellationRequested)
        {
            return cancellation.FromCanceled();
        }

        WriteValue(value);
        return Task.CompletedTask;
    }

    /// <summary>
    /// Asynchronously writes a <see cref="bool" /> value.
    /// </summary>
    public virtual Task WriteValueAsync(bool? value, Cancellation cancellation = default)
    {
        if (cancellation.IsCancellationRequested)
        {
            return cancellation.FromCanceled();
        }

        WriteValue(value);
        return Task.CompletedTask;
    }

    /// <summary>
    /// Asynchronously writes a <see cref="byte" /> value.
    /// </summary>
    public virtual Task WriteValueAsync(byte value, Cancellation cancellation = default)
    {
        if (cancellation.IsCancellationRequested)
        {
            return cancellation.FromCanceled();
        }

        WriteValue(value);
        return Task.CompletedTask;
    }

    /// <summary>
    /// Asynchronously writes a <see cref="Nullable{T}" /> of <see cref="byte" /> value.
    /// </summary>
    public virtual Task WriteValueAsync(byte? value, Cancellation cancellation = default)
    {
        if (cancellation.IsCancellationRequested)
        {
            return cancellation.FromCanceled();
        }

        WriteValue(value);
        return Task.CompletedTask;
    }

    /// <summary>
    /// Asynchronously writes a <see cref="byte" />[] value.
    /// </summary>
    public virtual Task WriteValueAsync(byte[]? value, Cancellation cancellation = default)
    {
        if (cancellation.IsCancellationRequested)
        {
            return cancellation.FromCanceled();
        }

        WriteValue(value);
        return Task.CompletedTask;
    }

    /// <summary>
    /// Asynchronously writes a <see cref="char" /> value.
    /// </summary>
    public virtual Task WriteValueAsync(char value, Cancellation cancellation = default)
    {
        if (cancellation.IsCancellationRequested)
        {
            return cancellation.FromCanceled();
        }

        WriteValue(value);
        return Task.CompletedTask;
    }

    /// <summary>
    /// Asynchronously writes a <see cref="Nullable{T}" /> of <see cref="char" /> value.
    /// </summary>
    public virtual Task WriteValueAsync(char? value, Cancellation cancellation = default)
    {
        if (cancellation.IsCancellationRequested)
        {
            return cancellation.FromCanceled();
        }

        WriteValue(value);
        return Task.CompletedTask;
    }

    /// <summary>
    /// Asynchronously writes a <see cref="DateTime" /> value.
    /// </summary>
    public virtual Task WriteValueAsync(DateTime value, Cancellation cancellation = default)
    {
        if (cancellation.IsCancellationRequested)
        {
            return cancellation.FromCanceled();
        }

        WriteValue(value);
        return Task.CompletedTask;
    }

    /// <summary>
    /// Asynchronously writes a <see cref="Nullable{T}" /> of <see cref="DateTime" /> value.
    /// </summary>
    public virtual Task WriteValueAsync(DateTime? value, Cancellation cancellation = default)
    {
        if (cancellation.IsCancellationRequested)
        {
            return cancellation.FromCanceled();
        }

        WriteValue(value);
        return Task.CompletedTask;
    }

    /// <summary>
    /// Asynchronously writes a <see cref="DateTimeOffset" /> value.
    /// </summary>
    public virtual Task WriteValueAsync(DateTimeOffset value, Cancellation cancellation = default)
    {
        if (cancellation.IsCancellationRequested)
        {
            return cancellation.FromCanceled();
        }

        WriteValue(value);
        return Task.CompletedTask;
    }

    /// <summary>
    /// Asynchronously writes a <see cref="Nullable{T}" /> of <see cref="DateTimeOffset" /> value.
    /// </summary>
    public virtual Task WriteValueAsync(DateTimeOffset? value, Cancellation cancellation = default)
    {
        if (cancellation.IsCancellationRequested)
        {
            return cancellation.FromCanceled();
        }

        WriteValue(value);
        return Task.CompletedTask;
    }

    /// <summary>
    /// Asynchronously writes a <see cref="decimal" /> value.
    /// </summary>
    public virtual Task WriteValueAsync(decimal value, Cancellation cancellation = default)
    {
        if (cancellation.IsCancellationRequested)
        {
            return cancellation.FromCanceled();
        }

        WriteValue(value);
        return Task.CompletedTask;
    }

    /// <summary>
    /// Asynchronously writes a <see cref="Nullable{T}" /> of <see cref="decimal" /> value.
    /// </summary>
    public virtual Task WriteValueAsync(decimal? value, Cancellation cancellation = default)
    {
        if (cancellation.IsCancellationRequested)
        {
            return cancellation.FromCanceled();
        }

        WriteValue(value);
        return Task.CompletedTask;
    }

    /// <summary>
    /// Asynchronously writes a <see cref="double" /> value.
    /// </summary>
    public virtual Task WriteValueAsync(double value, Cancellation cancellation = default)
    {
        if (cancellation.IsCancellationRequested)
        {
            return cancellation.FromCanceled();
        }

        WriteValue(value);
        return Task.CompletedTask;
    }

    /// <summary>
    /// Asynchronously writes a <see cref="Nullable{T}" /> of <see cref="double" /> value.
    /// </summary>
    public virtual Task WriteValueAsync(double? value, Cancellation cancellation = default)
    {
        if (cancellation.IsCancellationRequested)
        {
            return cancellation.FromCanceled();
        }

        WriteValue(value);
        return Task.CompletedTask;
    }

    /// <summary>
    /// Asynchronously writes a <see cref="float" /> value.
    /// </summary>
    public virtual Task WriteValueAsync(float value, Cancellation cancellation = default)
    {
        if (cancellation.IsCancellationRequested)
        {
            return cancellation.FromCanceled();
        }

        WriteValue(value);
        return Task.CompletedTask;
    }

    /// <summary>
    /// Asynchronously writes a <see cref="Nullable{T}" /> of <see cref="float" /> value.
    /// </summary>
    public virtual Task WriteValueAsync(float? value, Cancellation cancellation = default)
    {
        if (cancellation.IsCancellationRequested)
        {
            return cancellation.FromCanceled();
        }

        WriteValue(value);
        return Task.CompletedTask;
    }

    /// <summary>
    /// Asynchronously writes a <see cref="Guid" /> value.
    /// </summary>
    public virtual Task WriteValueAsync(Guid value, Cancellation cancellation = default)
    {
        if (cancellation.IsCancellationRequested)
        {
            return cancellation.FromCanceled();
        }

        WriteValue(value);
        return Task.CompletedTask;
    }

    /// <summary>
    /// Asynchronously writes a <see cref="Nullable{T}" /> of <see cref="Guid" /> value.
    /// </summary>
    public virtual Task WriteValueAsync(Guid? value, Cancellation cancellation = default)
    {
        if (cancellation.IsCancellationRequested)
        {
            return cancellation.FromCanceled();
        }

        WriteValue(value);
        return Task.CompletedTask;
    }

    /// <summary>
    /// Asynchronously writes a <see cref="int" /> value.
    /// </summary>
    public virtual Task WriteValueAsync(int value, Cancellation cancellation = default)
    {
        if (cancellation.IsCancellationRequested)
        {
            return cancellation.FromCanceled();
        }

        WriteValue(value);
        return Task.CompletedTask;
    }

    /// <summary>
    /// Asynchronously writes a <see cref="Nullable{T}" /> of <see cref="int" /> value.
    /// </summary>
    public virtual Task WriteValueAsync(int? value, Cancellation cancellation = default)
    {
        if (cancellation.IsCancellationRequested)
        {
            return cancellation.FromCanceled();
        }

        WriteValue(value);
        return Task.CompletedTask;
    }

    /// <summary>
    /// Asynchronously writes a <see cref="long" /> value.
    /// </summary>
    public virtual Task WriteValueAsync(long value, Cancellation cancellation = default)
    {
        if (cancellation.IsCancellationRequested)
        {
            return cancellation.FromCanceled();
        }

        WriteValue(value);
        return Task.CompletedTask;
    }

    /// <summary>
    /// Asynchronously writes a <see cref="Nullable{T}" /> of <see cref="long" /> value.
    /// </summary>
    public virtual Task WriteValueAsync(long? value, Cancellation cancellation = default)
    {
        if (cancellation.IsCancellationRequested)
        {
            return cancellation.FromCanceled();
        }

        WriteValue(value);
        return Task.CompletedTask;
    }

    /// <summary>
    /// Asynchronously writes a <see cref="object" /> value.
    /// </summary>
    public virtual Task WriteValueAsync(object? value, Cancellation cancellation = default)
    {
        if (cancellation.IsCancellationRequested)
        {
            return cancellation.FromCanceled();
        }

        WriteValue(value);
        return Task.CompletedTask;
    }

    /// <summary>
    /// Asynchronously writes a <see cref="sbyte" /> value.
    /// </summary>
    public virtual Task WriteValueAsync(sbyte value, Cancellation cancellation = default)
    {
        if (cancellation.IsCancellationRequested)
        {
            return cancellation.FromCanceled();
        }

        WriteValue(value);
        return Task.CompletedTask;
    }

    /// <summary>
    /// Asynchronously writes a <see cref="Nullable{T}" /> of <see cref="sbyte" /> value.
    /// </summary>
    public virtual Task WriteValueAsync(sbyte? value, Cancellation cancellation = default)
    {
        if (cancellation.IsCancellationRequested)
        {
            return cancellation.FromCanceled();
        }

        WriteValue(value);
        return Task.CompletedTask;
    }

    /// <summary>
    /// Asynchronously writes a <see cref="short" /> value.
    /// </summary>
    public virtual Task WriteValueAsync(short value, Cancellation cancellation = default)
    {
        if (cancellation.IsCancellationRequested)
        {
            return cancellation.FromCanceled();
        }

        WriteValue(value);
        return Task.CompletedTask;
    }

    /// <summary>
    /// Asynchronously writes a <see cref="Nullable{T}" /> of <see cref="short" /> value.
    /// </summary>
    public virtual Task WriteValueAsync(short? value, Cancellation cancellation = default)
    {
        if (cancellation.IsCancellationRequested)
        {
            return cancellation.FromCanceled();
        }

        WriteValue(value);
        return Task.CompletedTask;
    }

    /// <summary>
    /// Asynchronously writes a <see cref="string" /> value.
    /// </summary>
    public virtual Task WriteValueAsync(string? value, Cancellation cancellation = default)
    {
        if (cancellation.IsCancellationRequested)
        {
            return cancellation.FromCanceled();
        }

        WriteValue(value);
        return Task.CompletedTask;
    }

    /// <summary>
    /// Asynchronously writes a <see cref="TimeSpan" /> value.
    /// </summary>
    public virtual Task WriteValueAsync(TimeSpan value, Cancellation cancellation = default)
    {
        if (cancellation.IsCancellationRequested)
        {
            return cancellation.FromCanceled();
        }

        WriteValue(value);
        return Task.CompletedTask;
    }

    /// <summary>
    /// Asynchronously writes a <see cref="Nullable{T}" /> of <see cref="TimeSpan" /> value.
    /// </summary>
    public virtual Task WriteValueAsync(TimeSpan? value, Cancellation cancellation = default)
    {
        if (cancellation.IsCancellationRequested)
        {
            return cancellation.FromCanceled();
        }

        WriteValue(value);
        return Task.CompletedTask;
    }

    /// <summary>
    /// Asynchronously writes a <see cref="uint" /> value.
    /// </summary>
    public virtual Task WriteValueAsync(uint value, Cancellation cancellation = default)
    {
        if (cancellation.IsCancellationRequested)
        {
            return cancellation.FromCanceled();
        }

        WriteValue(value);
        return Task.CompletedTask;
    }

    /// <summary>
    /// Asynchronously writes a <see cref="Nullable{T}" /> of <see cref="uint" /> value.
    /// </summary>
    public virtual Task WriteValueAsync(uint? value, Cancellation cancellation = default)
    {
        if (cancellation.IsCancellationRequested)
        {
            return cancellation.FromCanceled();
        }

        WriteValue(value);
        return Task.CompletedTask;
    }

    /// <summary>
    /// Asynchronously writes a <see cref="ulong" /> value.
    /// </summary>
    public virtual Task WriteValueAsync(ulong value, Cancellation cancellation = default)
    {
        if (cancellation.IsCancellationRequested)
        {
            return cancellation.FromCanceled();
        }

        WriteValue(value);
        return Task.CompletedTask;
    }

    /// <summary>
    /// Asynchronously writes a <see cref="Nullable{T}" /> of <see cref="ulong" /> value.
    /// </summary>
    public virtual Task WriteValueAsync(ulong? value, Cancellation cancellation = default)
    {
        if (cancellation.IsCancellationRequested)
        {
            return cancellation.FromCanceled();
        }

        WriteValue(value);
        return Task.CompletedTask;
    }

    /// <summary>
    /// Asynchronously writes a <see cref="Uri" /> value.
    /// </summary>
    public virtual Task WriteValueAsync(Uri? value, Cancellation cancellation = default)
    {
        if (cancellation.IsCancellationRequested)
        {
            return cancellation.FromCanceled();
        }

        WriteValue(value);
        return Task.CompletedTask;
    }

    /// <summary>
    /// Asynchronously writes a <see cref="ushort" /> value.
    /// </summary>
    public virtual Task WriteValueAsync(ushort value, Cancellation cancellation = default)
    {
        if (cancellation.IsCancellationRequested)
        {
            return cancellation.FromCanceled();
        }

        WriteValue(value);
        return Task.CompletedTask;
    }

    /// <summary>
    /// Asynchronously writes a <see cref="Nullable{T}" /> of <see cref="ushort" /> value.
    /// </summary>
    public virtual Task WriteValueAsync(ushort? value, Cancellation cancellation = default)
    {
        if (cancellation.IsCancellationRequested)
        {
            return cancellation.FromCanceled();
        }

        WriteValue(value);
        return Task.CompletedTask;
    }

    /// <summary>
    /// Asynchronously writes an undefined value.
    /// </summary>
    public virtual Task WriteUndefinedAsync(Cancellation cancellation = default)
    {
        if (cancellation.IsCancellationRequested)
        {
            return cancellation.FromCanceled();
        }

        WriteUndefined();
        return Task.CompletedTask;
    }

    /// <summary>
    /// Asynchronously writes the given white space.
    /// </summary>
    public virtual Task WriteWhitespaceAsync(string ws, Cancellation cancellation = default)
    {
        if (cancellation.IsCancellationRequested)
        {
            return cancellation.FromCanceled();
        }

        WriteWhitespace(ws);
        return Task.CompletedTask;
    }

    internal Task InternalWriteValueAsync(JsonToken token, Cancellation cancellation)
    {
        if (cancellation.IsCancellationRequested)
        {
            return cancellation.FromCanceled();
        }

        UpdateScopeWithFinishedValue();
        return AutoCompleteAsync(token, cancellation);
    }

    /// <summary>
    /// Asynchronously ets the state of the <see cref="JsonWriter" />.
    /// </summary>
    protected Task SetWriteStateAsync(JsonToken token, object value, Cancellation cancellation)
    {
        if (cancellation.IsCancellationRequested)
        {
            return cancellation.FromCanceled();
        }

        switch (token)
        {
            case JsonToken.StartObject:
                return InternalWriteStartAsync(token, JsonContainerType.Object, cancellation);
            case JsonToken.StartArray:
                return InternalWriteStartAsync(token, JsonContainerType.Array, cancellation);
            case JsonToken.PropertyName:
                if (value is not string s)
                {
                    throw new ArgumentException("A name is required when setting property name state.", nameof(value));
                }

                return InternalWritePropertyNameAsync(s, cancellation);
            case JsonToken.Comment:
                return InternalWriteCommentAsync(cancellation);
            case JsonToken.Raw:
                return Task.CompletedTask;
            case JsonToken.Integer:
            case JsonToken.Float:
            case JsonToken.String:
            case JsonToken.Boolean:
            case JsonToken.Date:
            case JsonToken.Bytes:
            case JsonToken.Null:
            case JsonToken.Undefined:
                return InternalWriteValueAsync(token, cancellation);
            case JsonToken.EndObject:
                return InternalWriteEndAsync(JsonContainerType.Object, cancellation);
            case JsonToken.EndArray:
                return InternalWriteEndAsync(JsonContainerType.Array, cancellation);
            default:
                throw new ArgumentOutOfRangeException(nameof(token));
        }
    }

    internal static Task WriteValueAsync(JsonWriter writer, PrimitiveTypeCode typeCode, object value, Cancellation cancellation)
    {
        // ReSharper disable ConditionIsAlwaysTrueOrFalseAccordingToNullableAPIContract
        while (true)
        {
            switch (typeCode)
            {
                case PrimitiveTypeCode.Char:
                    return writer.WriteValueAsync((char) value, cancellation);
                case PrimitiveTypeCode.CharNullable:
                    return writer.WriteValueAsync((char?) value, cancellation);
                case PrimitiveTypeCode.Boolean:
                    return writer.WriteValueAsync((bool) value, cancellation);
                case PrimitiveTypeCode.BooleanNullable:
                    return writer.WriteValueAsync((bool?) value, cancellation);
                case PrimitiveTypeCode.SByte:
                    return writer.WriteValueAsync((sbyte) value, cancellation);
                case PrimitiveTypeCode.SByteNullable:
                    return writer.WriteValueAsync((sbyte?) value, cancellation);
                case PrimitiveTypeCode.Int16:
                    return writer.WriteValueAsync((short) value, cancellation);
                case PrimitiveTypeCode.Int16Nullable:
                    return writer.WriteValueAsync((short?) value, cancellation);
                case PrimitiveTypeCode.UInt16:
                    return writer.WriteValueAsync((ushort) value, cancellation);
                case PrimitiveTypeCode.UInt16Nullable:
                    return writer.WriteValueAsync((ushort?) value, cancellation);
                case PrimitiveTypeCode.Int32:
                    return writer.WriteValueAsync((int) value, cancellation);
                case PrimitiveTypeCode.Int32Nullable:
                    // ReSharper disable once MergeConditionalExpression
                    return writer.WriteValueAsync(value == null ? null : (int) value, cancellation);
                case PrimitiveTypeCode.Byte:
                    return writer.WriteValueAsync((byte) value, cancellation);
                case PrimitiveTypeCode.ByteNullable:
                    return writer.WriteValueAsync((byte?) value, cancellation);
                case PrimitiveTypeCode.UInt32:
                    return writer.WriteValueAsync((uint) value, cancellation);
                case PrimitiveTypeCode.UInt32Nullable:
                    return writer.WriteValueAsync((uint?) value, cancellation);
                case PrimitiveTypeCode.Int64:
                    return writer.WriteValueAsync((long) value, cancellation);
                case PrimitiveTypeCode.Int64Nullable:
                    return writer.WriteValueAsync((long?) value, cancellation);
                case PrimitiveTypeCode.UInt64:
                    return writer.WriteValueAsync((ulong) value, cancellation);
                case PrimitiveTypeCode.UInt64Nullable:
                    return writer.WriteValueAsync((ulong?) value, cancellation);
                case PrimitiveTypeCode.Single:
                    return writer.WriteValueAsync((float) value, cancellation);
                case PrimitiveTypeCode.SingleNullable:
                    return writer.WriteValueAsync((float?) value, cancellation);
                case PrimitiveTypeCode.Double:
                    return writer.WriteValueAsync((double) value, cancellation);
                case PrimitiveTypeCode.DoubleNullable:
                    return writer.WriteValueAsync((double?) value, cancellation);
                case PrimitiveTypeCode.DateTime:
                    return writer.WriteValueAsync((DateTime) value, cancellation);
                case PrimitiveTypeCode.DateTimeNullable:
                    return writer.WriteValueAsync((DateTime?) value, cancellation);
                case PrimitiveTypeCode.DateTimeOffset:
                    return writer.WriteValueAsync((DateTimeOffset) value, cancellation);
                case PrimitiveTypeCode.DateTimeOffsetNullable:
                    return writer.WriteValueAsync((DateTimeOffset?) value, cancellation);
                case PrimitiveTypeCode.Decimal:
                    return writer.WriteValueAsync((decimal) value, cancellation);
                case PrimitiveTypeCode.DecimalNullable:
                    return writer.WriteValueAsync((decimal?) value, cancellation);
                case PrimitiveTypeCode.Guid:
                    return writer.WriteValueAsync((Guid) value, cancellation);
                case PrimitiveTypeCode.GuidNullable:
                    return writer.WriteValueAsync((Guid?) value, cancellation);
                case PrimitiveTypeCode.TimeSpan:
                    return writer.WriteValueAsync((TimeSpan) value, cancellation);
                case PrimitiveTypeCode.TimeSpanNullable:
                    return writer.WriteValueAsync((TimeSpan?) value, cancellation);
                case PrimitiveTypeCode.BigInteger:

                    // this will call to WriteValueAsync(object)
                    return writer.WriteValueAsync((BigInteger) value, cancellation);
                case PrimitiveTypeCode.BigIntegerNullable:

                    // this will call to WriteValueAsync(object)
                    return writer.WriteValueAsync((BigInteger?) value, cancellation);
                case PrimitiveTypeCode.Uri:
                    return writer.WriteValueAsync((Uri) value, cancellation);
                case PrimitiveTypeCode.String:
                    return writer.WriteValueAsync((string) value, cancellation);
                case PrimitiveTypeCode.Bytes:
                    return writer.WriteValueAsync((byte[]) value, cancellation);
                case PrimitiveTypeCode.DBNull:
                    return writer.WriteNullAsync(cancellation);
                default:
                    if (value is IConvertible convertible)
                    {
                        ResolveConvertibleValue(convertible, out typeCode, out value);
                        continue;
                    }

                    // write an unknown null value, fix https://github.com/JamesNK/Newtonsoft.Json/issues/1460
                    if (value == null)
                    {
                        return writer.WriteNullAsync(cancellation);
                    }

                    throw CreateUnsupportedTypeException(writer, value);
            }
        }
        // ReSharper restore ConditionIsAlwaysTrueOrFalseAccordingToNullableAPIContract
    }
}