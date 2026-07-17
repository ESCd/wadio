using LiteDB;
using Wadio.Platform.Sampler.Abstractions;

namespace Wadio.Platform.Sampler.Infrastructure;

internal sealed class SamplerDbContext( LiteDbOptions<SamplerDbContext> options ) : LiteDbContext( options )
{
    public LiteDbSet<MetadataSample> Meta => DbSet<MetadataSample>();

    protected override void OnCreatingDatabase( LiteDatabase database )
    {
        ArgumentNullException.ThrowIfNull( database );

        database.Timeout = TimeSpan.FromSeconds( 30 );
    }

    protected override void OnCreatingMapper( BsonMapper mapper )
    {
        ArgumentNullException.ThrowIfNull( mapper );

        mapper.ConfigureUlid();

        mapper.Entity<MetadataSample>()
            .Id( x => x.Id );
    }
}