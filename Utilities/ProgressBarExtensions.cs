using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace WindSoftInstaller.Utilities;

public static class ProgressBarExtensions
{
    [DllImport("user32.dll", CharSet = CharSet.Auto)]
    static extern IntPtr SendMessage(IntPtr hWnd, uint Msg, IntPtr w, IntPtr l);

    public static void SetState(this ProgressBar pBar, int state)
    {
        // PBM_SETSTATE = 0x0400 + 16
        SendMessage(pBar.Handle, 0x0410, (IntPtr)state, IntPtr.Zero);
    }
}
