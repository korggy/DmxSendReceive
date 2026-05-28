namespace DmxSender;

public interface IMousePositionSource
{
    MousePosition GetPosition();

    ScreenBounds GetScreenBounds();
}
