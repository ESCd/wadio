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
    Slowest = 240,
    Slower = 168,
    Slow = 124,
    Normal = 96,
    Fast = 68,
    Faster = 24,
    Fastest = 0,
}