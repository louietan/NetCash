namespace NetCash.Tests;

using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;


internal class Bootstrap
{
    [ModuleInitializer]
    internal static void InitializeEngine()
    {
        Logging.Config(Logging.LogLevel.DEBUG, Logging.Appender.Console, Logging.Appender.OfFile("netcash.log"));

        GnuCashEngine.Initialize();

        foreach (var mod in new[] { "", "qof", "gnc" })
            GnuCashEngine.SetLogLevel(mod, Bindings.QofLogLevel.QOF_LOG_DEBUG);

        GnuCashEngine.SetLogOutput(LogOutputType.NewFile("libgnc.log"));

        Bindings.gnc_prefs_set_file_save_compressed(false);
        Bindings.gnc_prefs_set_file_retention_policy(Bindings.XMLFileRetentionType.XML_RETAIN_NONE);
    }

    /// <summary>
    /// Replicate premade books to different schemes to be tested.
    /// </summary>
    [ModuleInitializer]
    internal static void ReplicatePremadeBooks()
    {
        var premadeScheme = GnuCashUri.SchemeXml;
        var schemesToReplicate = Config.SupportedBackends.Where(b => b != premadeScheme);

        foreach (var file in Directory.GetFiles(TestingBook.RootPath))
        {
            var premadeUri = new GnuCashUri(GnuCashUri.SchemeXml, null, 0, null, null, file);
            foreach (var scheme in schemesToReplicate)
            {
                var bookName = PathExtensions.GetBaseFileName(file);
                var uri = TestingBook.MakeUri(scheme, bookName);
                DbHelper.EnsureDatabase(scheme, uri.Path);
                using var _ = Book.OpenRead(premadeUri).SaveAs(uri);
            }
        }
    }
}
