using System.Net.Mime;
using ESCd.Extensions.Http;
using Microsoft.IO;
using SkiaSharp;

namespace Wadio.Platform.Api.Infrastructure;

internal sealed class StationIconLoader( HttpClient http, RecyclableMemoryStreamManager? streamManager = default )
{
    private static bool IsValidContentType( string? type )
        => type is MediaTypeNames.Image.Gif
                or MediaTypeNames.Image.Jpeg
                or MediaTypeNames.Image.Png
                or MediaTypeNames.Image.Svg
                or MediaTypeNames.Image.Webp
                or "image/heic";

    public async Task<StationIconContent> LoadAsync( Uri url, CancellationToken cancellation )
    {
        ArgumentNullException.ThrowIfNull( url );

        var response = await http.GetAsync(
            url,
            HttpCompletionOption.ResponseHeadersRead,
            cancellation );

        try
        {
            response.EnsureSuccessStatusCode();
            if( response.Content.Headers.ContentLength.HasValue && response.Content.Headers.ContentLength.Value > 10_000_000 )
            {
                throw new InvalidOperationException( "Response content exceeds 10MB limit." );
            }

            var contentType = response.Content.Headers.ContentType?.ToString();
            var fileName = Path.GetFileName( url.AbsolutePath );

            if( !IsValidContentType( contentType ) )
            {
                using var data = await ReadAsStream(
                    response.Content,
                    streamManager,
                    cancellation );

                var bitmap = SKBitmap.Decode( data ) ?? throw new InvalidOperationException( "Failed to decode image." );
                return new SKBitmapStationIconContent( bitmap, streamManager )
                {
                    FileName = fileName
                };
            }

            return new HttpStationIconContent( response, streamManager )
            {
                FileName = fileName
            };
        }
        catch
        {
            response.Dispose();
            throw;
        }

        static async Task<Stream> ReadAsStream( HttpContent content, RecyclableMemoryStreamManager? streamManager, CancellationToken cancellation )
        {
            if( streamManager is null )
            {
                var source = await content.ReadAsStreamAsync( cancellation );
                var buffer = content.Headers.ContentLength.HasValue
                    ? new MemoryStream( ( int )content.Headers.ContentLength.Value )
                    : new MemoryStream();

                await source.CopyToAsync( buffer, cancellation );
                buffer.Seek( 0, SeekOrigin.Begin );

                return buffer;
            }

            return await content.ReadAsStreamAsync( streamManager, cancellation );
        }
    }
}

public abstract class StationIconContent : IDisposable
{
    public abstract string ContentType { get; }

    public virtual string? FileName { get; init; }

    public abstract Task<Stream> CreateReadStream( CancellationToken cancellation = default );

    public abstract void Dispose( );
}

internal sealed class SKBitmapStationIconContent( SKBitmap bitmap, RecyclableMemoryStreamManager? streamManager = default ) : StationIconContent
{
    public override string ContentType => MediaTypeNames.Image.Png;

    public override Task<Stream> CreateReadStream( CancellationToken cancellation = default )
    {
        var stream = streamManager?.GetStream( nameof( SKBitmapStationIconContent ) )
            ?? new MemoryStream();

        bitmap.Encode(
            stream,
            SKEncodedImageFormat.Png,
            100 );

        stream.Seek( 0, SeekOrigin.Begin );
        return Task.FromResult<Stream>( stream );
    }

    public override void Dispose( ) => bitmap.Dispose();
}

internal sealed class HttpStationIconContent( HttpResponseMessage response, RecyclableMemoryStreamManager? streamManager = default ) : StationIconContent
{
    public override string ContentType { get; } = response.Content.Headers.ContentType?.ToString()!;

    public override Task<Stream> CreateReadStream( CancellationToken cancellation = default )
    {
        if( streamManager is null )
        {
            return response.Content.ReadAsStreamAsync( cancellation );
        }

        return response.Content.ReadAsStreamAsync( streamManager, cancellation );
    }

    public override void Dispose( ) => response.Dispose();
}