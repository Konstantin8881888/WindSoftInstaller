using System.Runtime.Versioning;

namespace WindSoftInstaller
{
    [SupportedOSPlatform("windows")]
    public class EulaForm : Form
    {
        private readonly Button btnAccept;
        private readonly Button btnDecline;
        private readonly TextBox txtEula;
        private readonly Panel btnPanel;

        public bool Accepted { get; private set; }

        public EulaForm()
        {
            Text = Localization.T("EulaForm.Title");
            Size = new Size(600, 500);
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            MinimizeBox = false;
            StartPosition = FormStartPosition.CenterScreen;
            Font = new Font("Segoe UI", 9F);

            // --- Текст соглашения ---
            Text = Localization.T("EulaForm.Title");

            // 2) Создаём TextBox без текста
            txtEula = new TextBox
            {
                Multiline = true,
                ReadOnly = true,
                ScrollBars = ScrollBars.Vertical,
                Dock = DockStyle.Fill
            };
            Controls.Add(txtEula);

            // 3) Отдельно присваиваем локализованный текст
            txtEula.Text = Localization.T("EulaForm.Text");

            Controls.Add(txtEula);
            txtEula.TabStop = false;
            txtEula.SelectionStart = 0;
            txtEula.SelectionLength = 0;
            txtEula.HideSelection = true;


            // --- Панель для кнопок ---
            btnPanel = new Panel
            {
                Dock = DockStyle.Bottom,
                Height = 50
            };
            Controls.Add(btnPanel);  // обязательно добавить панель в Controls

            // --- Кнопка "Принять" ---
            btnAccept = new Button
            {
                Text = Localization.T("EulaForm.agree"),
                Size = new Size(100, 30),
                Anchor = AnchorStyles.Top | AnchorStyles.Right,
                Location = new Point(
                    btnPanel.ClientSize.Width - 220,
                    (btnPanel.Height - 30) / 2
                ),
                DialogResult = DialogResult.OK
            };
            btnAccept.Click += (_, __) => { Accepted = true; Close(); };
            btnPanel.Controls.Add(btnAccept);

            // --- Кнопка "Отказаться" ---
            btnDecline = new Button
            {
                Text = Localization.T("EulaForm.decline"),
                Size = new Size(100, 30),
                Anchor = AnchorStyles.Top | AnchorStyles.Right,
                Location = new Point(
                    btnPanel.ClientSize.Width - 110,
                    (btnPanel.Height - 30) / 2
                ),
                DialogResult = DialogResult.Cancel
            };
            btnDecline.Click += (_, __) => { Accepted = false; Close(); };
            btnPanel.Controls.Add(btnDecline);

            // --- Назначаем стандартные кнопки формы ---
            AcceptButton = btnAccept;
            CancelButton = btnDecline;
        }
    }
}
