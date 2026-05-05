namespace HamburburModManager;

public class Program
{
    public static void Main()
    {
        Renderer renderer     = new();
        Thread   renderThread = new(renderer.Start().Wait);
        renderThread.Start();
    }
}