using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using Microsoft.Data.Sqlite;
using MiniMuhasebePro.Core;

namespace MiniMuhasebePro.Data
{
    public class DatabaseInitializer
    {
        public void Initialize()
        {
            var provider = AppSettings.DatabaseProvider;
            if (provider.Equals("SQLServer", StringComparison.OrdinalIgnoreCase))
            {
                InitializeSqlServer();
                return;
            }

            InitializeSqlite();
        }

        private void InitializeSqlite()
        {
            using (var connection = new SqliteConnection(AppSettings.SqliteConnectionString))
            {
                connection.Open();
                foreach (var sql in BuildSchemaSql(provider: "SQLite"))
                {
                    using (var command = connection.CreateCommand())
                    {
                        command.CommandText = sql;
                        command.ExecuteNonQuery();
                    }
                }
            }
        }

        private void InitializeSqlServer()
        {
            using (var connection = new SqlConnection(AppSettings.SqlServerConnectionString))
            {
                connection.Open();
                foreach (var sql in BuildSchemaSql(provider: "SQLServer"))
                {
                    using (var command = new SqlCommand(sql, connection))
                    {
                        command.ExecuteNonQuery();
                    }
                }
            }
        }

        private IEnumerable<string> BuildSchemaSql(string provider)
        {
            var textType = provider == "SQLServer" ? "NVARCHAR(500)" : "TEXT";
            var idType = provider == "SQLServer" ? "INT IDENTITY(1,1) PRIMARY KEY" : "INTEGER PRIMARY KEY AUTOINCREMENT";

            return new List<string>
            {
                $"CREATE TABLE IF NOT EXISTS Users (id {idType}, username {textType} NOT NULL, password_hash {textType} NOT NULL, role {textType}, status {textType});",
                $"CREATE TABLE IF NOT EXISTS Companies (id {idType}, name {textType}, tax_no {textType}, base_currency {textType});",
                $"CREATE TABLE IF NOT EXISTS BankConnections (id {idType}, company_id INT, bank_name {textType}, api_config_encrypted {textType}, active INT);",
                $"CREATE TABLE IF NOT EXISTS BankAccounts (id {idType}, company_id INT, iban {textType}, currency {textType}, account_type {textType});",
                $"CREATE TABLE IF NOT EXISTS BankTransactions (id {idType}, account_id INT, trx_date {textType}, amount DECIMAL(18,2), currency {textType}, description {textType}, ref_no {textType}, unique_hash {textType});",
                $"CREATE TABLE IF NOT EXISTS TransferOrders (id {idType}, company_id INT, type {textType}, amount DECIMAL(18,2), currency {textType}, status {textType}, bank_ref_no {textType});",
                $"CREATE TABLE IF NOT EXISTS ExchangeRates (id {idType}, bank_name {textType}, currency_pair {textType}, rate DECIMAL(18,8), rate_time {textType});",
                $"CREATE TABLE IF NOT EXISTS AccountingVouchers (id {idType}, company_id INT, voucher_no {textType}, voucher_date {textType}, status {textType});",
                $"CREATE TABLE IF NOT EXISTS VoucherLines (id {idType}, voucher_id INT, account_code {textType}, debit DECIMAL(18,2), credit DECIMAL(18,2), currency {textType}, fx_rate DECIMAL(18,8));",
                $"CREATE TABLE IF NOT EXISTS AuditLogs (id {idType}, user_id INT, action {textType}, entity {textType}, entity_id INT, timestamp {textType}, detail_masked {textType});"
            };
        }
    }
}
