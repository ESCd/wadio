using System.Net.Http.Json;
using System.Net.Mime;
using ESCd.Extensions.Http;
using Microsoft.Extensions.ObjectPool;
using Wadio.Extensions.CloudflareApi;
using Wadio.Extensions.CloudflareApi.Abstractions;

internal sealed class CloudflareImagesApi(
    HttpClient http,
    ObjectPool<QueryStringBuilder> queryStringPool ) : ICloudflareImagesApi
{
    public Task<DeleteImageResponse> DeleteAsync( string imageId, CancellationToken cancellation )
    {
        ArgumentException.ThrowIfNullOrWhiteSpace( imageId );

        return http.DeleteFromJsonAsync(
            $"v1/{imageId}",
            CloudflareApiJsonContext.Default.DeleteImageResponse,
            cancellation )!;
    }

    public Task<ListImagesResponse> ListAsync( ListImagesRequest request, CancellationToken cancellation )
    {
        ArgumentNullException.ThrowIfNull( request );

        var query = CreateQuery(
            queryStringPool,
            request );

        return http.GetFromJsonAsync(
            $"v2{query}",
            CloudflareApiJsonContext.Default.ListImagesResponse,
            cancellation )!;

        static string CreateQuery( ObjectPool<QueryStringBuilder> queryStringPool, ListImagesRequest request )
        {
            ArgumentNullException.ThrowIfNull( queryStringPool );
            ArgumentNullException.ThrowIfNull( request );

            var query = queryStringPool.Get();
            try
            {
                query = query.Append( "continuation_token", request?.ContinuationToken )
                    .Append( "creator", request?.Creator );

                foreach( var filter in request?.Meta ?? [] )
                {
                    query = query.Append( $"meta.{filter.Key}{filter.GetOperatorString()}", filter.Value );
                }

                query = query.Append( "per_page", request?.PerPage )
                    .Append( "sort", request?.Sort?.ToString().ToLowerInvariant() );

                return query.ToString();
            }
            finally
            {
                queryStringPool.Return( query );
            }
        }
    }

    public Task<StatsResponse> StatsAsync( CancellationToken cancellation ) => http.GetFromJsonAsync(
        "v1/stats",
        CloudflareApiJsonContext.Default.StatsResponse,
        cancellation )!;

    public async Task<UploadImageResponse> UploadAsync( UploadImageRequest request, CancellationToken cancellation )
    {
        ArgumentNullException.ThrowIfNull( request );

        try
        {
            using var content = CreateUploadContent( request );
            using var response = await http.PostAsync(
                "v1",
                content,
                cancellation );

            return (await response.Content.ReadFromJsonAsync(
                CloudflareApiJsonContext.Default.UploadImageResponse,
                cancellation ))!;
        }
        finally
        {
            if( request is UploadImageRequest.File file )
            {
                await file.Data.DisposeAsync();
            }
        }

        static MultipartFormDataContent CreateUploadContent( UploadImageRequest request )
        {
            ArgumentNullException.ThrowIfNull( request );

            var content = new MultipartFormDataContent();
            if( !string.IsNullOrWhiteSpace( request.Creator ) )
            {
                content.Add(
                    new StringContent( request.Creator ),
                    "creator" );
            }

            if( request is UploadImageRequest.File file )
            {
                content.Add(
                    new StreamContent( file.Data )
                    {
                        Headers =
                        {
                            ContentType = new( file.ContentType ?? MediaTypeNames.Application.Octet )
                        }
                    },
                    "file",
                    file.FileName ?? "file" );
            }

            if( !string.IsNullOrWhiteSpace( request.Id ) )
            {
                content.Add(
                    new StringContent( request.Id ),
                    "id" );
            }

            if( request.Meta is not null )
            {
                throw new NotSupportedException( "Meta data is not supported in this implementation." );
            }

            if( request.RequireSignedUrls is not null )
            {
                content.Add(
                    new StringContent( request.RequireSignedUrls.Value.ToString().ToLowerInvariant() ),
                    "requireSignedUrls" );
            }

            if( request is UploadImageRequest.Url url )
            {
                content.Add(
                    new StringContent( url.Value.ToString() ),
                    "url" );
            }

            return content;
        }
    }
}