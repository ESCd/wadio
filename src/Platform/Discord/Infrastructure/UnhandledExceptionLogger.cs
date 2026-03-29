namespace Wadio.Platform.Discord.Infrastructure;

internal static partial class UnhandledExceptionLogging
{
    [LoggerMessage( Level = LogLevel.Error, Message = "Unhandled Exception (Terminating={terminating})" )]
    private static partial void OnUnhandledException( ILogger logger, bool terminating, Exception? exception );

    [LoggerMessage( Level = LogLevel.Error, Message = "Unhandled Exception (Terminating={terminating}) {error}" )]
#pragma warning disable LOGGEN036
    private static partial void OnUnhandledException( ILogger logger, bool terminating, object? error );
#pragma warning restore LOGGEN036

    public static THost UseUnhandledExceptionLogging<THost>( this THost app )
        where THost : IHost
    {
        var logger = app.Services.GetRequiredService<ILogger<THost>>();
        AppDomain.CurrentDomain.UnhandledException += ( _, e ) =>
        {
            if( e.ExceptionObject is Exception exception )
            {
                OnUnhandledException( logger, e.IsTerminating, exception );
                return;
            }

            OnUnhandledException( logger, e.IsTerminating, e.ExceptionObject );
        };

        return app;
    }
}