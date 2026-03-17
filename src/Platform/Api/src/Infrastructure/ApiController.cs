using System.Net.Mime;
using Microsoft.AspNetCore.Http.Timeouts;
using Microsoft.AspNetCore.Mvc;

namespace Wadio.Platform.Api.Infrastructure;

/// <summary> Defines a Controller for API Endpoints. </summary>
[ApiController]
[Produces( MediaTypeNames.Application.Json )]
[RequestTimeout( 1000 * 60 * 2 )]
public abstract class ApiController : ControllerBase;