namespace DmxSender;

public sealed record ScreenBounds(int Left, int Top, int Width, int Height)
{
    public int Right => Left + Width - 1;

    public int Bottom => Top + Height - 1;
}
