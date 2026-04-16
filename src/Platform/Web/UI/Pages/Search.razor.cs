using System.Collections.Immutable;
using System.Runtime.CompilerServices;
using ESCd.AspNetCore.Components.Stateful;
using Wadio.Platform.Api.Abstractions;
using Wadio.Platform.Web.UI.Components.Forms;

namespace Wadio.Platform.Web.UI.Pages;

public sealed record SearchState : State<SearchState>
{
    public const uint StationCount = 24;

    public bool IsLoaded { get; init; }
    public bool IsSearching { get; init; }

    public ImmutableArray<FilterOption> Countries { get; init; } = [];
    public ImmutableArray<FilterOption> Languages { get; init; } = [];
    public ImmutableArray<Api.Abstractions.Station> Stations { get; init; } = [];
    public ImmutableArray<FilterOption> Tags { get; init; } = [];

    internal static async ValueTask<SearchState> Load( IWadioApi api, SearchState state, CancellationToken cancellation )
    {
        ArgumentNullException.ThrowIfNull( api );
        ArgumentNullException.ThrowIfNull( state );

        return state with
        {
            Countries = [ ..
                await api.Countries.Get( cancellation )
                    .Select( static country => new FilterOption( country.Name, country.Code, country.Count ) )
                    .ToListAsync( cancellation ) ],

            IsLoaded = OperatingSystem.IsBrowser(),
            Languages = [ ..
                await api.Languages.Get( cancellation )
                    .Select( static language => new FilterOption( language.Name, language.Code, language.Count ) )
                    .ToListAsync( cancellation ) ],

            Tags = [ ..
                await api.Tags.Get( cancellation )
                    .Select( static tag => new FilterOption( tag.Name, tag.Name, tag.Count ) )
                    .ToListAsync( cancellation ) ],
        };
    }

    internal static async IAsyncEnumerable<SearchState> ContinueSearch( IStationsApi api, SearchStationsParameters parameters, SearchState state, [EnumeratorCancellation] CancellationToken cancellation )
    {
        ArgumentNullException.ThrowIfNull( api );
        ArgumentNullException.ThrowIfNull( parameters );
        ArgumentNullException.ThrowIfNull( state );

        yield return state = (state with
        {
            IsSearching = true,
        });

        await foreach( var station in api.Search( parameters with { Count = StationCount }, cancellation ) )
        {
            yield return state = (state with
            {
                Stations = state.Stations.Add( station ),
            });
        }

        yield return state with
        {
            IsSearching = false,
        };
    }

    internal static async IAsyncEnumerable<SearchState> Search( IStationsApi api, SearchStationsParameters parameters, SearchState state, [EnumeratorCancellation] CancellationToken cancellation )
    {
        ArgumentNullException.ThrowIfNull( api );
        ArgumentNullException.ThrowIfNull( parameters );
        ArgumentNullException.ThrowIfNull( state );

        yield return state = (state with
        {
            IsSearching = true,
            Stations = []
        });

        await foreach( var mutation in ContinueSearch( api, parameters, state, cancellation ) )
        {
            yield return state = mutation;
        }

        if( state.IsSearching )
        {
            yield return state with
            {
                IsSearching = false,
            };
        }
    }
}