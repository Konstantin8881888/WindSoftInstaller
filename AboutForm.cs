using System.Diagnostics;
using System.Runtime.InteropServices;

namespace WindSoftInstaller
{
    public class AboutForm : Form
    {
        public AboutForm()
        {
            // Общие настройки
            Text = "О программе";
            Size = new Size(450, 400);
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            MinimizeBox = false;
            StartPosition = FormStartPosition.CenterParent;
            Font = new Font("Segoe UI", 9F);

            const int left = 20;
            const int vGap = 10;

            //1) Логотип
            var logoBytes = Properties.Resources._04e64;   // byte[]
            Image logoImage;
            using (var ms = new MemoryStream(logoBytes))
            {
                logoImage = Image.FromStream(ms);
            }

            var logo = new PictureBox
            {
               Image = logoImage,
               SizeMode = PictureBoxSizeMode.Zoom,
               Location = new Point(left, 20),
               Size = new Size(64, 64)
            };
            Controls.Add(logo);

            // 2) Заголовок
            var lblTitle = new Label
            {
                Text = "WindSoft Installer",
                Font = new Font(Font.FontFamily, 14, FontStyle.Bold),
                AutoSize = true
            };
            int titleY = 20 + (64 - lblTitle.PreferredHeight) / 2;
            lblTitle.Location = new Point(100, titleY);
            Controls.Add(lblTitle);

            // 3) Версия
            var lblVersion = new Label
            {
                Text = $"Версия: {Application.ProductVersion}",
                AutoSize = true,
                Location = new Point(100, titleY + lblTitle.PreferredHeight + vGap)
            };
            Controls.Add(lblVersion);

            // 4) Описание
            var txtDescription = new TextBox
            {
                Multiline = true,
                ReadOnly = true,
                BorderStyle = BorderStyle.None,
                BackColor = this.BackColor,
                Text = "Установщик WindSoft — лёгкий и удобный способ массово инсталлировать ваши любимые программы в пару кликов.",
                Location = new Point(left,
                    Math.Max(20 + 64, lblVersion.Location.Y + lblVersion.PreferredHeight) + vGap),
                Size = new Size(400, 60)
            };
            Controls.Add(txtDescription);

            // 5) Поддержать проект
            var linkSupport = new LinkLabel
            {
                Text = "Поддержать проект",
                AutoSize = true,
                Location = new Point(left, txtDescription.Location.Y + txtDescription.Height + vGap)
            };
            linkSupport.LinkClicked += (_, __) =>
                Process.Start(new ProcessStartInfo("https://yourprojectsite.example.com/donate")
                { UseShellExecute = true });
            Controls.Add(linkSupport);

            // 6) Авторские права
            var linkCopyright = new LinkLabel
            {
                Text = "© 2025 WindSoft. Все права защищены.",
                AutoSize = true,
                Location = new Point(left,
                    linkSupport.Location.Y + linkSupport.PreferredHeight + vGap)
            };
            linkCopyright.LinkClicked += (_, __) =>
                Process.Start(new ProcessStartInfo("https://andreytsvla.pythonanywhere.com/")
                { UseShellExecute = true });
            Controls.Add(linkCopyright);

            // 7) E-mail поддержки
            var linkEmail = new LinkLabel
            {
                Text = "Связаться: kamagoroff@gmail.com",
                AutoSize = true,
                Location = new Point(left,
                    linkCopyright.Location.Y + linkCopyright.PreferredHeight + vGap)
            };
            linkEmail.LinkClicked += (_, __) =>
                Process.Start(new ProcessStartInfo("mailto:kamagoroff@gmail.com")
                { UseShellExecute = true });
            Controls.Add(linkEmail);

            // 8) Системный отчет
            var btnSysReport = new Button
            {
                Text = "Системный отчет",
                Size = new Size(130, 30),
                Location = new Point(left,
                    linkEmail.Location.Y + linkEmail.PreferredHeight + 2 * vGap)
            };
            btnSysReport.Click += BtnSysReport_Click;
            Controls.Add(btnSysReport);

            // 9) Закрыть
            var btnClose = new Button
            {
                Text = "Закрыть",
                DialogResult = DialogResult.OK,
                Size = new Size(80, 30),
                Anchor = AnchorStyles.Bottom | AnchorStyles.Right
            };
            btnClose.Location = new Point(
                ClientSize.Width - btnClose.Width - left,
                ClientSize.Height - btnClose.Height - vGap
            );
            Controls.Add(btnClose);

            int supportTop = linkEmail.Location.Y + linkEmail.PreferredHeight + 3 * vGap;

            var lblSupport = new Label
            {
                Text = "Поддержать проект (адреса кошельков):",
                AutoSize = true,
                Font = new Font(Font.FontFamily, 9F, FontStyle.Bold),
                Location = new Point(left, supportTop)
            };
            Controls.Add(lblSupport);

            // Bitcoin
            var txtBtc = new TextBox
            {
                ReadOnly = true,
                BorderStyle = BorderStyle.None,
                BackColor = this.BackColor,
                Text = "BTC: bc1qfamn5mcee7egfl8pu7w85ax7yvj5n9hxz8vxh4",
                Location = new Point(left, lblSupport.Bottom + vGap),
                Width = 400
            };
            Controls.Add(txtBtc);

            // Ethereum
            var txtEth = new TextBox
            {
                ReadOnly = true,
                BorderStyle = BorderStyle.None,
                BackColor = this.BackColor,
                Text = "ETH: 0xbC7fE973BFA32Ca0D4d4900ee94214E61F23271E",
                Location = new Point(left, txtBtc.Bottom + vGap),
                Width = 400
            };
            Controls.Add(txtEth);

            AcceptButton = btnClose;
        }

        private void BtnSysReport_Click(object sender, EventArgs e)
        {
            // Собираем информацию об ОС и .NET
            string os = RuntimeInformation.OSDescription;
            string arch = RuntimeInformation.OSArchitecture.ToString();
            string framework = RuntimeInformation.FrameworkDescription;
            string report = $"OS: {os} ({arch}){Environment.NewLine}.NET: {framework}";

            Clipboard.SetText(report);

            MessageBox.Show(
                "Системный отчет скопирован в буфер обмена.",
                "Системный отчет",
                MessageBoxButtons.OK,
                MessageBoxIcon.Information
            );
        }
    }
}
