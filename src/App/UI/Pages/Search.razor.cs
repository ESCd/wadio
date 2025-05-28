using System.Collections.Immutable;
using Wadio.App.UI.Abstractions;
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
    public ImmutableArray<Abstractions.Station> Stations { get; init; } = [];

    internal static async ValueTask<SearchState> Load( IWadioApi api, SearchState state )
    {
        ArgumentNullException.ThrowIfNull( api );
        ArgumentNullException.ThrowIfNull( state );

        return state with
        {
            Countries = [ .. await api.Countries.Get().Select(static country => new FilterOption(country.Name, country.Code)
            {
                Count = country.StationCount
            }).ToListAsync() ],
            IsLoaded = true,
            Languages = [ .. await api.Languages.Get().Select(static language => new FilterOption(language.Name, language.Code)
            {
                Count = language.StationCount
            }).ToListAsync()]
        };
    }

    internal static async IAsyncEnumerable<SearchState> Search( IStationsApi api, SearchStationsParameters parameters, SearchState state )
    {
        ArgumentNullException.ThrowIfNull( api );
        ArgumentNullException.ThrowIfNull( parameters );
        ArgumentNullException.ThrowIfNull( state );

        yield return state = (state with
        {
            IsSearching = true
        });

        var stations = await api.Search( parameters with { Count = StationCount } ).ToListAsync();
        yield return state with
        {
            IsSearching = false,
            Stations = [ .. stations ],
        };
    }
}

public sealed record FilterData
{
    public bool IsLoading { get; init; } = true;
    public ImmutableArray<FilterOption> Options { get; init; } = [];
}