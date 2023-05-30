namespace NetCash.Tests;

using System;
using System.Diagnostics;
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
}
