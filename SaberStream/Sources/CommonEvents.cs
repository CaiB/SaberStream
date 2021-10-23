using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SaberStream.Sources
{
    public static class CommonEvents
    {

        public delegate void ExitHandler(object? sender, EventArgs evt);
        public static event ExitHandler? Exit;
        public static void InvokeExit(object? sender, EventArgs evt) => Exit?.Invoke(sender, evt);

    }
}
