﻿// Copyright (c) 2007 James Newton-King. All rights reserved.
// Use of this source code is governed by The MIT License,
// as found in the license.md file.

static class AsyncUtils
{
    // Pre-allocate to avoid wasted allocations.
    public static readonly Task<bool> False = Task.FromResult(false);
    public static readonly Task<bool> True = Task.FromResult(true);

    internal static Task<bool> ToAsync(this bool value) =>
        value ? True : False;

    public static Task? CancelIfRequestedAsync(this Cancellation cancellation) =>
        cancellation.IsCancellationRequested ? FromCanceled(cancellation) : null;

    public static Task<T>? CancelIfRequestedAsync<T>(this Cancellation cancellation) =>
        cancellation.IsCancellationRequested ? FromCanceled<T>(cancellation) : null;

    // From 4.6 on we could use Task.FromCanceled(), but we need an equivalent for
    // previous frameworks.
    public static Task FromCanceled(this Cancellation cancellation)
    {
        MiscellaneousUtils.Assert(cancellation.IsCancellationRequested);
        return new(
            () =>
            {
            },
            cancellation);
    }

    static Task<T> FromCanceled<T>(this Cancellation cancellation)
    {
        MiscellaneousUtils.Assert(cancellation.IsCancellationRequested);
#pragma warning disable CS8603 // Possible null reference return.
        return new(() => default, cancellation);
#pragma warning restore CS8603 // Possible null reference return.
    }

    public static Task WriteAsync(this TextWriter writer, char value, Cancellation cancellation) =>
        cancellation.IsCancellationRequested ? FromCanceled(cancellation) : writer.WriteAsync(value);

    public static Task WriteAsync(this TextWriter writer, string? value, Cancellation cancellation) =>
        cancellation.IsCancellationRequested ? FromCanceled(cancellation) : writer.WriteAsync(value);

    public static Task WriteAsync(this TextWriter writer, char[] value, int start, int count, Cancellation cancellation) =>
        cancellation.IsCancellationRequested ? FromCanceled(cancellation) : writer.WriteAsync(value, start, count);

    public static Task<int> ReadAsync(this TextReader reader, char[] buffer, int index, int count, Cancellation cancellation) =>
        cancellation.IsCancellationRequested ? FromCanceled<int>(cancellation) : reader.ReadAsync(buffer, index, count);

    public static bool IsCompletedSuccessfully(this Task task) =>
        task.Status == TaskStatus.RanToCompletion;
}