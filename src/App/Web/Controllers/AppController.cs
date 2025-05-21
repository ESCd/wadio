using Microsoft.AspNetCore.Http.Timeouts;
using Microsoft.AspNetCore.Mvc;

namespace Wadio.App.Web.Controllers;

/// <summary> General App Controller. </summary>
public sealed class AppController : Controller
{
    /// <summary> Default App Route. </summary>
    [ApiExplorerSettings( IgnoreApi = true )]
    [RequestTimeout( 1000 * 60 * 2 )]
    public ViewResult Index( ) => View( "_App" );
}