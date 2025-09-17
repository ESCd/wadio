using System.Collections.Immutable;
using System.Runtime.CompilerServices;
using Wadio.App.Abstractions.Api;

namespace Wadio.App.UI.Components.Layout;

public sealed record HeaderState : State<HeaderState>
{
    private const uint StationCount = 4;

    public bool IsLoading { get; init; }
    public ImmutableArray<Station>? Stations { get; init; }

    internal static HeaderState Reset( HeaderState state )
    {
        ArgumentNullException.ThrowIfNull( state );
        return state with
        {
            IsLoading = false,
            Stations = default,
        };
    }

    internal static async IAsyncEnumerable<HeaderState> Search( IStationsApi api, string query, HeaderState state, [EnumeratorCancellation] CancellationToken cancellation )
    {
        ArgumentNullException.ThrowIfNull( api );
        ArgumentException.ThrowIfNullOrWhiteSpace( query );
        ArgumentNullException.ThrowIfNull( state );

        yield return state = (state with
        {
            IsLoading = true,
            Stations = [],
        });

        yield return await Search( api, query, state, cancellation );

        static async Task<HeaderState> Search( IStationsApi api, string query, HeaderState state, CancellationToken cancellation )
        {
            ArgumentNullException.ThrowIfNull( api );
            ArgumentException.ThrowIfNullOrWhiteSpace( query );
            ArgumentNullException.ThrowIfNull( state );

            var search = api.Search( new()
            {
                Count = StationCount,
                Name = query,
                Order = StationOrderBy.Name,
            }, cancellation );

            return state with
            {
                IsLoading = false,
                Stations = [ .. await search.ToListAsync( cancellation ) ],
            };
        }
    }
}