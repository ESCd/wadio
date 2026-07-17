using System.Collections.Frozen;
using System.Diagnostics;
using Wadio.Platform.Api.Abstractions;

namespace Wadio.Platform.Discord.Infrastructure.Playback;

internal sealed class FFmpegPcmEncoder( Codec codec ) : PcmEncoder( codec )
{
    private static readonly IReadOnlyDictionary<Codec, ProcessStartInfo> StartupByCodec = Enum.GetValues<Codec>()
        .Where( codec => codec is not Codec.Unknown )
        .ToFrozenDictionary( codec => codec, codec => new ProcessStartInfo( "ffmpeg", BuildArguments( codec ) )
        {
            CreateNoWindow = true,
            RedirectStandardError = false,
            RedirectStandardInput = true,
            RedirectStandardOutput = true,
            UseShellExecute = false,
            WindowStyle = ProcessWindowStyle.Hidden,
        } );

    private readonly CancellationTokenSource cancellation = new();
    private readonly ProcessStartInfo startup = StartupByCodec[ codec ];

    private static IEnumerable<string> BuildArguments( Codec codec )
    {
        yield return "-hide_banner";
        yield return "-loglevel"; yield return "-8";
        yield return "-fflags"; yield return "+discardcorrupt";
        yield return "-flags"; yield return "low_delay";

        if( codec is not Codec.Unknown )
        {
            yield return "-f"; yield return FormatCodec( codec );
        }

        yield return "-i"; yield return "pipe:0";
        yield return "-vn"; yield return "-sn"; yield return "-dn";
        yield return "-f"; yield return "s16le";
        yield return "-sample_fmt"; yield return "s16";
        yield return "-af"; yield return "aresample=resampler=soxr:precision=28";
        yield return "-ac"; yield return "2";
        yield return "-ar"; yield return "48000";
        yield return "-threads"; yield return "3";
        yield return "pipe:1";

        static string FormatCodec( Codec codec ) => codec switch
        {
            // NOTE: HE-AAC is still aac container from ffmpeg's perspective
            Codec.AAC or Codec.AACPlus => "aac",

            // NOTE: AAC+H.264 implies a muxed container — TS is the safe bet
            Codec.AACH264 or Codec.AACPlusH264 => "mpegts",
            Codec.FLAC => "flac",
            Codec.FLV => "flv",
            Codec.MP3 => "mp3",
            Codec.OGG => "ogg",

            _ => throw new ArgumentException( $"Codec '{codec}' cannot be formatted to a supported ffmpeg codec.", nameof( codec ) )
        };
    }

    public override async ValueTask DisposeAsync( )
    {
        await cancellation.CancelAsync();
        cancellation.Dispose();
    }

    public override async Task Encode( Stream source, Stream output, CancellationToken cancellation = default )
    {
        ArgumentNullException.ThrowIfNull( source );
        ArgumentNullException.ThrowIfNull( output );

        using var process = new Process
        {
            StartInfo = startup,
        };

        if( !TryStart( process ) )
        {
            ThrowFailedToStart();
        }

        using var combined = CancellationTokenSource.CreateLinkedTokenSource( this.cancellation.Token, cancellation );
        try
        {
            var reading = Task.Run( ( ) => Read( process, output, combined.Token ), combined.Token );
            var writing = Task.Run( ( ) => Write( process, source, combined.Token ), combined.Token );

            await Task.WhenAll( reading, writing );
        }
        catch( OperationCanceledException e ) when( e.CancellationToken == combined.Token && this.cancellation.IsCancellationRequested )
        {
            throw new ObjectDisposedException( nameof( FFmpegPcmEncoder ), "Encoding was canceled because the encoder was disposed." );
        }
    }

    private static async Task Read( Process ffmpeg, Stream output, CancellationToken cancellation )
    {
        ArgumentNullException.ThrowIfNull( ffmpeg );
        ArgumentNullException.ThrowIfNull( output );

        try
        {
            await ffmpeg.StandardOutput.BaseStream.CopyToAsync( output, cancellation );
            await ffmpeg.WaitForExitAsync( cancellation );
        }
        catch( OperationCanceledException ) { }
        catch( IOException ) { }
        finally
        {
            if( !ffmpeg.HasExited )
            {
                ffmpeg.Kill();
            }
        }
    }

    private static bool TryStart( Process ffmpeg )
    {
        ArgumentNullException.ThrowIfNull( ffmpeg );

        if( ffmpeg.Start() )
        {
            if( OperatingSystem.IsWindows() )
            {
                ffmpeg.PriorityBoostEnabled = true;
                ffmpeg.PriorityClass = ProcessPriorityClass.AboveNormal;
            }

            return true;
        }

        return false;
    }

    private static void ThrowFailedToStart( ) => throw new InvalidProgramException( "Failed to start ffmpeg process." );

    private static async Task Write( Process ffmpeg, Stream audio, CancellationToken cancellation )
    {
        ArgumentNullException.ThrowIfNull( ffmpeg );
        ArgumentNullException.ThrowIfNull( audio );

        try
        {
            await audio.CopyToAsync( ffmpeg.StandardInput.BaseStream, cancellation );
        }
        catch( OperationCanceledException ) { }
        catch( IOException ) { }
        finally
        {
            // NOTE: Signal EOF to ffmpeg; else ffmpeg will hang waiting for more input
            ffmpeg.StandardInput.Close();
        }
    }
}