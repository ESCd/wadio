namespace Wadio.Platform.Api.Abstractions;

public static class StationsApiExtensions
{
    public static IAsyncEnumerable<Station> Related( this IStationsApi api, Station station, SearchStationsParameters? parameters = default, CancellationToken cancellation = default )
    {
        ArgumentNullException.ThrowIfNull( api );
        ArgumentNullException.ThrowIfNull( station );

        if( station.Tags.Length is 0 )
        {
            return AsyncEnumerable.Empty<Station>();
        }

        var count = parameters?.Count ?? 10;
        parameters = (parameters ??= new()) with
        {
            // NOTE: request one extra, so we can trim the current station if present
            Count = count + 1, // +1 to account for the station itself

            Order = parameters?.Order ?? StationOrderBy.Random,

            // NOTE: override tag filters to find stations with similar tags
            Tags = SelectTags( station )
        };

        return api.Search( parameters, cancellation )

            // NOTE: filter out current station if present
            .Where( value => value.Id != station.Id )
            .Take( ( int )count );

        static string[] SelectTags( Station station )
        {
            ArgumentNullException.ThrowIfNull( station );
            if( station.Tags.Length <= 3 )
            {
                return station.Tags;
            }

            var tags = new List<string>( station.Tags );
            while( tags.Count > 3 )
            {
                // NOTE: reduce to 3 random tags
                tags.RemoveAt( Random.Shared.Next( 0, tags.Count ) );
            }

            return [ .. tags ];
        }
    }
}