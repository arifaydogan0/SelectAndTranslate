using System;
using System.Threading;
using System.Windows.Forms;

namespace SelectAndTranslate
{
    internal static class Program
    {
        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main()
        {
            bool AcikUygulamaVar = false;
            Mutex m = new Mutex(true, "SelectAndTranslate", out AcikUygulamaVar);
            if (AcikUygulamaVar)
            {
                Application.EnableVisualStyles();
                Application.SetCompatibleTextRenderingDefault(false);
                Application.Run(new Form1());
            }
            else
            {
                MessageBox.Show("Uygulamadan aynı anda bir tane açabilirsiniz!", "Uyarı", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
    }
}
