using System.Collections.Frozen;
using System.Diagnostics.CodeAnalysis;
using System.Linq.Expressions;
using System.Reflection;
using NetCord;

namespace Wadio.Platform.Discord.Infrastructure;

internal sealed class ModalParser<[DynamicallyAccessedMembers( DynamicallyAccessedMemberTypes.PublicProperties )] T>( IReadOnlyDictionary<string, ComponentMapperDelegate<T>> mappers )
    where T : class, new()
{
    public bool TryParse( IReadOnlyList<IModalComponent> components, [NotNullWhen( true )] out T? result )
    {
        ArgumentNullException.ThrowIfNull( components );

        if( components.Count is 0 )
        {
            result = default;
            return false;
        }

        result = new();
        foreach( var component in components )
        {
            if( component is Label label && label.Component is IInteractiveComponent interactive )
            {
                if( mappers.TryGetValue( interactive.CustomId, out var mapper ) )
                {
                    mapper( interactive, result );
                    continue;
                }
            }
        }

        return true;
    }
}

internal static class ModalParserBuilder
{
    public static ModalParserBuilder<T> Create<[DynamicallyAccessedMembers( DynamicallyAccessedMemberTypes.PublicProperties )] T>( )
        where T : class, new()
       => new();
}

internal sealed class ModalParserBuilder<[DynamicallyAccessedMembers( DynamicallyAccessedMemberTypes.PublicProperties )] T>
    where T : class, new()
{
    private readonly Dictionary<string, ComponentParserDelegate> parsers = [];

    public ModalParser<T> Build( )
    {
        return new( CreateMappers( parsers ) );

        static FrozenDictionary<string, ComponentMapperDelegate<T>> CreateMappers( IReadOnlyDictionary<string, ComponentParserDelegate> parsers )
        {
            ArgumentNullException.ThrowIfNull( parsers );

            if( parsers.Count is 0 )
            {
                return FrozenDictionary<string, ComponentMapperDelegate<T>>.Empty;
            }

            var mappers = new Dictionary<string, ComponentMapperDelegate<T>>();
            foreach( var (key, parser) in parsers )
            {
                var property = typeof( T ).GetProperty( key );
                if( property is not null )
                {
                    mappers.Add(
                        key,
                        ( component, value ) => property.SetValue( value, parser( component ) ) );
                }
            }

            return mappers.ToFrozenDictionary();
        }
    }

    public ModalParserBuilder<T> Map<TValue>( Expression<Func<T, TValue>> selector, ComponentParserDelegate<TValue> parser )
    {
        ArgumentNullException.ThrowIfNull( selector );
        ArgumentNullException.ThrowIfNull( parser );

        if( selector.Body is not MemberExpression { Member: PropertyInfo property } )
        {
            throw new ArgumentException( "The expression must be a property access.", nameof( selector ) );
        }

        parsers.Add( property.Name, component => parser( component )! );
        return this;
    }
}

internal static class ComponentParser
{
    public static readonly ComponentParserDelegate<bool?> Checkbox = Create<Checkbox, bool?>( component => component.Checked );
    public static readonly ComponentParserDelegate<string?> TextInput = Create<TextInput, string?>( component => component.Value );

    public static ComponentParserDelegate<T> Create<TComponent, T>( Func<TComponent, T> parse )
        where TComponent : IInteractiveComponent
    {
        ArgumentNullException.ThrowIfNull( parse );

        return component => parse( ( TComponent )component );
    }

    public static ComponentParserDelegate<T?> EnumMenu<T>( )
        where T : struct, Enum
        => Create<StringMenu, T?>( component =>
        {
            ArgumentNullException.ThrowIfNull( component );

            if( component.SelectedValues?.Count is null or 0 )
            {
                return default;
            }

            if( Enum.TryParse<T>( component.SelectedValues[ 0 ], out var result ) )
            {
                return result;
            }

            return default;
        } );
}

internal delegate object ComponentParserDelegate( IInteractiveComponent component );
internal delegate T ComponentParserDelegate<T>( IInteractiveComponent component );
internal delegate void ComponentMapperDelegate<T>( IInteractiveComponent component, T value );