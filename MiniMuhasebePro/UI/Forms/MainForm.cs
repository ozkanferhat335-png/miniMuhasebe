using System.Drawing;
using System.Windows.Forms;

namespace MiniMuhasebePro.UI.Forms
{
    public class MainForm : Form
    {
        public MainForm()
        {
            Text = "MiniMuhasebe Pro Dashboard";
            WindowState = FormWindowState.Maximized;

            var tabs = new TabControl { Dock = DockStyle.Fill };
            tabs.TabPages.Add(BuildPage("Dashboard"));
            tabs.TabPages.Add(BuildPage("Banka Hesapları"));
            tabs.TabPages.Add(BuildPage("Hesap Hareketleri"));
            tabs.TabPages.Add(BuildPage("EFT / Havale"));
            tabs.TabPages.Add(BuildPage("Muhasebe Fişleri"));
            tabs.TabPages.Add(BuildPage("Mutabakat"));
            tabs.TabPages.Add(BuildPage("Raporlar"));
            tabs.TabPages.Add(BuildPage("Yönetim / Yetki"));

            Controls.Add(tabs);
        }

        private TabPage BuildPage(string title)
        {
            var page = new TabPage(title);
            page.Controls.Add(new Label
            {
                Text = title + " modülü hazır.",
                AutoSize = true,
                Left = 20,
                Top = 20,
                Font = new Font("Segoe UI", 11, FontStyle.Bold)
            });
            page.Controls.Add(new ProgressBar { Left = 20, Top = 55, Width = 400, Style = ProgressBarStyle.Continuous, Value = 25 });
            page.Controls.Add(new Button { Left = 430, Top = 52, Width = 120, Text = "İptal" });
            return page;
        }
    }
}
