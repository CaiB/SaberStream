using System;

namespace SaberStream.Sources
{
    public static class CommonEvents
    {

        public delegate void ExitHandler(object? sender, EventArgs evt);
        public static event ExitHandler? Exit;
        public static void InvokeExit(object? sender, EventArgs evt)
        {
            Console.WriteLine("Exiting...");
            Exit?.Invoke(sender, evt);
        }
    }
}
