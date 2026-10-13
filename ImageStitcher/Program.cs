using System;
using System.Threading;
using System.Windows.Forms;

namespace ImageStitcher
{
    internal static class Program
    {
        [STAThread]
        private static void Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.SetUnhandledExceptionMode(UnhandledExceptionMode.CatchException);

            Application.ThreadException += OnThreadException;
            AppDomain.CurrentDomain.UnhandledException += OnDomainException;

            Application.Run(new MainForm());
        }

        private static void OnThreadException(object sender, ThreadExceptionEventArgs e)
        {
            Report(e.Exception);
        }

        private static void OnDomainException(object sender, UnhandledExceptionEventArgs e)
        {
            Report(e.ExceptionObject as Exception);
        }

        private static void Report(Exception error)
        {
            string message = error == null ? "未知错误。" : error.Message;

            if (IsNativeLoadFailure(error))
            {
                message =
                    "OpenCV 本机库 (OpenCvSharpExtern.dll) 加载失败。\r\n\r\n" +
                    "请确认：\r\n" +
                    "  1. OpenCvSharpExtern.dll 与 ImageStitcher.exe 位于同一目录；\r\n" +
                    "  2. 程序以 x64 方式构建和运行；\r\n" +
                    "  3. 已安装 Visual C++ 2015-2022 运行时。\r\n\r\n" +
                    "原始错误：" + message;
            }

            MessageBox.Show(message, "图像拼接 — 未处理错误",
                MessageBoxButtons.OK, MessageBoxIcon.Error);
        }

        /// <summary>
        /// Walks the exception chain looking for the signatures of a failed native load.
        /// </summary>
        internal static bool IsNativeLoadFailure(Exception error)
        {
            for (Exception current = error; current != null; current = current.InnerException)
            {
                if (current is DllNotFoundException ||
                    current is BadImageFormatException ||
                    current is EntryPointNotFoundException)
                {
                    return true;
                }
            }
            return false;
        }
    }
}
