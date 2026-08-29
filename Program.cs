using System.Runtime.InteropServices;

namespace HidTest
{
    internal static class Program
    {
        private const int SwHide = 0;

        [DllImport("kernel32.dll")]
        private static extern IntPtr GetConsoleWindow();

        [DllImport("kernel32.dll")]
        private static extern bool FreeConsole();

        [DllImport("kernel32.dll")]
        private static extern uint GetConsoleProcessList(uint[] processList, uint count);

        [DllImport("user32.dll")]
        private static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);

        [STAThread]
        private static int Main(string[] args)
        {
            // 有參數 → 命令列模式；沒有參數 → 圖形介面
            if (args.Length > 0)
            {
                try { Console.OutputEncoding = System.Text.Encoding.UTF8; } catch { }
                return ConsoleApp.Run(args);
            }

            HideOwnConsole();
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new MainForm());
            return 0;
        }

        /// <summary>
        /// 圖形介面模式下收掉主控台視窗。
        /// 若主控台是從 cmd / PowerShell 繼承而來 (附著的行程不只自己一個)，
        /// 就只卸離而不隱藏，以免把使用者的終端機視窗一起藏起來。
        /// </summary>
        private static void HideOwnConsole()
        {
            try
            {
                IntPtr window = GetConsoleWindow();
                if (window == IntPtr.Zero) return;

                var buffer = new uint[4];
                uint attached = GetConsoleProcessList(buffer, (uint)buffer.Length);
                if (attached == 1) ShowWindow(window, SwHide);

                FreeConsole();
            }
            catch { }
        }
    }
}
