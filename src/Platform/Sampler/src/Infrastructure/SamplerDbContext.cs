using LiteDB;
using Wadio.Platform.Sampler.Abstractions;

namespace Wadio.Platform.Sampler.Infrastructure;

internal sealed class SamplerDbContext( LiteDbOptions<SamplerDbContext> options ) : LiteDbContext( options )
{
    public LiteDbSet<MetadataSample> Meta => DbSet<MetadataSample>();

    protected override void OnCreatedDatabase( LiteDatabase database )
    {
        ArgumentNullException.ThrowIfNull( database );

        database.Timeout = TimeSpan.FromSeconds( 30 );
    }

    protected override void OnCreatingMapper( BsonMapper mapper )
    {
        ArgumentNullException.ThrowIfNull( mapper );
        LiteDBPragmas.I_AM_AWARE_MY_DATABASE_BREAKS_WHEN_I_USE_THIS();

        mapper.ConfigureUlid();

        mapper.Entity<MetadataSample>()
            .Id( x => x.Id );
    }
}