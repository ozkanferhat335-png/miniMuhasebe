using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;
using MiniMuhasebePro.Domain.Models;
using MiniMuhasebePro.UI.Forms.Modules;

namespace MiniMuhasebePro.UI.Forms
{
    public class MainForm : Form
    {
        private readonly User _currentUser;

        public MainForm(User currentUser)
        {
            _currentUser = currentUser;
            Text = "MiniMuhasebe Pro Dashboard";
            WindowState = FormWindowState.Maximized;

            var menu = new MenuStrip();
            var moduller = new ToolStripMenuItem("Modüller");
            var forms = new Dictionary<string, Form>
            {
                {"Banka Hesapları", new BankAccountsForm()},
                {"Hesap Hareketleri", new TransactionsForm()},
                {"EFT/Havale", new TransferForm()},
                {"Muhasebe Fişleri", new VoucherForm()},
                {"Mutabakat", new ReconciliationForm()},
                {"Raporlar", new ReportsForm()},
                {"Yönetim/Yetki", new AdminForm()}
            };

            foreach (var kv in forms)
            {
                var item = new ToolStripMenuItem(kv.Key);
                item.Click += (s, e) => kv.Value.ShowDialog();
                moduller.DropDownItems.Add(item);
            }

            menu.Items.Add(moduller);
            MainMenuStrip = menu;
            Controls.Add(menu);

            Controls.Add(new Label { Left = 20, Top = 60, AutoSize = true, Font = new Font("Segoe UI", 11, FontStyle.Bold), Text = "Hoşgeldiniz: " + _currentUser.Username });
            Controls.Add(new Label { Left = 20, Top = 90, AutoSize = true, Text = "Rol: " + _currentUser.Role });
            Controls.Add(new Button { Left = 20, Top = 130, Width = 170, Text = "Hareketleri Senkronize Et" });
            Controls.Add(new ProgressBar { Left = 200, Top = 130, Width = 300, Style = ProgressBarStyle.Marquee });
            Controls.Add(new Button { Left = 510, Top = 130, Width = 120, Text = "İptal" });
        }
    }
}
