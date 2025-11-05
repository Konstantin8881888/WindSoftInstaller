using System.Drawing;
using System.Windows.Forms;
using WindSoftInstaller.Services;

namespace WindSoftInstaller.Utilities
{
    public class MenuRenderer : ToolStripProfessionalRenderer
    {
        private readonly Theme _theme;

        public MenuRenderer(Theme theme) : base(new MenuColorTable(theme))
        {
            _theme = theme ?? ThemeManager.DefaultTheme;
        }

        protected override void OnRenderMenuItemBackground(ToolStripItemRenderEventArgs e)
        {
            try
            {
                if (e.Item.Selected || e.Item.Pressed)
                {
                    using (var brush = new SolidBrush(_theme.GridSelectionBackColor))
                    {
                        e.Graphics.FillRectangle(brush, new Rectangle(Point.Empty, e.Item.Size));
                    }
                }
                else
                {
                    base.OnRenderMenuItemBackground(e);
                }
            }
            catch
            {
                base.OnRenderMenuItemBackground(e);
            }
        }

        protected override void OnRenderItemText(ToolStripItemTextRenderEventArgs e)
        {
            try
            {
                if (e.Item.Selected || e.Item.Pressed)
                {
                    e.TextColor = _theme.GridForeColor;
                }
                else
                {
                    e.TextColor = _theme.MenuForeColor;
                }
            }
            catch
            {
                // Оставляем стандартный цвет текста в случае ошибки
            }

            base.OnRenderItemText(e);
        }
    }

    public class MenuColorTable : ProfessionalColorTable
    {
        private readonly Theme _theme;

        public MenuColorTable(Theme theme)
        {
            _theme = theme ?? ThemeManager.DefaultTheme;
        }

        public override Color MenuStripGradientBegin => _theme.MenuBackColor;
        public override Color MenuStripGradientEnd => _theme.MenuBackColor;

        public override Color ToolStripDropDownBackground => _theme.MenuDropDownBackColor;

        public override Color MenuBorder => _theme.GridLineColor;

        public override Color MenuItemBorder => _theme.GridLineColor;

        public override Color MenuItemSelected => _theme.GridSelectionBackColor;

        public override Color MenuItemSelectedGradientBegin => _theme.GridSelectionBackColor;
        public override Color MenuItemSelectedGradientEnd => _theme.GridSelectionBackColor;

        public override Color ImageMarginGradientBegin => _theme.MenuDropDownBackColor;
        public override Color ImageMarginGradientMiddle => _theme.MenuDropDownBackColor;
        public override Color ImageMarginGradientEnd => _theme.MenuDropDownBackColor;
    }
}