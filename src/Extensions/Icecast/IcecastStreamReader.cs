using System.Buffers;
using System.Diagnostics.CodeAnalysis;
using System.IO.Pipelines;
using System.Text;
using Wadio.Extensions.Icecast.Abstractions;

namespace Wadio.Extensions.Icecast;

public sealed class IcecastStreamReader : IAsyncDisposable
{
    private Pipe? audio;
    private readonly CancellationTokenSource cancellation = new();
    private readonly Stream data;
    private readonly PipeReader reader;
    private readonly Task reading;
    private readonly HttpResponseMessage response;

    public int Interval { get; }
    public Uri Url { get; }

    [MemberNotNullWhen( true, nameof( Exception ) )]
    public bool IsFaulted => Exception is not null;
    public Exception? Exception { get; private set; }

    public event Func<Exception?, ValueTask> Ended;
    public event MetadataReadHandler MetadataRead;

    internal IcecastStreamReader( HttpResponseMessage response, Stream data, int interval )
    {
        ArgumentNullException.ThrowIfNull( response );
        ArgumentNullException.ThrowIfNull( data );
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero( interval );

        Interval = interval;
        Url = response.RequestMessage!.RequestUri!;

        this.data = data;
        this.response = response;

        reader = PipeReader.Create( data );
        reading = Task.Run( Read, cancellation.Token );
    }

    public async Task CloseAsync( )
    {
        await cancellation.CancelAsync().ConfigureAwait( false );

        if( audio is not null )
        {
            audio.Reader.CancelPendingRead();
            await audio.Reader.CompleteAsync().ConfigureAwait( false );

            audio.Writer.CancelPendingFlush();
            await audio.Writer.CompleteAsync().ConfigureAwait( false );
        }

        reader.CancelPendingRead();
        await reader.CompleteAsync().ConfigureAwait( false );
    }

    public Stream CreateAudioStream( )
    {
        audio ??= new();
        return audio.Reader.AsStream( true );
    }

    public async ValueTask DisposeAsync( )
    {
        await CloseAsync().ConfigureAwait( false );
        try
        {
            await reading.ConfigureAwait( false );
        }
        catch( OperationCanceledException )
        {
        }

        await data.DisposeAsync().ConfigureAwait( false );

        response.Dispose();
        cancellation.Dispose();
    }

    private async Task Read( )
    {
        Exception? exception = default;
        try
        {
            while( !cancellation.IsCancellationRequested )
            {
                var result = await reader.ReadAsync( cancellation.Token );
                var buffer = result.Buffer;

                // NOTE: read at least `interval` (with padding) bytes
                if( buffer.Length < Interval + 1 )
                {
                    reader.AdvanceTo( buffer.Start, buffer.End );
                    if( result.IsCompleted )
                    {
                        break;
                    }

                    continue;
                }

                if( audio is not null )
                {
                    await WriteAudioChunk(
                        audio.Writer,
                        buffer.Slice( buffer.Start, Interval ),
                        cancellation.Token ).ConfigureAwait( false );
                }

                var sequence = new SequenceReader<byte>( buffer );

                // NOTE: skip audio data
                sequence.Advance( Interval );

                // NOTE: attempt to read the length "header"
                if( !sequence.TryRead( out var value ) )
                {
                    reader.AdvanceTo( buffer.Start, buffer.End );
                    continue;
                }

                var length = value * 16;
                if( length is 0 )
                {
                    // throw new InvalidDataException( "The stream did not contain a valid Icecast/Shoutcast metadata payload." );

                    reader.AdvanceTo( sequence.Position, sequence.Position );
                    continue;
                }

                // NOTE: ensure the entire contents of the metadata block have been buffered
                if( buffer.Length < Interval + 1 + length )
                {
                    reader.AdvanceTo( buffer.Start, buffer.End );
                    if( result.IsCompleted )
                    {
                        throw new EndOfStreamException();
                    }

                    continue;
                }

                if( MetadataRead is not null && TryReadMetadata( buffer.Slice( sequence.Position, length ), out var values ) )
                {
                    await MetadataRead.Invoke( new( Interval, values ) ).ConfigureAwait( false );
                }

                var end = buffer.GetPosition( Interval + 1 + length );
                reader.AdvanceTo( end, end );
            }
        }
        catch( Exception e )
        {
            exception = e.GetBaseException();
        }
        finally
        {
            if( audio is not null )
            {
                await audio.Writer.CompleteAsync( exception ).ConfigureAwait( false );
            }

            if( Ended is not null )
            {
                await Ended.Invoke( exception ).ConfigureAwait( false );
            }
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

        static async ValueTask WriteAudioChunk( PipeWriter writer, ReadOnlySequence<byte> chunk, CancellationToken cancellation )
        {
            ArgumentNullException.ThrowIfNull( writer );

            foreach( var segment in chunk )
            {
                var memory = writer.GetMemory( segment.Length );
                segment.CopyTo( memory );

                writer.Advance( segment.Length );
            }

            await writer.FlushAsync( cancellation ).ConfigureAwait( false );
        }
    }
}

public delegate ValueTask MetadataReadHandler( IcecastMetadataDictionary metadata );