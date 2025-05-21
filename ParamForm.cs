using System.Runtime.Versioning;

namespace WindSoftInstaller
{
    [SupportedOSPlatform("windows")]
    partial class ParamForm : Form
    {
        // После закрытия формы здесь окажутся новые параметры
        // Инициализация пустым словарём
        public Dictionary<string, string> Parameters { get; private set; } = [];

        public ParamForm(Dictionary<string, string> existingParams)
        {
            InitializeComponent();

            // Настраиваем DataGridView: две текстовые колонки
            dgvParams.Columns.Clear();
            dgvParams.Columns.Add(new DataGridViewTextBoxColumn
            {
                Name = "colKey",
                HeaderText = "Ключ",
                AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill
            });
            dgvParams.Columns.Add(new DataGridViewTextBoxColumn
            {
                Name = "colValue",
                HeaderText = "Значение",
                AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill
            });

            // Заполняем из существующих параметров
            foreach (var kv in existingParams)
            {
                dgvParams.Rows.Add(kv.Key, kv.Value);
            }

            // Привязываем кнопку OK к обработчику сохранения
            btnOK.Click += BtnOK_Click;
            this.AcceptButton = btnOK;
            this.CancelButton = btnCancel;

        }

        private void BtnOK_Click(object? sender, EventArgs e)
        {
            var dict = new Dictionary<string, string>();

            foreach (DataGridViewRow row in dgvParams.Rows)
            {
                if (row.IsNewRow || row.Cells["colKey"] == null || row.Cells["colValue"] == null)
                    continue;

                var keyCell = row.Cells["colKey"];
                var valueCell = row.Cells["colValue"];

                // Безопасное преобразование
                string key = (keyCell.Value?.ToString() ?? "").Trim();
                string value = (valueCell.Value?.ToString() ?? "").Trim();

                // Игнорируем полностью пустые строки
                if (string.IsNullOrEmpty(key) && string.IsNullOrEmpty(value))
                    continue;

                // Требовать непустой ключ
                if (string.IsNullOrEmpty(key))
                {
                    MessageBox.Show("У каждой строки должен быть непустой ключ.", "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                // Если ключ уже есть, перезапишем
                dict[key] = value;
            }

            this.Parameters = dict;
            this.DialogResult = DialogResult.OK;
            this.Close();
        }
    }
}
