using Microsoft.AspNetCore.Mvc;
using Wadio.App.Web.Endpoints;

namespace Wadio.App.Web;

public static class WebEndpointExtensions
{
    public static RouteGroupBuilder MapWadioApi( this WebApplication app, string prefix = "/api" )
    {
        ArgumentNullException.ThrowIfNull( app );
        ArgumentException.ThrowIfNullOrWhiteSpace( prefix );

        var api = app.MapGroup( prefix )
            .ProducesValidationProblem()
            .WithMetadata( new ApiControllerAttribute() );

        api.MapGet( "/version", ApiEndpoints.Version );

        MapCountriesApi( api );
        MapLanguagesApi( api );
        MapStationsApi( api );
        MapTagsApi( api );

        return api;

        static RouteGroupBuilder MapCountriesApi( RouteGroupBuilder api )
        {
            ArgumentNullException.ThrowIfNull( api );

            var countries = api.MapGroup( "/countries" );

            countries.MapGet( "/", CountryApiEndpoints.Get )
                .WithDescription( "Retrieve Countries." );

            return countries;
        }

        static RouteGroupBuilder MapLanguagesApi( RouteGroupBuilder api )
        {
            ArgumentNullException.ThrowIfNull( api );

            var countries = api.MapGroup( "/languages" );

            countries.MapGet( "/", LanguageApiEndpoints.Get )
                .WithDescription( "Retrieve Languages." );

            return countries;
        }

        static RouteGroupBuilder MapStationsApi( RouteGroupBuilder api )
        {
            ArgumentNullException.ThrowIfNull( api );

            var stations = api.MapGroup( "/stations" );

            stations.MapGet( "/", StationApiEndpoints.Search )
                .WithDescription( "Retrieve Stations matching the given parameters." );

            stations.MapGet( "/{stationId:guid}", StationApiEndpoints.Get )
                .WithDescription( "Retrieve a Station by it's identifier." );

            stations.MapPost( "/{stationId:guid}/track", StationApiEndpoints.Track )
                .WithDescription( "Track a Station by it's identifier." );

            stations.MapPost( "/{stationId:guid}/vote", StationApiEndpoints.Vote )
                .WithDescription( "Vote for a Station by it's identifier." );

            stations.MapGet( "/random", StationApiEndpoints.Random )
                .WithDescription( "Retrieve a random Station." );

            return stations;
        }

        static RouteGroupBuilder MapTagsApi( RouteGroupBuilder api )
        {
            ArgumentNullException.ThrowIfNull( api );

            var tags = api.MapGroup( "/tags" );

            tags.MapGet( "/", TagApiEndpoints.Get )
                .WithDescription( "Retrieve Tags." );

            return tags;
        }
    }
}