namespace Wadio.App.Abstractions.Signals;

public abstract record Signal<TSignal> where TSignal : Signal<TSignal>;