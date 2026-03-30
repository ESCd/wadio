using LiteDB;

namespace Wadio.Platform.Sampler.Infrastructure;

internal static partial class BsonMapperExtensions
{
    public static BsonMapper ConfigureUlid( this BsonMapper mapper )
    {
        ArgumentNullException.ThrowIfNull( mapper );

        mapper.RegisterType<Ulid>(
            value => value.ToBsonValue(),
            value => new( value.AsBinary ) );

        mapper.RegisterType<Ulid?>(
            value => value.ToBsonValue(),
            value => !value.IsNull ? new( value.AsBinary ) : default );

        return mapper;
    }

    public static BsonValue ToBsonValue( this Ulid value ) => new( value.ToByteArray() );
    public static BsonValue ToBsonValue( this Ulid? value ) => value.HasValue ? new( value.Value.ToByteArray() ) : BsonValue.Null;
}