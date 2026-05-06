using System;
using System.Drawing;
using System.Windows.Forms;
using MiniMuhasebePro.Services;

namespace MiniMuhasebePro.UI.Forms
{
    public class LoginForm : Form
    {
        private readonly TextBox _txtUser = new TextBox();
        private readonly TextBox _txtPass = new TextBox();
        private readonly AuthService _authService = new AuthService();

        public LoginForm()
        {
            Text = "MiniMuhasebe Pro - Giriş";
            Size = new Size(360, 240);
            Controls.Add(new Label { Left = 20, Top = 30, Text = "Kullanıcı Adı" });
            Controls.Add(new Label { Left = 20, Top = 75, Text = "Şifre" });
            Controls.Add(new Label { Left = 20, Top = 160, Width = 300, Text = "Demo: admin / admin123" });

            _txtUser.Left = 130; _txtUser.Top = 25; _txtUser.Width = 180;
            _txtPass.Left = 130; _txtPass.Top = 70; _txtPass.Width = 180; _txtPass.PasswordChar = '*';
            var btnLogin = new Button { Left = 130, Top = 115, Width = 180, Text = "Giriş" };
            btnLogin.Click += Login;

            Controls.Add(_txtUser);
            Controls.Add(_txtPass);
            Controls.Add(btnLogin);
        }

        private void Login(object sender, EventArgs e)
        {
            var user = _authService.Login(_txtUser.Text, _txtPass.Text);
            if (user == null)
            {
                MessageBox.Show("Giriş başarısız.");
                return;
            }

            Hide();
            new MainForm(user).ShowDialog();
            Close();
        }
    }
}
