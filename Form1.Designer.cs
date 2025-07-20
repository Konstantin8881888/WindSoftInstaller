using Microsoft.Extensions.Logging;
using Timer = System.Windows.Forms.Timer;

namespace WindSoftInstaller
{
    partial class Form1
    {
        /// Обязательная переменная конструктора.
        private System.ComponentModel.IContainer components = null;

        /// Освободить все используемые ресурсы.
        /// <param name="disposing">истинно, если управляемый ресурс должен быть удален; иначе ложно.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Код, автоматически созданный конструктором форм Windows

        /// Требуемый метод для поддержки конструктора — не изменяйте содержимое этого метода с помощью редактора кода.
        private void InitializeComponent()
        {
            this.dataGridViewPrograms = new System.Windows.Forms.DataGridView();
            btnInstall = new Button();
            progressBar = new ProgressBar();
            folderBrowserDialog = new FolderBrowserDialog();
            btnBrowse = new Button();
            txtInstallPath = new TextBox();
            lblStatus = new Label();

            SuspendLayout();
            // ─── Блок: MenuStrip + позиционирование ─────────────────────
            // Создаем и докируем MenuStrip
            _logger.LogInformation("InitializeComponent: начало создания UI-элементов");
            var menuStrip = new MenuStrip();
            menuStrip.Dock = DockStyle.Top;
            this.MainMenuStrip = menuStrip;
            this.Controls.Add(menuStrip);
            // Логируем создание и добавление MenuStrip
            _logger.LogDebug("MenuStrip создан и добавлен на форму");

            // Настраиваем пункты меню
            var fileMenu = new ToolStripMenuItem("Файл");
            var helpMenu = new ToolStripMenuItem("Справка");
            menuStrip.Items.AddRange(new[] { fileMenu, helpMenu });
            // Логируем, что пункты «Файл» и «Справка» добавлены
            _logger.LogDebug("MenuStrip Items: добавлены пункты 'Файл' и 'Справка'");
            fileMenu.DropDownItems.Add(new ToolStripMenuItem("Выход", null, (s, e) => this.Close()));
            _logger.LogDebug("MenuStrip: добавлен пункт 'Выход' в 'Файл'");
            helpMenu.DropDownItems.Add(new ToolStripMenuItem("О программе", null, OnAboutClick));
            _logger.LogDebug("MenuStrip: добавлен пункт 'О программе' в 'Справка'");

            // Позиционируем текстовое поле и кнопку ниже меню
            int offsetY = menuStrip.Bottom + 5;            // 5px от меню

            txtInstallPath.Location = new Point(1, offsetY + 3);
            this.Controls.Add(txtInstallPath);
            // Логируем создание и добавление текстового поля для пути установки
            _logger.LogDebug("TextBox txtInstallPath создан, Location={Location}", txtInstallPath.Location);

            btnBrowse.Location = new Point(125, offsetY);
            this.Controls.Add(btnBrowse);
            // Логируем создание и добавление кнопки «Выбрать папку»
            _logger.LogDebug("Button btnBrowse создан, Location={Location}", btnBrowse.Location);

            // Общие настройки грида
            dataGridViewPrograms.AutoGenerateColumns = false;  // колонки задаём вручную
            dataGridViewPrograms.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
            dataGridViewPrograms.MultiSelect = false;
            dataGridViewPrograms.CellDoubleClick += DataGridViewPrograms_CellDoubleClick;
            // Запрещаем пользователю добавлять новые строки вручную
            this.dataGridViewPrograms.AllowUserToAddRows = false;
            // Логируем старт настройки DataGridView (без колонок)
            _logger.LogDebug("DataGridView dataGridViewPrograms создан (пока без колонок)");
            // Разрешаем изменение состояния чекбоксов
            dataGridViewPrograms.CellValueChanged += DataGridViewPrograms_CellValueChanged;

            // ===== Designer‑Generated Columns =====
            // 1) Checkbox
            this.colSelect = new System.Windows.Forms.DataGridViewCheckBoxColumn();
            this.colSelect.Name = "colSelect";
            this.colSelect.HeaderText = "";
            this.colSelect.FalseValue = false;
            this.colSelect.TrueValue = true;
            this.colSelect.Width = 30;
            // Логируем создание колонки colSelect
            _logger.LogDebug("Колонка colSelect (DataGridViewCheckBoxColumn) создана");

            // 2) Icon
            this.colIcon = new System.Windows.Forms.DataGridViewImageColumn();
            this.colIcon.Name = "colIcon";
            this.colIcon.HeaderText = "";
            this.colIcon.DataPropertyName = "Icon";
            this.colIcon.ImageLayout = System.Windows.Forms.DataGridViewImageCellLayout.Zoom;
            this.colIcon.Width = 32;
            // Логируем создание колонки colIcon
            _logger.LogDebug("Колонка colIcon (DataGridViewImageColumn) создана");

            // 3) Name
            this.colName = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.colName.Name = "colName";
            this.colName.HeaderText = "Название";
            this.colName.DataPropertyName = "Name";
            this.colName.AutoSizeMode = System.Windows.Forms.DataGridViewAutoSizeColumnMode.Fill;
            // Логируем создание колонки colName
            _logger.LogDebug("Колонка colName (DataGridViewTextBoxColumn) создана");

            // 4) Description
            this.colDescription = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.colDescription.Name = "colDescription";
            this.colDescription.HeaderText = "Описание";
            this.colDescription.DataPropertyName = "Description";
            this.colDescription.AutoSizeMode = System.Windows.Forms.DataGridViewAutoSizeColumnMode.Fill;
            this.colDescription.DefaultCellStyle.WrapMode = System.Windows.Forms.DataGridViewTriState.True;
            // Логируем создание колонки colDescription
            _logger.LogDebug("Колонка colDescription (DataGridViewTextBoxColumn, wrap) создан");

            // 5) ParametersDisplay
            this.colParams = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.colParams.Name = "colParams";
            this.colParams.HeaderText = "Ключи";
            this.colParams.DataPropertyName = "ParametersDisplay";
            this.colParams.AutoSizeMode = System.Windows.Forms.DataGridViewAutoSizeColumnMode.Fill;
            // Логируем создание колонки colParams
            _logger.LogDebug("Колонка colParams (DataGridViewTextBoxColumn) создана");

            // Колонка размера
            this.colSize = new DataGridViewTextBoxColumn();
            this.colSize.Name = "colSize";
            this.colSize.HeaderText = "Размер (МБ)";
            this.colSize.DataPropertyName = "SizeMB";
            this.colSize.DefaultCellStyle.Format = "N2";
            this.colSize.Width = 80;

            // Колонка лицензии
            this.colLicense = new DataGridViewLinkColumn();
            this.colLicense.Name = "colLicense";
            this.colLicense.HeaderText = "Лицензия";
            this.colLicense.DataPropertyName = "LicenseUrl";
            this.colLicense.Width = 100;
            this.colLicense.ActiveLinkColor = Color.Blue;
            this.colLicense.LinkBehavior = LinkBehavior.SystemDefault;

            // 6) Добавляем колонки в нужном порядке
            this.dataGridViewPrograms.Columns.AddRange(new System.Windows.Forms.DataGridViewColumn[] {
            this.colSelect,
            this.colIcon,
            this.colName,
            this.colDescription,
            this.colParams,
            this.colSize,
            this.colLicense
        });
            // Логируем, что все колонки успешно добавлены
            _logger.LogDebug("Все колонки добавлены в DataGridView");

            // ===== End Designer‑Generated Columns =====

            dataGridViewPrograms.AutoSizeRowsMode = DataGridViewAutoSizeRowsMode.AllCellsExceptHeaders;  // динамическая высота
            // Основной фон ячеек
            dataGridViewPrograms.DefaultCellStyle.BackColor = Color.White;
            // Чередующиеся ряды
            dataGridViewPrograms.AlternatingRowsDefaultCellStyle.BackColor = Color.LightGray;
            // Цвет выделенной строки
            dataGridViewPrograms.DefaultCellStyle.SelectionBackColor = Color.DarkGray;
            // Границы и сетка
            dataGridViewPrograms.CellBorderStyle = DataGridViewCellBorderStyle.SingleHorizontal;
            dataGridViewPrograms.GridColor = Color.Silver;
            // Подписываемся на события форматирования и кликов
            dataGridViewPrograms.CellFormatting += DataGridViewPrograms_CellFormatting;
            dataGridViewPrograms.CellContentClick += DataGridViewPrograms_CellContentClick;
            // 
            // btnToggleSelection
            // 
            this.btnToggleSelection = new System.Windows.Forms.Button();
            this.btnToggleSelection.Name = "btnToggleSelection";
            this.btnToggleSelection.Size = new System.Drawing.Size(120, 27);
            this.btnToggleSelection.Location = new System.Drawing.Point(12, 431);  // теперь X=12, внутри формы для красивого отображения на уровне текста
            btnToggleSelection.FlatStyle = FlatStyle.Flat;
            btnToggleSelection.BackColor = Color.CornflowerBlue;
            btnToggleSelection.ForeColor = Color.White;
            this.btnToggleSelection.TabIndex = 0;
            this.btnToggleSelection.Text = "Выбрать все";
            this.btnToggleSelection.UseVisualStyleBackColor = true;
            this.btnToggleSelection.Anchor = AnchorStyles.Bottom | AnchorStyles.Left;
            this.btnToggleSelection.Click += new System.EventHandler(this.BtnToggleSelection_Click);
            this.Controls.Add(this.btnToggleSelection);
            // Логируем создание и добавление кнопки «Выбрать все»
            _logger.LogDebug("Button btnToggleSelection создан, Location={Location}", btnToggleSelection.Location);
            // 
            // btnInstall
            // 
            int installX = this.btnToggleSelection.Location.X + this.btnToggleSelection.Width + 5; // отступ 5px
            this.btnInstall.Location = new System.Drawing.Point(installX, 431);
            btnInstall.FlatStyle = FlatStyle.Flat;
            btnInstall.BackColor = Color.MediumSeaGreen;
            btnInstall.ForeColor = Color.White;
            btnInstall.Margin = new Padding(4, 3, 4, 3);
            btnInstall.Name = "btnInstall";
            btnInstall.Size = new Size(88, 27);
            btnInstall.TabIndex = 1;
            btnInstall.Text = "Установить";
            btnInstall.UseVisualStyleBackColor = true;
            btnInstall.Anchor = AnchorStyles.Bottom | AnchorStyles.Left;
            btnInstall.Click += BtnInstall_Click;
            // Логируем создание и добавление кнопки «Установить»
            _logger.LogDebug("Button btnInstall создан, Location={Location}", btnInstall.Location);
            // 
            // btnCancelInstall
            // 
            this.btnCancelInstall = new Button();
            this.btnCancelInstall.Text = "Отменить";
            this.btnCancelInstall.Enabled = false;
            this.btnCancelInstall.Anchor = AnchorStyles.Bottom | AnchorStyles.Left;
            btnCancelInstall.FlatStyle = FlatStyle.Flat;
            btnCancelInstall.BackColor = Color.IndianRed;
            btnCancelInstall.ForeColor = Color.White;
            this.btnCancelInstall.Size = new Size(88, 27);
            this.btnCancelInstall.Location = new Point(btnInstall.Location.X + btnInstall.Width + 5, btnInstall.Location.Y);
            this.btnCancelInstall.Click += BtnCancelInstall_Click;
            this.Controls.Add(this.btnCancelInstall);
            // Логируем создание и добавление кнопки «Отменить»
            _logger.LogDebug("Button btnCancelInstall создан, Location={Location}", btnCancelInstall.Location);
            // 
            // progressBar
            // 
            progressBar.Location = new Point(1, 505);
            progressBar.Margin = new Padding(4, 3, 4, 3);
            progressBar.Name = "progressBar";
            progressBar.Size = new Size(100, 27);
            progressBar.TabIndex = 2;
            progressBar.Anchor = AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
            // Логируем создание и добавление ProgressBar
            _logger.LogDebug("ProgressBar создан, Location={Location}, Size={Size}", progressBar.Location, progressBar.Size);
            // 
            // btnBrowse
            //
            btnBrowse.Margin = new Padding(4, 3, 4, 3);
            btnBrowse.FlatStyle = FlatStyle.Flat;
            btnBrowse.BackColor = Color.MediumSeaGreen;
            btnBrowse.ForeColor = Color.White;
            btnBrowse.Name = "btnBrowse";
            btnBrowse.Size = new Size(88, 27);
            btnBrowse.TabIndex = 3;
            btnBrowse.Text = "Выбрать папку";
            btnBrowse.UseVisualStyleBackColor = true;
            btnBrowse.Click += BtnBrowse_Click;
            // 
            // txtInstallPath
            //
            txtInstallPath.Margin = new Padding(4, 3, 4, 3);
            txtInstallPath.Name = "txtInstallPath";
            txtInstallPath.Size = new Size(116, 23);
            txtInstallPath.TabIndex = 4;
            // 
            // Label для отображения суммы размеров файлов
            // 
            this.lblTotalSize = new System.Windows.Forms.Label();
            this.lblTotalSize.AutoSize = true;
            this.lblTotalSize.Location = new System.Drawing.Point(12, 465); // Позиция над progressBar
            this.lblTotalSize.Name = "lblTotalSize";
            this.lblTotalSize.Size = new System.Drawing.Size(200, 15);
            this.lblTotalSize.TabIndex = 9;
            this.lblTotalSize.Text = "Общий размер: 0.00 МБ";
            this.Controls.Add(this.lblTotalSize);
            // 
            // lblStatus
            // 
            lblStatus.AutoSize = true;
            lblStatus.Location = new Point(12, 485);
            lblStatus.Name = "lblStatus";
            lblStatus.Size = new Size(38, 15);
            lblStatus.TabIndex = 5;
            lblStatus.Text = "Выберите программы в таблице выше и нажмите кнопку Установить";
            lblStatus.Anchor = AnchorStyles.Bottom | AnchorStyles.Left;
            // Логируем создание и добавление Label lblStatus
            _logger.LogDebug("Label lblStatus создан, Text=\"{Text}\"", lblStatus.Text);
            // 
            // statusTimer
            // 
            this.statusTimer = new System.Windows.Forms.Timer();
            this.statusTimer.Interval = 500; // обновлять каждые 500 мс (полсекунды)
            this.statusTimer.Tick += new System.EventHandler(this.StatusTimer_Tick);
            // Логируем создание Timer statusTimer
            _logger.LogDebug("Таймер statusTimer создан с Interval={Interval}", statusTimer.Interval);
            // ────────────────────────────────────────────────────────────────
            // Перенесено из верха для нормальной инициализации остальных элементов
            // Вычисляем Y-координату верхнего края грида: чуть ниже txtInstallPath
            int gridY = txtInstallPath.Bottom + 5;
            // Вычисляем Y-координату нижнего края грида: чуть выше btnToggleSelection
            int bottomY = btnToggleSelection.Location.Y - 5;

            dataGridViewPrograms.Location = new Point(1, gridY);
            dataGridViewPrograms.Size = new Size(1068, bottomY - gridY);
            dataGridViewPrograms.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
            this.Controls.Add(this.dataGridViewPrograms);// Логируем позиционирование и размер DataGridView
            _logger.LogDebug("DataGridView location set to {Location}, size={Size}",
            dataGridViewPrograms.Location, dataGridViewPrograms.Size);
            // ────────────────────────────────────────────────────────────────
            // 
            // lblDonate
            // 
            this.lblDonate = new System.Windows.Forms.Label();
            this.lblDonate.AutoSize = true;
            this.lblDonate.Location = new System.Drawing.Point(12, 540);
            this.lblDonate.Name = "lblDonate";
            this.lblDonate.Size = new System.Drawing.Size(225, 15);
            this.lblDonate.TabIndex = 6;
            this.lblDonate.Text = "Поддержать проект (BTC и ETH (ERC20) адреса):";
            lblDonate.Anchor = AnchorStyles.Bottom | AnchorStyles.Left;
            this.Controls.Add(this.lblDonate);
            // Логируем создание и добавление Label lblDonate
            _logger.LogDebug("Label lblDonate создан, Text=\"{Text}\"", lblDonate.Text);
            // 
            // txtBTC
            // 
            this.txtBTC = new System.Windows.Forms.TextBox();
            this.txtBTC.Location = new System.Drawing.Point(12, 560);
            this.txtBTC.Name = "txtBTC";
            this.txtBTC.ReadOnly = true;
            this.txtBTC.Size = new System.Drawing.Size(400, 23);
            this.txtBTC.TabIndex = 7;
            this.txtBTC.Text = "bc1qfamn5mcee7egfl8pu7w85ax7yvj5n9hxz8vxh4";
            this.txtBTC.BackColor = Color.White;
            this.txtBTC.BorderStyle = BorderStyle.FixedSingle;
            this.txtBTC.Anchor = AnchorStyles.Bottom | AnchorStyles.Left;
            this.Controls.Add(this.txtBTC);
            // Логируем создание TextBox txtBTC
            _logger.LogDebug("TextBox txtBTC создан, Text=\"{Text}\"", txtBTC.Text);
            // 
            // txtETH
            // 
            this.txtETH = new System.Windows.Forms.TextBox();
            this.txtETH.Location = new System.Drawing.Point(12, 590);
            this.txtETH.Name = "txtETH";
            this.txtETH.ReadOnly = true;
            this.txtETH.Size = new System.Drawing.Size(400, 23);
            this.txtETH.TabIndex = 8;
            this.txtETH.Text = "0xbC7fE973BFA32Ca0D4d4900ee94214E61F23271E";
            this.txtETH.BackColor = Color.White;
            this.txtETH.BorderStyle = BorderStyle.FixedSingle;
            this.txtETH.Anchor = AnchorStyles.Bottom | AnchorStyles.Left;
            this.Controls.Add(this.txtETH);
            // Логируем создание TextBox txtETH
            _logger.LogDebug("TextBox txtETH создан, Text=\"{Text}\"", txtETH.Text);
            // 
            // Form1
            // 
            AutoScaleDimensions = new SizeF(7F, 15F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(1100, 660);
            // Коррекция ширины ProgressBar после установки ClientSize
            progressBar.Width = (this.ClientSize.Width - 3); // Растягиваем на всю ширину минус 3 для красоты.
            Controls.Add(lblStatus);
            Controls.Add(progressBar);
            Controls.Add(btnInstall);
            Margin = new Padding(4, 3, 4, 3);
            Name = "Form1";
            Text = "Установщик WindSoft";
            Load += Form1_Load;
            _logger.LogInformation("InitializeComponent: завершено, все элементы добавлены на форму");
            ResumeLayout(false);
            PerformLayout();

        }

        #endregion
        private DataGridView dataGridViewPrograms;
        private DataGridViewCheckBoxColumn colSelect;
        private DataGridViewImageColumn colIcon;
        private DataGridViewTextBoxColumn colName;
        private DataGridViewTextBoxColumn colDescription;
        private DataGridViewTextBoxColumn colParams;
        private Button btnInstall;
        private ProgressBar progressBar;
        private FolderBrowserDialog folderBrowserDialog;
        private Button btnBrowse;
        private Button btnToggleSelection;
        private Button btnCancelInstall;
        private TextBox txtInstallPath;
        private Label lblStatus;
        private Timer statusTimer;
        private Label lblDonate;
        private TextBox txtBTC;
        private TextBox txtETH;
        private DataGridViewTextBoxColumn colSize;
        private DataGridViewLinkColumn colLicense;
        private Label lblTotalSize;
    }
}