using System.Diagnostics.CodeAnalysis;
using Microsoft.AspNetCore.Components;

namespace Wadio.App.UI.Components;

/// <summary> Defines a type of component state. </summary>
public abstract record State<[DynamicallyAccessedMembers( DynamicallyAccessedMemberTypes.All )] T>;

/// <summary> Defines an abstract component that reacts to mutation to its <see cref="State"/>. </summary>
/// <typeparam name="T"> The type of <see cref="Components.State"/>. </typeparam>
public abstract class Stateful<[DynamicallyAccessedMembers( DynamicallyAccessedMemberTypes.All )] T> : ComponentBase, IDisposable
    where T : State<T>, new()
{
    private bool disposed;
    private SemaphoreSlim? locker = new( 1, 1 );
    private PersistingComponentStateSubscription? persistence;

    [Inject]
    private PersistentComponentState PersistentState { get; init; } = default!;

    /// <summary> The current state values. </summary>
    protected T State { get; private set; } = new();

    /// <inheritdoc/>
    public void Dispose( )
    {
        Dispose( true );
        GC.SuppressFinalize( this );
    }

    /// <inheritdoc/>
    protected virtual void Dispose( bool disposing )
    {
        if( !disposed )
        {
            if( disposing )
            {
                persistence?.Dispose();
                persistence = null;

                locker?.Dispose();
                locker = null;
            }

            disposed = true;
        }
    }

    /// <summary> Mutate the component's <see cref="State"/>. </summary>
    /// <param name="mutator"> A method that mutates the component's state. </param>
    /// <returns> A task that completes when the component has reacted to the mutation. </returns>
    protected async Task<bool> Mutate( Func<T, ValueTask<T>> mutator )
    {
        ObjectDisposedException.ThrowIf( disposed, this );
        ArgumentNullException.ThrowIfNull( mutator );

        await locker!.WaitAsync();
        try
        {
            var state = await mutator( State );
            if( State != state )
            {
                State = state;
                await InvokeAsync( StateHasChanged );

                return true;
            }

            return false;
        }
        finally
        {
            locker?.Release();
        }
    }

    /// <summary> Mutate the component's <see cref="State"/>. </summary>
    /// <param name="mutator"> A method that mutates the component's state. </param>
    /// <returns> A task that completes when the component has reacted to the mutation. </returns>
    protected async Task<bool> Mutate( Func<T, Task<T>> mutator )
    {
        ObjectDisposedException.ThrowIf( disposed, this );
        ArgumentNullException.ThrowIfNull( mutator );

        await locker!.WaitAsync();
        try
        {
            var state = await mutator( State );
            if( State != state )
            {
                State = state;
                await InvokeAsync( StateHasChanged );

                return true;
            }

            return false;
        }
        finally
        {
            locker?.Release();
        }
    }

    /// <summary> Mutate the component's <see cref="State"/>. </summary>
    /// <param name="mutator"> A method that mutates the component's state. </param>
    /// <returns> A task that completes when the component has reacted to the mutation. </returns>
    protected async Task<bool> Mutate( Func<T, T> mutator )
    {
        ObjectDisposedException.ThrowIf( disposed, this );
        ArgumentNullException.ThrowIfNull( mutator );

        await locker!.WaitAsync();
        try
        {
            var state = mutator( State );
            if( State != state )
            {
                State = state;
                await InvokeAsync( StateHasChanged );

                return true;
            }

            return false;
        }
        finally
        {
            locker?.Release();
        }
    }

    /// <summary> Mutate the component's <see cref="State"/>. </summary>
    /// <param name="mutator"> A method that mutates the component's state. </param>
    /// <returns> A task that completes when the component has reacted to the mutation. </returns>
    protected async Task<bool> Mutate( Func<T, IAsyncEnumerable<T>> mutator )
    {
        ObjectDisposedException.ThrowIf( disposed, this );
        ArgumentNullException.ThrowIfNull( mutator );

        await locker!.WaitAsync();
        try
        {
            var mutated = false;
            await foreach( var state in mutator( State ) )
            {
                if( State != state )
                {
                    mutated = true;

                    State = state;
                    await InvokeAsync( StateHasChanged );
                }
            }

            return mutated;
        }
        finally
        {
            locker?.Release();
        }
    }

    /// <summary> Mutate the component's <see cref="State"/>. </summary>
    /// <param name="mutator"> A method that mutates the component's state. </param>
    /// <returns> A task that completes when the component has reacted to the mutation. </returns>
    protected async Task<bool> Mutate( Func<T, IEnumerable<T>> mutator )
    {
        ObjectDisposedException.ThrowIf( disposed, this );
        ArgumentNullException.ThrowIfNull( mutator );

        await locker!.WaitAsync();
        try
        {
            var mutated = false;
            foreach( var state in mutator( State ) )
            {
                if( State != state )
                {
                    mutated = true;

                    State = state;
                    await InvokeAsync( StateHasChanged );
                }
            }

            return mutated;
        }
        finally
        {
            locker?.Release();
        }
    }

    /// <summary> Attempt to restore the component's <see cref="State"/> from <see cref="PersistentComponentState"/>. </summary>
    /// <param name="key"> The key to restore state from. </param>
    /// <returns> A value indicating whether a persisted state was restored. </returns>
    [UnconditionalSuppressMessage( "Trimming", "IL2026", Justification = "The generic type parameter 'T' is properly annotated to prevent trimming of metadata required by serialization." )]
    protected bool TryRestoreFromPersistence( string? key = default )
    {
        ObjectDisposedException.ThrowIf( disposed, this );

        key = $"{GetType().FullName}_{key}";
        if( PersistentState.TryTakeFromJson<T>( key, out var state ) )
        {
            State = state ?? new();
            return true;
        }

        persistence ??= PersistentState.RegisterOnPersisting( ( ) =>
        {
            PersistentState.PersistAsJson( key, State );
            return Task.CompletedTask;
        } );

        return false;
    }
}