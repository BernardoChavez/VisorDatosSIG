#nullable enable
using System;
using System.Windows.Forms;
using VisorDatosSIG.Migrador.Forms;

namespace VisorDatosSIG.Migrador;

internal static class Program
{
    [STAThread]
    static void Main()
    {
        try { System.Text.Encoding.RegisterProvider(System.Text.CodePagesEncodingProvider.Instance); } catch { }
        System.Windows.Forms.Application.EnableVisualStyles();
        System.Windows.Forms.Application.SetCompatibleTextRenderingDefault(false);
        System.Windows.Forms.Application.Run(new MainMigradorForm());
    }
}
