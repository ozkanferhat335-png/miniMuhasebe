using System;
using System.Windows.Forms;
using MiniMuhasebePro.Data;
using MiniMuhasebePro.UI.Forms;

namespace MiniMuhasebePro
{
    static class Program
    {
        [STAThread]
        static void Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);

            var dbInitializer = new DatabaseInitializer();
            dbInitializer.Initialize();

            Application.Run(new LoginForm());
        }
    }
}
