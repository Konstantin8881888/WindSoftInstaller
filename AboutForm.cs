using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using WindSoftInstaller.Services;

namespace WindSoftInstaller
{
    [SupportedOSPlatform("windows")]
    public class AboutForm : Form
    {
        private Panel mainPanel;
        private Panel contentPanel;
        private Label lblTitle;
        private Label lblVersion;
        private TextBox txtDescription;
        private LinkLabel linkSupport;
        private LinkLabel linkCopyright;
        private LinkLabel linkEmail;
        private Label lblSupport;
        private Label lblBtc;
        private TextBox txtBtc;
        private Button btnCopyBtc;
        private Label lblEth;
        private TextBox txtEth;
        private Button btnCopyEth;
        private Button btnSysReport;
        private Button btnClose;

        public AboutForm()
        {
            InitializeComponent();
            ApplyTheme(ThemeManager.CurrentTheme);

            // Подписываемся на событие смены темы
            ThemeManager.ThemeChanged += OnThemeChanged;
        }

        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            base.OnFormClosing(e);
            // Отписываемся от события при закрытии формы
            ThemeManager.ThemeChanged -= OnThemeChanged;
        }

        private void OnThemeChanged(Theme theme)
        {
            ApplyTheme(theme);
        }

        private void ApplyTheme(Theme theme)
        {
            this.BackColor = theme.FormBackColor;
            this.ForeColor = theme.ControlForeColor;

            // Применяем тему ко всем контролам
            mainPanel.BackColor = theme.FormBackColor;
            contentPanel.BackColor = theme.FormBackColor;

            // Labels
            lblTitle.BackColor = theme.FormBackColor;
            lblTitle.ForeColor = theme.ControlForeColor;
            lblVersion.BackColor = theme.FormBackColor;
            lblVersion.ForeColor = theme.ControlForeColor;
            lblSupport.BackColor = theme.FormBackColor;
            lblSupport.ForeColor = theme.ControlForeColor;
            lblBtc.BackColor = theme.FormBackColor;
            lblBtc.ForeColor = theme.ControlForeColor;
            lblEth.BackColor = theme.FormBackColor;
            lblEth.ForeColor = theme.ControlForeColor;

            // TextBoxes
            txtDescription.BackColor = theme.ControlBackColor;
            txtDescription.ForeColor = theme.ControlForeColor;
            txtBtc.BackColor = theme.ControlBackColor;
            txtBtc.ForeColor = theme.ControlForeColor;
            txtEth.BackColor = theme.ControlBackColor;
            txtEth.ForeColor = theme.ControlForeColor;

            // LinkLabels
            linkSupport.BackColor = theme.FormBackColor;
            linkSupport.ForeColor = theme.ControlForeColor;
            linkSupport.LinkColor = theme.ControlForeColor;
            linkCopyright.BackColor = theme.FormBackColor;
            linkCopyright.ForeColor = theme.ControlForeColor;
            linkCopyright.LinkColor = theme.ControlForeColor;
            linkEmail.BackColor = theme.FormBackColor;
            linkEmail.ForeColor = theme.ControlForeColor;
            linkEmail.LinkColor = theme.ControlForeColor;

            // Buttons
            btnCopyBtc.BackColor = theme.ButtonBackColor;
            btnCopyBtc.ForeColor = theme.ButtonForeColor;
            btnCopyEth.BackColor = theme.ButtonBackColor;
            btnCopyEth.ForeColor = theme.ButtonForeColor;
            btnSysReport.BackColor = theme.ButtonBackColor;
            btnSysReport.ForeColor = theme.ButtonForeColor;
            btnClose.BackColor = theme.ButtonBackColor;
            btnClose.ForeColor = theme.ButtonForeColor;
        }

        private void InitializeComponent()
        {
            // Общие настройки
            Text = Localization.T("AboutForm.Title");
            Size = new Size(500, 650);
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            MinimizeBox = false;
            StartPosition = FormStartPosition.CenterParent;
            Font = new Font("Segoe UI", 9F);
            Padding = new Padding(20);

            // Основной контейнер с вертикальным расположением
            mainPanel = new Panel
            {
                Dock = DockStyle.Fill,
                AutoScroll = true
            };
            Controls.Add(mainPanel);

            // Вертикальный контейнер для элементов
            contentPanel = new Panel
            {
                AutoSize = true,
                AutoSizeMode = AutoSizeMode.GrowAndShrink,
                Dock = DockStyle.Top
            };
            mainPanel.Controls.Add(contentPanel);

            // Текущая позиция Y для элементов
            int currentY = 0;

            // 1. Заголовок
            lblTitle = new Label
            {
                Text = Localization.T("AboutForm.lblTitle"),
                Font = new Font(Font.FontFamily, 16, FontStyle.Bold),
                AutoSize = true,
                Location = new Point(0, currentY),
                Width = contentPanel.Width - 40,
                TextAlign = ContentAlignment.MiddleCenter
            };
            contentPanel.Controls.Add(lblTitle);
            currentY += lblTitle.Height + 10;

            // 2. Версия
            const string HARDCODED_VERSION = "1.2.5";

            lblVersion = new Label
            {
                Text = string.Format(Localization.T("AboutForm.lblVersion"), HARDCODED_VERSION),
                AutoSize = true,
                Location = new Point(0, currentY),
                Width = contentPanel.Width - 40,
                TextAlign = ContentAlignment.MiddleCenter
            };
            contentPanel.Controls.Add(lblVersion);
            currentY += lblVersion.Height + 20;

            // 3. Описание
            txtDescription = new TextBox
            {
                Text = Localization.T("AboutForm.txtDescription"),
                Multiline = true,
                ReadOnly = true,
                BorderStyle = BorderStyle.FixedSingle,
                ScrollBars = ScrollBars.Vertical,
                Location = new Point(0, currentY),
                Width = contentPanel.Width - 40,
                Height = 80
            };

            // Сбрасываем выделение текста
            txtDescription.SelectionStart = 0;
            txtDescription.SelectionLength = 0;
            txtDescription.SelectionStart = txtDescription.Text.Length;

            // Предотвращаем выделение при фокусе
            txtDescription.Enter += (s, e) => {
                txtDescription.SelectionStart = txtDescription.Text.Length;
                txtDescription.SelectionLength = 0;
            };

            // Отключаем получение фокуса через Tab
            txtDescription.TabStop = false;

            contentPanel.Controls.Add(txtDescription);
            currentY += txtDescription.Height + 20;

            // 4. Ссылки
            linkSupport = new LinkLabel
            {
                Text = Localization.T("AboutForm.linkSupport"),
                AutoSize = true,
                Location = new Point(0, currentY)
            };
            linkSupport.LinkClicked += (_, __) =>
                Process.Start(new ProcessStartInfo("https://windsoftinstaller.site/donat/")
                { UseShellExecute = true });
            contentPanel.Controls.Add(linkSupport);
            currentY += linkSupport.Height + 5;

            linkCopyright = new LinkLabel
            {
                Text = Localization.T("AboutForm.linkCopyright"),
                AutoSize = true,
                Location = new Point(0, currentY)
            };
            linkCopyright.LinkClicked += (_, __) =>
                Process.Start(new ProcessStartInfo("https://windsoftinstaller.site/")
                { UseShellExecute = true });
            contentPanel.Controls.Add(linkCopyright);
            currentY += linkCopyright.Height + 5;

            linkEmail = new LinkLabel
            {
                Text = Localization.T("AboutForm.linkEmail"),
                AutoSize = true,
                Location = new Point(0, currentY)
            };
            linkEmail.LinkClicked += (_, __) =>
                Process.Start(new ProcessStartInfo("mailto:kamagoroff@gmail.com")
                { UseShellExecute = true });
            contentPanel.Controls.Add(linkEmail);
            currentY += linkEmail.Height + 30;

            // 5. Поддержка - заголовок
            lblSupport = new Label
            {
                Text = Localization.T("AboutForm.lblSupport"),
                AutoSize = true,
                Font = new Font(Font.FontFamily, 9F, FontStyle.Bold),
                Location = new Point(0, currentY)
            };
            contentPanel.Controls.Add(lblSupport);
            currentY += lblSupport.Height + 10;

            // 6. Адреса кошельков с кнопками копирования

            // BTC
            lblBtc = new Label
            {
                Text = "BTC:",
                AutoSize = true,
                Location = new Point(0, currentY)
            };
            contentPanel.Controls.Add(lblBtc);
            currentY += lblBtc.Height + 5;

            txtBtc = new TextBox
            {
                Text = "bc1qfamn5mcee7egfl8pu7w85ax7yvj5n9hxz8vxh4",
                ReadOnly = true,
                BorderStyle = BorderStyle.FixedSingle,
                Location = new Point(0, currentY),
                Width = contentPanel.Width - 40
            };
            // Сбрасываем выделение при фокусе
            txtBtc.Enter += (s, e) => txtBtc.SelectionLength = 0;
            contentPanel.Controls.Add(txtBtc);

            btnCopyBtc = new Button
            {
                Text = Localization.T("AboutForm.CopyButton"),
                Location = new Point(10, currentY + txtBtc.Height + 5),
                Size = new Size(120, 30)
            };
            btnCopyBtc.Click += (s, e) => CopyToClipboard(txtBtc.Text);
            contentPanel.Controls.Add(btnCopyBtc);
            currentY += txtBtc.Height + 40;

            // ETH
            lblEth = new Label
            {
                Text = "ETH (ERC20):",
                AutoSize = true,
                Location = new Point(0, currentY)
            };
            contentPanel.Controls.Add(lblEth);
            currentY += lblEth.Height + 5;

            txtEth = new TextBox
            {
                Text = "0xbC7fE973BFA32Ca0D4d4900ee94214E61F23271E",
                ReadOnly = true,
                BorderStyle = BorderStyle.FixedSingle,
                Location = new Point(0, currentY),
                Width = contentPanel.Width - 40
            };
            // Сбрасываем выделение при фокусе
            txtEth.Enter += (s, e) => txtEth.SelectionLength = 0;
            contentPanel.Controls.Add(txtEth);

            btnCopyEth = new Button
            {
                Text = Localization.T("AboutForm.CopyButton"),
                Location = new Point(10, currentY + txtEth.Height + 5),
                Size = new Size(120, 30)
            };
            btnCopyEth.Click += (s, e) => CopyToClipboard(txtEth.Text);
            contentPanel.Controls.Add(btnCopyEth);
            currentY += txtEth.Height + 50;

            // 7. Кнопка системного отчета
            btnSysReport = new Button
            {
                Text = Localization.T("AboutForm.btnSysReport"),
                Size = new Size(180, 35),
                Location = new Point((contentPanel.Width - 180) / 2, currentY)
            };
            btnSysReport.Click += BtnSysReport_Click;
            contentPanel.Controls.Add(btnSysReport);
            currentY += btnSysReport.Height + 20;

            // 8. Кнопка закрытия
            btnClose = new Button
            {
                Text = Localization.T("AboutForm.btnClose"),
                DialogResult = DialogResult.OK,
                Size = new Size(100, 35),
                Location = new Point((contentPanel.Width - 100) / 2, currentY)
            };
            contentPanel.Controls.Add(btnClose);
            currentY += btnClose.Height + 20;

            // Устанавливаем высоту контента
            contentPanel.Height = currentY;

            AcceptButton = btnClose;
        }

        // Обработчик для кнопки системного отчета
        private void BtnSysReport_Click(object sender, EventArgs e)
        {
            // Собираем информацию об ОС и .NET
            string os = RuntimeInformation.OSDescription;
            string arch = RuntimeInformation.OSArchitecture.ToString();
            string framework = RuntimeInformation.FrameworkDescription;
            string report = $"OS: {os} ({arch}){Environment.NewLine}.NET: {framework}";

            Clipboard.SetText(report);

            MessageBox.Show(
                Localization.T("AboutForm.SystemReportCopied"),
                Localization.T("AboutForm.SystemReport"),
                MessageBoxButtons.OK,
                MessageBoxIcon.Information
            );
        }

        private void CopyToClipboard(string text)
        {
            try
            {
                Clipboard.SetText(text);
                MessageBox.Show(
                    $"{text}\n\n{Localization.T("AboutForm.CopiedMessage")}",
                    Localization.T("AboutForm.CopiedTitle"),
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Information
                );
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    $"{Localization.T("AboutForm.CopyError")}: {ex.Message}",
                    Localization.T("errorTitle"),
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error
                );
            }
        }
    }
}