using System;
using System.Windows.Forms;

namespace Voltvane
{
    static class Program
    {
        [STAThread]
        static void Main(string[] args)
        {
            Application.SetHighDpiMode(HighDpiMode.PerMonitorV2);
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);

            // Handle OAuth deep link: voltvane://auth?access_token=...
            if (args.Length > 0 && args[0].StartsWith("voltvane://"))
            {
                var uri = new Uri(args[0]);
                // Write token to temp file so running instance can pick it up
                var tokenFile = System.IO.Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                    "Voltvane", "oauth_callback.txt");
                System.IO.Directory.CreateDirectory(System.IO.Path.GetDirectoryName(tokenFile)!);
                System.IO.File.WriteAllText(tokenFile, args[0]);
                return; // Don't open a new window
            }

            Application.Run(new MainWindow());
        }
    }
}
