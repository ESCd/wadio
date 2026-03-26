using System.Diagnostics.CodeAnalysis;
using Microsoft.AspNetCore.WebUtilities;
using NetCord;
using NetCord.Rest;
using NetCord.Services.ComponentInteractions;
using Wadio.Platform.Api.Abstractions;
using Wadio.Platform.Discord.Abstractions;
using Wadio.Platform.Discord.Infrastructure;

namespace Wadio.Platform.Discord.Interactions;

internal sealed class SearchPagerComponent(
    IWadioApi api,
    IComponentContextFactory contextFactory ) : ComponentInteractionModule<ButtonInteractionContext>
{
    private const string NextButtonId = "search.next";
    private const string PreviousButtonId = "search.previous";

    public static IEnumerable<IMessageComponentProperties> Create( ComponentCreationContext context, SearchStationsParameters parameters, IReadOnlyCollection<Station> stations )
    {
        ArgumentNullException.ThrowIfNull( context );
        ArgumentNullException.ThrowIfNull( parameters );
        ArgumentNullException.ThrowIfNull( stations );

        var components = new List<IMessageComponentProperties>( stations.Count + 3 )
        {
            new ComponentSectionProperties(
                new LinkButtonProperties(context.CreateSearchUrl( parameters ).AbsoluteUri, "View in Browser"),
                [new TextDisplayProperties($"## Search for '{parameters.Name}'")] )
        };

        if( stations.Count is 0 )
        {
            components.Add( new ComponentContainerProperties
            {
                AccentColor = WadioColor.Default,
                Components = [ new TextDisplayProperties( "### No stations found." ) ]
            } );
        }
        else
        {
            components.AddRange( stations.Select( station => CreateItem( context, station ) ) );
            components.Add( new TextDisplayProperties( $"-# {(parameters.Offset ?? 0) + 1}..{(parameters.Offset ?? 0) + stations.Count}" ) );
        }

        components.Add( new ActionRowProperties( [
            new ButtonProperties(PreviousButtonId, WadioEmoji.ArrowBack, ButtonStyle.Primary)
            {
                Disabled = parameters.Offset is null or 0
            },
            new ButtonProperties(NextButtonId, WadioEmoji.ArrowForward, ButtonStyle.Primary)
            {
                Disabled = stations.Count is 0 || stations.Count < parameters.Count
            }] ) );

        return components;
    }

    public static IMessageComponentProperties CreateItem( ComponentCreationContext context, Station station )
    {
        ArgumentNullException.ThrowIfNull( context );
        ArgumentNullException.ThrowIfNull( station );

        return new ComponentContainerProperties
        {
            AccentColor = WadioColor.Convert( station.Id ),
            Components = [
                StationComponent.CreateHeader(context, station),

                new ActionRowProperties([
                    new ButtonProperties(
                        $"{StationComponent.PlayButtonId}:{station.Id}",
                        station.IsHls ? WadioEmoji.PlayDisabled : WadioEmoji.PlayCircle,
                        ButtonStyle.Primary )
                    {
                        Disabled = station.IsHls
                    },
                    new ButtonProperties($"{StationComponent.InfoButtonId}:{station?.Id}", WadioEmoji.ExpandCircle, ButtonStyle.Secondary)])]
        };
    }

    private async Task<RestMessage> RespondWithSearch( Func<SearchStationsParameters, SearchStationsParameters> configure )
    {
        await RespondAsync( InteractionCallback.DeferredModifyMessage );

        if( !TryParseParameters( out var parameters ) )
        {
            return await FollowupAsync( new()
            {
                Content = "Failed to parse the search parameters.",
                Flags = MessageFlags.Ephemeral
            } );
        }

        parameters = configure( parameters ) with
        {
            Count = SearchInteraction.MaxResults
        };

        var search = api.Stations.Search( parameters );

        var components = Create(
            await contextFactory.Create(),
            parameters,
            await search.ToListAsync() );

        return await Context.Message.ModifyAsync( modify =>
        {
            modify.Components = components;
            modify.Flags = MessageFlags.IsComponentsV2;
        } );
    }

    [ComponentInteraction( NextButtonId )]
    public Task Next( ) => RespondWithSearch( parameters => parameters with
    {
        Offset = Math.Max( 0, (parameters.Offset ?? 0) + (parameters.Count ?? SearchInteraction.MaxResults) )
    } );

    [ComponentInteraction( PreviousButtonId )]
    public Task Previous( ) => RespondWithSearch( parameters => parameters with
    {
        Offset = Math.Max( 0, (parameters.Offset ?? 0) - (parameters.Count ?? SearchInteraction.MaxResults) )
    } );

    private bool TryParseParameters( [NotNullWhen( true )] out SearchStationsParameters? parameters )
    {
        if( Context.Message.Components.Count is not 0 && Context.Message.Components[ 0 ] is ComponentSection { Accessory: var component } )
        {
            if( component is LinkButton { Url: var value } && Uri.TryCreate( value, UriKind.Absolute, out var url ) )
            {
                var query = QueryHelpers.ParseQuery( url.Query );
                if( query.Count is not 0 )
                {
                    parameters = new()
                    {
                        Count = SearchInteraction.MaxResults,
                        Name = query.GetValueOrDefault( nameof( SearchStationsParameters.Name ) ),
                        Codec = Enum.TryParse<Codec>( query.GetValueOrDefault( nameof( SearchStationsParameters.Codec ) ), out var codec ) ? codec : default( Codec? ),
                        Offset = uint.TryParse( query.GetValueOrDefault( nameof( SearchStationsParameters.Offset ) ), out var offset ) ? offset : default,
                        Order = Enum.TryParse<StationOrderBy>( query.GetValueOrDefault( nameof( SearchStationsParameters.Order ) ), out var order ) ? order : default( StationOrderBy? ),
                        Reverse = bool.TryParse( query.GetValueOrDefault( nameof( SearchStationsParameters.Reverse ) ), out var reverse ) && reverse,
                        Tags = query.GetValueOrDefault( nameof( SearchStationsParameters.Tags ), [] )!
                    };

                    return true;
                }
            }
        }

        parameters = default;
        return false;
    }
}