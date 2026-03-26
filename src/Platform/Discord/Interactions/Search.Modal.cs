using NetCord;
using NetCord.Rest;
using NetCord.Services.ComponentInteractions;
using Wadio.Platform.Api.Abstractions;
using Wadio.Platform.Discord.Abstractions;
using Wadio.Platform.Discord.Infrastructure;

namespace Wadio.Platform.Discord.Interactions;

internal sealed class SearchModal(
    IWadioApi api,
    IComponentContextFactory contextFactory ) : ComponentInteractionModule<ModalInteractionContext>
{
    private const string ModalId = "station.search";

    private static readonly ModalParser<SearchStationsParameters> Parser = ModalParserBuilder.Create<SearchStationsParameters>()
        .Map( parameters => parameters.Codec, ComponentParser.EnumMenu<Codec>() )
        .Map( parameters => parameters.Name, ComponentParser.TextInput )
        .Map( parameters => parameters.Order, ComponentParser.EnumMenu<StationOrderBy>() )
        .Map( parameters => parameters.Reverse, ComponentParser.Checkbox )
        .Build();

    public static ModalProperties Create( SearchStationsParameters parameters ) => new( ModalId, "Station Search" )
    {
        Components = [
            new LabelProperties("Name", new TextInputProperties(nameof(parameters.Name), TextInputStyle.Short)
            {
                Value = parameters.Name,
            })
            {
                Description = "The name of the station.",
            },
            new LabelProperties("Codec", new StringMenuProperties(nameof(parameters.Codec))
            {
                Options = Enum.GetValues<Codec>().Select(codec => new StringMenuSelectOptionProperties(
                    EnumDisplay.GetName(codec),
                    codec.ToString())
                {
                    Default = codec == parameters.Codec
                }),
                Required = false,
            }),
            new LabelProperties("Order", new StringMenuProperties(nameof(parameters.Order))
            {
                Options = Enum.GetValues<StationOrderBy>().Select(order => new StringMenuSelectOptionProperties(
                    EnumDisplay.GetName(order),
                    order.ToString())
                {
                    Default = order == parameters.Order
                }),
                Required = false,
            }),
            new LabelProperties("Reverse", new CheckboxProperties(nameof(parameters.Reverse))
            {
                Default = parameters.Reverse is true
            }) ]
    };

    [ComponentInteraction( ModalId )]
    public async Task Search( )
    {
        await RespondAsync( InteractionCallback.DeferredMessage() );

        if( !Parser.TryParse( Context.Components, out var parameters ) )
        {
            await FollowupAsync( new()
            {
                Content = "Failed to parse the search parameters.",
            } );

            return;
        }

        parameters = parameters with
        {
            Count = SearchInteraction.MaxResults
        };

        var search = api.Stations.Search( parameters );
        await FollowupAsync( new()
        {
            Components = SearchPagerComponent.Create(
                await contextFactory.Create(),
                parameters,
                await search.ToListAsync() ),
            Flags = MessageFlags.IsComponentsV2,
        } );
    }
}