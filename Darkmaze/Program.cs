using System;
using System.IO;

namespace Darkmaze
{
#if WINDOWS || LINUX
    /// <summary>
    /// The main class.
    /// </summary>
    public static class Program
    {
        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main()
        {
            try
            {
                using (var game = new Core())
                    game.Run();
            }
            catch (Exception e)
            {
                using (var writer = new StreamWriter("crash.log"))
                    writer.Write(e);
                throw;
            }
        }
    }
#endif
}
