using System.Buffers;
using System.Diagnostics.CodeAnalysis;
using System.IO.Pipelines;
using System.Text;
using Wadio.Extensions.Icecast.Abstractions;

namespace Wadio.Extensions.Icecast;

public sealed class IcecastMetadataReader : IAsyncDisposable
{
    private readonly CancellationTokenSource cancellation = new();
    private readonly Stream data;
    private readonly PipeReader pipe;
    private readonly HttpResponseMessage response;

    public int Interval { get; }

    [MemberNotNullWhen( true, nameof( Exception ) )]
    public bool IsFaulted => Exception is not null;
    public Exception? Exception { get; private set; }

    public event Action<Exception?> Ended;
    public event MetadataReadHandler MetadataRead;

    internal IcecastMetadataReader( HttpResponseMessage response, Stream data, int interval )
    {
        ArgumentNullException.ThrowIfNull( response );
        ArgumentNullException.ThrowIfNull( data );
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero( interval );

        this.data = data;
        this.response = response;
        pipe = PipeReader.Create( data );

        Interval = interval;
        Task.Run( ( ) => Read( pipe, interval, metadata => MetadataRead?.Invoke( metadata ) ?? default, cancellation.Token ), cancellation.Token )
            .ContinueWith( task =>
            {
                Exception = task.Exception?.GetBaseException();
                Ended?.Invoke( Exception );
            }, TaskContinuationOptions.ExecuteSynchronously );
    }

    public async Task Close( )
    {
        pipe.CancelPendingRead();

        await cancellation.CancelAsync().ConfigureAwait( false );
        await pipe.CompleteAsync().ConfigureAwait( false );
    }

    public async ValueTask DisposeAsync( )
    {
        await Close().ConfigureAwait( false );
        await data.DisposeAsync().ConfigureAwait( false );

        response.Dispose();
        cancellation.Dispose();
    }

    private static async Task Read( PipeReader pipe, int interval, MetadataReadHandler onMetadataRead, CancellationToken cancellation )
    {
        ArgumentNullException.ThrowIfNull( pipe );
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero( interval );
        ArgumentNullException.ThrowIfNull( onMetadataRead );

        while( !cancellation.IsCancellationRequested )
        {
            var result = await pipe.ReadAsync( cancellation );
            var buffer = result.Buffer;

            // NOTE: read at least `interval` (with padding) bytes
            if( buffer.Length < interval + 1 )
            {
                pipe.AdvanceTo( buffer.Start, buffer.End );
                if( result.IsCompleted )
                {
                    break;
                }

                continue;
            }

            var reader = new SequenceReader<byte>( buffer );

            // NOTE: skip audio data
            reader.Advance( interval );

            // NOTE: attempt to read the length "header"
            if( !reader.TryRead( out var value ) )
            {
                pipe.AdvanceTo( buffer.Start, buffer.End );
                continue;
            }

            var length = value * 16;
            if( length is 0 )
            {
                // throw new InvalidDataException( "The stream did not contain a valid Icecast/Shoutcast metadata payload." );

                pipe.AdvanceTo( reader.Position, reader.Position );
                continue;
            }

            // NOTE: ensure the entire contents of the metadata block have been buffered
            if( buffer.Length < interval + 1 + length )
            {
                pipe.AdvanceTo( buffer.Start, buffer.End );
                if( result.IsCompleted )
                {
                    throw new EndOfStreamException();
                }

                continue;
            }

            if( TryReadMetadata( buffer.Slice( reader.Position, length ), out var values ) )
            {
                await onMetadataRead( new( interval, values ) ).ConfigureAwait( false );
            }

            var end = buffer.GetPosition( interval + 1 + length );
            pipe.AdvanceTo( end, end );
        }

        static bool TryReadMetadata( in ReadOnlySequence<byte> data, [NotNullWhen( true )] out IDictionary<string, string> values )
        {
            values = new Dictionary<string, string>( StringComparer.OrdinalIgnoreCase );

            var reader = new SequenceReader<byte>( data );
            while( !reader.End )
            {
                if( !reader.TryReadTo( out ReadOnlySequence<byte> key, ( byte )'=', true ) )
                {
                    break;
                }

                if( !reader.TryReadTo( out ReadOnlySequence<byte> value, ( byte )';', true ) )
                {
                    value = data.Slice( reader.Position );
                    reader.Advance( reader.Remaining );
                }

                values[ DecodeUtf8( key ) ] = DecodeUtf8( value ).Trim( '\'' );
            }

            return values.Count > 0;

            static string DecodeUtf8( in ReadOnlySequence<byte> value ) => Encoding.UTF8.GetString( value ).TrimEnd( '\0' );
        }
    }
}

public delegate ValueTask MetadataReadHandler( IcecastMetadataDictionary metadata );