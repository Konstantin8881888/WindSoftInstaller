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
            // Label для отображения суммы размеров файлов
            this.lblTotalSize = new Label();
            this.lblTotalSize.AutoSize = true;
            this.lblTotalSize.Name = "lblTotalSize";
            // Пока просто добавим — позиционировать будем ниже
            this.lblTotalSize.Anchor = AnchorStyles.Top | AnchorStyles.Left;
            this.Controls.Add(this.lblTotalSize);

            SuspendLayout();
            // ─── Блок: MenuStrip + позиционирование ─────────────────────
            // Создаем и докируем MenuStrip
            _logger.LogInformation("InitializeComponent: начало создания UI-элементов");
            this.menuStrip = new MenuStrip();
            menuStrip.Dock = DockStyle.Top;
            this.MainMenuStrip = menuStrip;
            this.Controls.Add(menuStrip);
            // Логируем создание и добавление MenuStrip
            _logger.LogDebug("MenuStrip создан и добавлен на форму");
            // Настраиваем пункты меню
            var fileMenu = new ToolStripMenuItem { Name = "menu.File", Text = "Файл" };
            var helpMenu = new ToolStripMenuItem { Name = "menu.Help", Text = "Справка" };
            menuStrip.Items.AddRange(new[] { fileMenu, helpMenu });
            // Логируем, что пункты «Файл» и «Справка» добавлены
            _logger.LogDebug("MenuStrip Items: добавлены пункты 'Файл' и 'Справка'");
            // 1) Создаем сам элемент Выход
            var exitItem = new ToolStripMenuItem
            {
                Name = "menu.File.Exit",
                Text = "Выход"
            };
            // 2) Подписываемся на событие
            exitItem.Click += (s, e) => this.Close();
            // 3) Добавляем в меню
            fileMenu.DropDownItems.Add(exitItem);
            _logger.LogDebug("MenuStrip: добавлен пункт 'Выход' в 'Файл'");

            // 1) Создаем сам элемент О программе
            var aboutItem = new ToolStripMenuItem
            {
                Name = "menu.Help.About",
                Text = "О программе"
            };
            // 2) Подписываемся на событие
            aboutItem.Click += OnAboutClick;
            // 3) Добавляем в меню
            helpMenu.DropDownItems.Add(aboutItem);

            _logger.LogDebug("MenuStrip: добавлен пункт 'О программе' в 'Справка'");
            // ─── Блок: меню "Язык" ────────────────────────────────────────
            // 1) создаём пункт "Язык" и два подпункта
            var langMenu = new ToolStripMenuItem("Язык") { Name = "menu.Language" };
            var langRu = new ToolStripMenuItem("Русский") { Name = "menu.Language.Ru" };
            var langEn = new ToolStripMenuItem("English") { Name = "menu.Language.En" };

            // 2) подписываем обработчики
            langRu.Click += (s, e) => SwitchLanguage("ru");
            langEn.Click += (s, e) => SwitchLanguage("en");

            // 3) собираем и вешаем на menuStrip
            langMenu.DropDownItems.AddRange(new[] { langRu, langEn });
            menuStrip.Items.Add(langMenu);
            _logger.LogDebug("MenuStrip: добавлен пункт Язык");
            // ───────────────────────────────────────────────────────────────
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
            // Checkbox
            this.colSelect = new System.Windows.Forms.DataGridViewCheckBoxColumn();
            this.colSelect.Name = "colSelect";
            this.colSelect.HeaderText = "";
            this.colSelect.FalseValue = false;
            this.colSelect.TrueValue = true;
            this.colSelect.Width = 30;
            // Логируем создание колонки colSelect
            _logger.LogDebug("Колонка colSelect (DataGridViewCheckBoxColumn) создана");
            // Icon
            this.colIcon = new System.Windows.Forms.DataGridViewImageColumn();
            this.colIcon.Name = "colIcon";
            this.colIcon.HeaderText = "";
            this.colIcon.DataPropertyName = "Icon";
            this.colIcon.ImageLayout = System.Windows.Forms.DataGridViewImageCellLayout.Zoom;
            this.colIcon.Width = 32;
            // Логируем создание колонки colIcon
            _logger.LogDebug("Колонка colIcon (DataGridViewImageColumn) создана");
            // Name
            this.colName = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.colName.Name = "colName";
            this.colName.HeaderText = "Название";
            this.colName.DataPropertyName = "Name";
            this.colName.AutoSizeMode = System.Windows.Forms.DataGridViewAutoSizeColumnMode.Fill;
            // Логируем создание колонки colName
            _logger.LogDebug("Колонка colName (DataGridViewTextBoxColumn) создана");
            // Description
            this.colDescription = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.colDescription.Name = "colDescription";
            this.colDescription.HeaderText = "Описание";
            this.colDescription.DataPropertyName = "Description";
            this.colDescription.AutoSizeMode = System.Windows.Forms.DataGridViewAutoSizeColumnMode.Fill;
            this.colDescription.DefaultCellStyle.WrapMode = System.Windows.Forms.DataGridViewTriState.True;
            // Логируем создание колонки colDescription
            _logger.LogDebug("Колонка colDescription (DataGridViewTextBoxColumn, wrap) создан");
            // ParametersDisplay
            this.colParams = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.colParams.Name = "colParams";
            this.colParams.HeaderText = "Ключи";
            this.colParams.DataPropertyName = "ParametersDisplay";
            this.colParams.DefaultCellStyle.WrapMode = System.Windows.Forms.DataGridViewTriState.True;
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
            // Добавляем колонки в нужном порядке
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
            // Добавляем грид на форму
            this.Controls.Add(this.dataGridViewPrograms);
            // btnToggleSelection
            this.btnToggleSelection = new System.Windows.Forms.Button();
            this.btnToggleSelection.Name = "btnToggleSelection";
            btnToggleSelection.FlatStyle = FlatStyle.Flat;
            btnToggleSelection.BackColor = Color.CornflowerBlue;
            btnToggleSelection.ForeColor = Color.White;
            this.btnToggleSelection.TabIndex = 0;
            this.btnToggleSelection.Text = "Выбрать все";
            this.btnToggleSelection.UseVisualStyleBackColor = true;
            this.btnToggleSelection.Click += new System.EventHandler(this.BtnToggleSelection_Click);
            this.Controls.Add(this.btnToggleSelection);
            // Логируем создание и добавление кнопки «Выбрать все»
            _logger.LogDebug("Button btnToggleSelection создан, Location={Location}", btnToggleSelection.Location);
            // btnInstall
            int installX = this.btnToggleSelection.Location.X + this.btnToggleSelection.Width + 5; // отступ 5px
            btnInstall.FlatStyle = FlatStyle.Flat;
            btnInstall.BackColor = Color.MediumSeaGreen;
            btnInstall.ForeColor = Color.White;
            btnInstall.Margin = new Padding(4, 3, 4, 3);
            btnInstall.Name = "btnInstall";
            btnInstall.TabIndex = 1;
            btnInstall.Text = "Установить";
            btnInstall.UseVisualStyleBackColor = true;
            btnInstall.Click += BtnInstall_Click;
            // Логируем создание и добавление кнопки «Установить»
            _logger.LogDebug("Button btnInstall создан, Location={Location}", btnInstall.Location);
            // btnCancelInstall
            this.btnCancelInstall = new Button();
            this.btnCancelInstall.Text = "Отменить";
            this.btnCancelInstall.Enabled = false;
            btnCancelInstall.FlatStyle = FlatStyle.Flat;
            btnCancelInstall.BackColor = Color.IndianRed;
            btnCancelInstall.ForeColor = Color.White;
            this.btnCancelInstall.Click += BtnCancelInstall_Click;
            this.Controls.Add(this.btnCancelInstall);
            // Логируем создание и добавление кнопки «Отменить»
            _logger.LogDebug("Button btnCancelInstall создан, Location={Location}", btnCancelInstall.Location);
            // progressBar
            progressBar.Margin = new Padding(4, 3, 4, 3);
            progressBar.Name = "progressBar";
            progressBar.TabIndex = 2;
            // Логируем создание и добавление ProgressBar
            _logger.LogDebug("ProgressBar создан, Location={Location}, Size={Size}", progressBar.Location, progressBar.Size);
            // btnBrowse
            btnBrowse.Margin = new Padding(4, 3, 4, 3);
            btnBrowse.FlatStyle = FlatStyle.Flat;
            btnBrowse.BackColor = Color.MediumSeaGreen;
            btnBrowse.ForeColor = Color.White;
            btnBrowse.Name = "btnBrowse";
            btnBrowse.Size = new Size(88, 27);
            btnBrowse.TabIndex = 3;
            btnBrowse.UseVisualStyleBackColor = true;
            btnBrowse.Click += BtnBrowse_Click;
            // txtInstallPath
            txtInstallPath.Margin = new Padding(4, 3, 4, 3);
            txtInstallPath.Name = "txtInstallPath";
            txtInstallPath.Size = new Size(116, 23);
            txtInstallPath.TabIndex = 4;
            // lblStatus
            lblStatus.AutoSize = true;
            lblStatus.Name = "lblStatus";
            lblStatus.TabIndex = 5;
            // Логируем создание и добавление Label lblStatus
            _logger.LogDebug("Label lblStatus создан, Text=\"{Text}\"", lblStatus.Text);
            // statusTimer
            this.statusTimer = new System.Windows.Forms.Timer();
            this.statusTimer.Interval = 500; // обновлять каждые 500 мс (полсекунды)
            this.statusTimer.Tick += new System.EventHandler(this.StatusTimer_Tick);
            // Логируем создание Timer statusTimer
            _logger.LogDebug("Таймер statusTimer создан с Interval={Interval}", statusTimer.Interval);

            // lblDonate
            this.lblDonate = new System.Windows.Forms.Label();
            this.lblDonate.AutoSize = true;
            this.lblDonate.Name = "lblDonate";
            this.lblDonate.TabIndex = 6;
            this.Controls.Add(this.lblDonate);
            // Логируем создание и добавление Label lblDonate
            _logger.LogDebug("Label lblDonate создан, Text=\"{Text}\"", lblDonate.Text);
            // txtBTC
            this.txtBTC = new System.Windows.Forms.TextBox();
            this.txtBTC.Name = "txtBTC";
            this.txtBTC.ReadOnly = true;
            this.txtBTC.TabIndex = 7;
            this.txtBTC.Text = "bc1qfamn5mcee7egfl8pu7w85ax7yvj5n9hxz8vxh4";
            this.txtBTC.BackColor = Color.White;
            this.txtBTC.BorderStyle = BorderStyle.FixedSingle;
            this.Controls.Add(this.txtBTC);
            // Логируем создание TextBox txtBTC
            _logger.LogDebug("TextBox txtBTC создан, Text=\"{Text}\"", txtBTC.Text);
            // txtETH
            this.txtETH = new System.Windows.Forms.TextBox();
            this.txtETH.Name = "txtETH";
            this.txtETH.ReadOnly = true;
            this.txtETH.TabIndex = 8;
            this.txtETH.Text = "0xbC7fE973BFA32Ca0D4d4900ee94214E61F23271E";
            this.txtETH.BackColor = Color.White;
            this.txtETH.BorderStyle = BorderStyle.FixedSingle;
            this.Controls.Add(this.txtETH);
            // Логируем создание TextBox txtETH
            _logger.LogDebug("TextBox txtETH создан, Text=\"{Text}\"", txtETH.Text);
            // Label для Telegram-канала
            this.lblTelegram = new Label();
            this.lblTelegram.AutoSize = true;
            this.lblTelegram.Name = "lblTelegram";
            this.lblTelegram.Text = Localization.T("lblTelegram"); // ключ в словаре
            this.Controls.Add(this.lblTelegram);
            _logger.LogDebug("Label lblTelegram создан");

            // TextBox для ссылки на Telegram
            this.txtTelegram = new TextBox();
            this.txtTelegram.Name = "txtTelegram";
            this.txtTelegram.ReadOnly = true;
            this.txtTelegram.BorderStyle = BorderStyle.FixedSingle;
            this.txtTelegram.Text = Localization.T("txtTelegram.Url"); // ключ в словаре
            this.txtTelegram.AutoSize = false; // чтобы можно было задать ширину
            this.txtTelegram.Height = txtETH.Height;
            this.Controls.Add(this.txtTelegram);
            _logger.LogDebug("TextBox txtTelegram создан");
            // bannerPictureBox Рекламный блок (изображение)
            this.bannerPictureBox = new PictureBox();
            this.bannerPictureBox.SizeMode = PictureBoxSizeMode.Zoom; // Растягиваем изображение
            // Выбираем баннер по текущему языку
            this.bannerPictureBox.Image = Localization.Current == "ru"
                ? Properties.Resources.banner
                : Properties.Resources.banneren;
            this.bannerPictureBox.WaitOnLoad = false;
            if (this.bannerPictureBox.Image == null)
                _logger.LogError("Banner image is null! Проверьте, что ресурс banner добавлен в Resources.resx");
            this.bannerPictureBox.Anchor = AnchorStyles.Left | AnchorStyles.Right; // Анкорим по горизонтали
            this.bannerPictureBox.Click += new EventHandler(BannerPictureBox_Click);
            this.Controls.Add(this.bannerPictureBox);
            this.bannerPictureBox.Visible = true;
            // Логируем создание bannerPictureBox
            _logger.LogDebug("bannerPictureBox создан");
            // Form1
            AutoScaleDimensions = new SizeF(7F, 15F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(1100, 660);

            Margin = new Padding(4, 3, 4, 3);
            Name = "Form1";
            Text = "Установщик WindSoft";
            // сразу же сделаем первичный расчёт
            Load += (s, e) => this.OnResize(EventArgs.Empty);
            Load += Form1_Load;
            _logger.LogInformation("InitializeComponent: завершено, все элементы добавлены на форму");
            // === Централизованный расчёт позиций и размеров ===
            this.Resize += (sender, e) =>
            {
                // 1) Основные координаты и размеры
                int left = txtInstallPath.Left;
                int top = txtInstallPath.Bottom + 5;
                int width = this.ClientSize.Width - left - 1;

                // Вычисляем общую ширину кнопок по самому длинному тексту
                int padding = 20;  // по 10px с каждой стороны
                int w1 = TextRenderer.MeasureText(btnToggleSelection.Text, btnToggleSelection.Font).Width;
                int w2 = TextRenderer.MeasureText(btnInstall.Text, btnInstall.Font).Width;
                int w3 = TextRenderer.MeasureText(btnCancelInstall.Text, btnCancelInstall.Font).Width;
                int commonW = Math.Max(w1, Math.Max(w2, w3)) + padding;

                // Задаём всем трём кнопкам одинаковый размер
                btnToggleSelection.Size = new Size(commonW, btnToggleSelection.Height);
                btnInstall.Size = new Size(commonW, btnInstall.Height);
                btnCancelInstall.Size = new Size(commonW, btnCancelInstall.Height);

                // Отступы для баннера
                int bannerMarginTop = 10;  // от txtETH до баннера
                int bannerMarginBottom = 10;  // от баннера до низа формы

                // 2) Размер баннера (нужно объявить до footerHeight)
                int bannerWidth = this.ClientSize.Width - 40;          // 20px отступы слева и справа
                float aspect = 45f / 728f;
                int bannerHeight = (int)(bannerWidth * aspect);            // высота = % от ширины

                // 3) Вычисляем высоту «футера» без баннера
                int footerHeight =
                      btnToggleSelection.Height + 5   // кнопки
                    + lblStatus.Height + 5           // статус
                    + progressBar.Height + 5         // прогресс-бар
                    + lblTotalSize.Height + 5        // метка «Общий размер»
                    + lblDonate.Height + 5           // надпись «Поддержать…»
                    + txtBTC.Height + 5               // поле BTC
                    + txtETH.Height + 5               // поле ETH
                    + lblTelegram.Height + 5          // надпись Telegram
                    + txtTelegram.Height + bannerMarginTop; // поле Telegram + отступ сверху

                // 4) Считаем размер и позицию DataGridView так, чтобы под ним осталось место под footer + баннер
                int gridHeight = this.ClientSize.Height
                               - footerHeight      // место под все footer-элементы
                               - bannerHeight      // место под баннер
                               - bannerMarginBottom // отступ под баннером
                               - top;               // пространство сверху
                dataGridViewPrograms.Location = new Point(left, top);
                dataGridViewPrograms.Size = new Size(width, gridHeight);

                // 5) Позиционируем footer-элементы (кнопки, статусы, BTC/ETH, Telegram) прямо под DataGridView
                int y = dataGridViewPrograms.Bottom + 5;
                btnToggleSelection.Location = new Point(left, y);
                btnInstall.Location = new Point(left + commonW + 5, y);
                btnCancelInstall.Location = new Point(left + 2 * (commonW + 5), y);

                y = btnToggleSelection.Bottom + 5;
                lblStatus.Location = new Point(left, y);

                y = lblStatus.Bottom + 5;
                progressBar.Location = new Point(left, y);
                progressBar.Width = width;

                y = progressBar.Bottom + 5;
                lblTotalSize.Location = new Point(left, y);

                y = lblTotalSize.Bottom + 5;
                lblDonate.Location = new Point(left, y);

                y = lblDonate.Bottom + 5;
                txtBTC.Location = new Point(left, y);
                txtBTC.Size = new Size(width, txtBTC.Height);

                y = txtBTC.Bottom + 5;
                txtETH.Location = new Point(left, y);
                txtETH.Size = new Size(width, txtETH.Height);

                // Telegram-блок
                y = txtETH.Bottom + 5;
                lblTelegram.Location = new Point(left, y);
                y = lblTelegram.Bottom + 5;
                txtTelegram.Location = new Point(left, y);
                txtTelegram.Width = width;

                // 6) Позиционируем баннер в самом низу окна
                int yBanner = this.ClientSize.Height - bannerHeight - bannerMarginBottom;
                bannerPictureBox.Size = new Size(bannerWidth, bannerHeight);
                bannerPictureBox.Location = new Point(20, yBanner);
                bannerPictureBox.BringToFront();
            };

            Controls.Add(lblStatus);
            Controls.Add(progressBar);
            Controls.Add(btnInstall);

            ResumeLayout(false);
            PerformLayout();
        }
        private void BannerPictureBox_Click(object sender, EventArgs e)
        {
            var url = "https://cg91646-django-jb1bh.tw1.ru/";
            try
            {
                var psi = new System.Diagnostics.ProcessStartInfo
                {
                    FileName = url,
                    UseShellExecute = true
                };
                System.Diagnostics.Process.Start(psi);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Не удалось открыть URL {Url}", url);
                MessageBox.Show($"{Localization.T("failedOpenLink")}: {ex.Message}", Localization.T("errorTitle"), MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
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
        private PictureBox bannerPictureBox;
        private Label lblTelegram;
        private TextBox txtTelegram;
    }
}