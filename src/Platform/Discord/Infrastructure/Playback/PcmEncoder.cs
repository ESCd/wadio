using System.Collections.Concurrent;
using Wadio.Platform.Api.Abstractions;

namespace Wadio.Platform.Discord.Infrastructure.Playback;

internal abstract class PcmEncoder( Codec codec ) : IAsyncDisposable
{
    public Codec Codec => codec;

    public abstract ValueTask DisposeAsync( );
    public abstract Task Encode( Stream source, Stream output, CancellationToken cancellation = default );
}

internal sealed class PcmEncoderPool( PcmEncoderFactory factory, ILogger<PcmEncoderPool>? logger = default ) : IAsyncDisposable
{
    private int count;
    private bool disposed;

    private readonly ConcurrentDictionary<Codec, ConcurrentQueue<PcmEncoder>> encoders = new();

    public int Capacity { get; } = Environment.ProcessorCount * 4;
    public int Count => count;

    public async ValueTask DisposeAsync( )
    {
        disposed = true;

        foreach( var queue in encoders.Values )
        {
            while( queue.TryDequeue( out var encoder ) )
            {
                await encoder.DisposeAsync();
            }
        }

        encoders.Clear();
    }

    public PcmEncoder Get( Codec codec )
    {
        ObjectDisposedException.ThrowIf( disposed, this );

        if( encoders.GetOrAdd( codec, _ => new() ).TryDequeue( out var encoder ) )
        {
            Interlocked.Decrement( ref count );

            logger?.OnEncoderRetrieved( codec, count );
            return encoder;
        }

        logger?.OnEncoderCreated( codec, count );
        return factory( codec );
    }

    public ValueTask Return( PcmEncoder encoder )
    {
        ArgumentNullException.ThrowIfNull( encoder );

        if( disposed || !Return( encoder ) )
        {
            logger?.OnEncoderEvicted( encoder.Codec, count );
            return encoder.DisposeAsync();
        }

        logger?.OnEncoderReturned( encoder.Codec, count );
        return default;

        bool Return( PcmEncoder encoder )
        {
            if( Interlocked.Increment( ref count ) <= Capacity )
            {
                encoders.GetOrAdd( encoder.Codec, _ => new() )
                    .Enqueue( encoder );

                return true;
            }

            Interlocked.Decrement( ref count );
            return false;
        }
    }
}

internal delegate PcmEncoder PcmEncoderFactory( Codec codec );

internal static partial class PcmEncoderPoolLogging
{
    [LoggerMessage( Level = LogLevel.Trace, Message = "Created a new PCM encoder for codec '{codec}' (encoders: {count})." )]
    public static partial void OnEncoderCreated( this ILogger<PcmEncoderPool> logger, Codec codec, int count );

    [LoggerMessage( Level = LogLevel.Trace, Message = "Evicted a PCM encoder for codec '{codec}' (encoders: {count})." )]
    public static partial void OnEncoderEvicted( this ILogger<PcmEncoderPool> logger, Codec codec, int count );

    [LoggerMessage( Level = LogLevel.Trace, Message = "Retrieved a PCM encoder for codec '{codec}' (encoders: {count})." )]
    public static partial void OnEncoderRetrieved( this ILogger<PcmEncoderPool> logger, Codec codec, int count );

    [LoggerMessage( Level = LogLevel.Trace, Message = "Returned a PCM encoder for codec '{codec}' (encoders: {count})." )]
    public static partial void OnEncoderReturned( this ILogger<PcmEncoderPool> logger, Codec codec, int count );
}