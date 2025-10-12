using System.Collections.Immutable;
using Wadio.App.Abstractions.Api;
using Wadio.App.UI.Components;
using Wadio.App.UI.Components.Forms;

namespace Wadio.App.UI.Pages;

public sealed record SearchState : State<SearchState>
{
    public const uint StationCount = 24;

    public bool IsLoaded { get; init; }
    public bool IsSearching { get; init; }

    public ImmutableArray<FilterOption> Countries { get; init; } = [];
    public ImmutableArray<FilterOption> Languages { get; init; } = [];
    public ImmutableArray<Abstractions.Api.Station> Stations { get; init; } = [];
    public ImmutableArray<FilterOption> Tags { get; init; } = [];

    internal static async ValueTask<SearchState> Load( IWadioApi api, SearchState state )
    {
        ArgumentNullException.ThrowIfNull( api );
        ArgumentNullException.ThrowIfNull( state );

        return state with
        {
            Countries = [ .. await api.Countries.Get().Select(static country => new FilterOption(country.Name, country.Code)
            {
                Count = country.Count
            }).ToListAsync() ],
            IsLoaded = true,
            Languages = [ .. await api.Languages.Get().Select(static language => new FilterOption(language.Name, language.Code)
            {
                Count = language.Count
            }).ToListAsync()],
            Tags = [ .. await api.Tags.Get().Select(static tag => new FilterOption(tag.Name, tag.Name)
            {
                Count = tag.Count
            }).ToListAsync()],
        };
    }

    internal static async IAsyncEnumerable<SearchState> ContinueSearch( IStationsApi api, SearchStationsParameters parameters, SearchState state )
    {
        ArgumentNullException.ThrowIfNull( api );
        ArgumentNullException.ThrowIfNull( parameters );
        ArgumentNullException.ThrowIfNull( state );

        await foreach( var station in api.Search( parameters with { Count = StationCount } ) )
        {
            yield return state = (state with
            {
                Stations = state.Stations.Add( station ),
            });
        }
    }

    internal static async IAsyncEnumerable<SearchState> Search( IStationsApi api, SearchStationsParameters parameters, SearchState state )
    {
        ArgumentNullException.ThrowIfNull( api );
        ArgumentNullException.ThrowIfNull( parameters );
        ArgumentNullException.ThrowIfNull( state );

        yield return state = (state with
        {
            IsSearching = true,
            Stations = []
        });

        await foreach( var mutation in ContinueSearch( api, parameters, state ) )
        {
            yield return state = mutation with
            {
                IsSearching = true,
            };
        }

        yield return state with
        {
            IsSearching = false,
        };
    }
}