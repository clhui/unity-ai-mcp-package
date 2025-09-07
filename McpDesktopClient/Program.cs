using System;
using System.Windows.Forms;

namespace McpDesktopClient
{
    internal static class Program
    {
        /// <summary>
        /// 应用程序的主入口点。
        /// </summary>
        [STAThread]
        static void Main()
        {
            try
            {
                Console.WriteLine("正在启动MCP桌面客户端...");
                Application.EnableVisualStyles();
                Application.SetCompatibleTextRenderingDefault(false);
                Console.WriteLine("正在创建主窗体...");
                var mainForm = new MainForm();
                Console.WriteLine("正在运行应用程序...");
                Application.Run(mainForm);
                Console.WriteLine("应用程序已退出。");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"启动失败: {ex.Message}");
                Console.WriteLine($"详细错误: {ex}");
                MessageBox.Show($"启动失败: {ex.Message}", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
    }
}