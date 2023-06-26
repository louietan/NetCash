namespace NetCash.Tests;

using System;

public static class DbHelper
{
    public static DatabaseConfig GetConfig(string scheme) =>
            scheme switch
            {
                string s when s == GnuCashUri.UriSchemeMySQL => Config.MySQL,
                string s when s == GnuCashUri.UriSchemePostgreSQL => Config.PostgreSQL,
                _ => throw new Exception($"Unsupported database scheme: {scheme}")
            };

    public static void EnsureDatabase(string scheme, string dbName)
    {
        if (Bindings.gnc_uri_is_file_scheme(scheme))
            return;

        var config = GetConfig(scheme);

        var connString = $"Server={config.Host},{config.Port};User ID={config.UserName};Password={config.Password}";

        if (scheme == GnuCashUri.UriSchemeMySQL)
        {
            using var conn = new MySqlConnector.MySqlConnection(connString);
            conn.Open();
            using var cmd = conn.CreateCommand();
            cmd.CommandText = @$"DROP DATABASE IF EXISTS `{dbName}`; CREATE DATABASE `{dbName}`;";
            cmd.ExecuteNonQuery();
        }
        else if (scheme == GnuCashUri.UriSchemePostgreSQL)
        {
            using var conn = new Npgsql.NpgsqlConnection(connString);
            conn.Open();
            using var cmd = conn.CreateCommand();
            cmd.CommandText = @$"DROP DATABASE IF EXISTS ""{dbName}""; CREATE DATABASE ""{dbName}"";";
            cmd.ExecuteNonQuery();
        }
    }
}
