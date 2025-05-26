namespace Wadio.App.UI.Components;

[Flags]
public enum MarqueeMode : byte
{
    Always = 1,
    Active = 2,
    Hover = 4,
}

public enum MarqueeSpeed : byte
{
    Slowest = 0,
    Slower = 24,
    Slow = 68,
    Normal = 96,
    Fast = 124,
    Faster = 168,
    Fastest = 240,
}