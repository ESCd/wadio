using System.Collections.Immutable;
using Wadio.Platform.Api.Abstractions;

namespace Wadio.Platform.Web.UI.Components;

public sealed record StationCarouselData
{
    public bool IsLoading { get; init; } = true;
    public ImmutableArray<Station> Value { get; init; } = [];
}