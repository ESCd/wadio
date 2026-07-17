using System.Text;
using ESCd.Extensions.Http;
using Microsoft.Extensions.ObjectPool;
using Microsoft.Extensions.Options;
using Wadio.Platform.Discord.Abstractions;
using Wadio.Platform.Hosting.Abstractions;

namespace Wadio.Platform.Discord.Infrastructure;

internal sealed class ComponentContextFactory(
    IOptionsMonitor<PlatformOptions> options,
    ObjectPool<QueryStringBuilder> queryStringBuilders,
    ObjectPool<StringBuilder> stringBuilders ) : IComponentContextFactory
{
    public ValueTask<ComponentCreationContext> Create( ) => new( new ComponentCreationContext(
        options.CurrentValue,
        queryStringBuilders,
        stringBuilders ) );
}