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
            Text = "Лицензионное соглашение";
            Size = new Size(600, 500);
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            MinimizeBox = false;
            StartPosition = FormStartPosition.CenterScreen;
            Font = new Font("Segoe UI", 9F);

            // --- Текст соглашения ---
            txtEula = new TextBox
            {
                Multiline = true,
                ReadOnly = true,
                ScrollBars = ScrollBars.Vertical,
                Dock = DockStyle.Fill,
                Text = "Текст лицензионного соглашения…"
            };
            Controls.Add(txtEula);

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
                Text = "Я принимаю",
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
                Text = "Отказаться",
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
