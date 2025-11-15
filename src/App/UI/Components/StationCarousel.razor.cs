using System.Collections.Immutable;

namespace Wadio.App.UI.Components;

public sealed record StationCarouselData
{
    public bool IsLoading { get; init; } = true;
    public ImmutableArray<Abstractions.Api.Station> Value { get; init; } = [];
}