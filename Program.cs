using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Data.SQLite;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace MiniMuhasebe
{
    internal static class Program
    {
        [STAThread]
        private static void Main()
        {
            try
            {
                Application.EnableVisualStyles();
                Application.SetCompatibleTextRenderingDefault(false);

                DatabaseHelper.InitializeDatabase();

                var context = AppContextFactory.Create();
                Application.Run(new MainForm(context));
            }
            catch (Exception ex)
            {
                MessageBox.Show("Uygulama başlatılamadı: " + ex.Message, "Hata", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
    }

    #region Entities

    public class Company
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string TaxNumber { get; set; }
    }

    public class Period
    {
        public int Id { get; set; }
        public int CompanyId { get; set; }
        public int Year { get; set; }
        public int Month { get; set; }
        public bool IsClosed { get; set; }
    }

    public class Account
    {
        public int Id { get; set; }
        public int CompanyId { get; set; }
        public string Code { get; set; }
        public string Name { get; set; }
        public string Type { get; set; }
        public int? ParentAccountId { get; set; }
    }

    public class VoucherType
    {
        public int Id { get; set; }
        public string Name { get; set; }
    }

    public class Voucher
    {
        public int Id { get; set; }
        public int CompanyId { get; set; }
        public int PeriodId { get; set; }
        public int VoucherTypeId { get; set; }
        public DateTime Date { get; set; }
        public string Description { get; set; }
    }

    public class VoucherLine
    {
        public int Id { get; set; }
        public int VoucherId { get; set; }
        public int AccountId { get; set; }
        public decimal Debit { get; set; }
        public decimal Credit { get; set; }
        public string Description { get; set; }
    }

    public class Invoice
    {
        public int Id { get; set; }
        public int CompanyId { get; set; }
        public int CustomerId { get; set; }
        public string InvoiceNumber { get; set; }
        public DateTime Date { get; set; }
        public decimal TotalAmount { get; set; }
        public string Description { get; set; }
    }

    public class InvoiceLine
    {
        public int Id { get; set; }
        public int InvoiceId { get; set; }
        public string ItemName { get; set; }
        public decimal Quantity { get; set; }
        public decimal UnitPrice { get; set; }
        public decimal LineTotal { get; set; }
    }

    public class CurrentAccount
    {
        public int Id { get; set; }
        public int CompanyId { get; set; }
        public int CustomerId { get; set; }
        public decimal Debit { get; set; }
        public decimal Credit { get; set; }
        public decimal Balance { get; set; }
    }

    public class CurrentAccountTransaction
    {
        public int Id { get; set; }
        public int CompanyId { get; set; }
        public int CustomerId { get; set; }
        public string TransactionType { get; set; }
        public decimal Amount { get; set; }
        public string ReferenceNo { get; set; }
        public DateTime Date { get; set; }
        public string Description { get; set; }
    }

    public class Customer
    {
        public int Id { get; set; }
        public int CompanyId { get; set; }
        public string Name { get; set; }
        public string TaxNumber { get; set; }
        public string Phone { get; set; }
    }

    public class Supplier
    {
        public int Id { get; set; }
        public int CompanyId { get; set; }
        public string Name { get; set; }
        public string TaxNumber { get; set; }
        public string Phone { get; set; }
    }

    public class User
    {
        public int Id { get; set; }
        public string Username { get; set; }
        public string PasswordHash { get; set; }
    }

    public class TransactionLog
    {
        public int Id { get; set; }
        public int VoucherId { get; set; }
        public int UserId { get; set; }
        public DateTime CreatedDate { get; set; }
    }

    public class TrialBalanceItem
    {
        public string AccountCode { get; set; }
        public string AccountName { get; set; }
        public decimal TotalDebit { get; set; }
        public decimal TotalCredit { get; set; }
    }

    public class LedgerItem
    {
        public DateTime Date { get; set; }
        public string VoucherDescription { get; set; }
        public string LineDescription { get; set; }
        public decimal Debit { get; set; }
        public decimal Credit { get; set; }
    }

    public class IncomeStatementItem
    {
        public string Category { get; set; }
        public decimal Amount { get; set; }
    }

    public class VoucherCreateRequest
    {
        public int CompanyId { get; set; }
        public int PeriodId { get; set; }
        public int VoucherTypeId { get; set; }
        public DateTime Date { get; set; }
        public string Description { get; set; }
        public List<VoucherLineCreateRequest> Lines { get; set; }
        public int UserId { get; set; }
    }

    public class VoucherLineCreateRequest
    {
        public int AccountId { get; set; }
        public decimal Debit { get; set; }
        public decimal Credit { get; set; }
        public string Description { get; set; }
    }

    public class AppContext
    {
        public int ActiveCompanyId { get; set; }
        public int ActivePeriodId { get; set; }
        public int ActiveUserId { get; set; }
    }

    #endregion

    #region Database

    public static class DatabaseHelper
    {
        private static readonly string DbPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "mini_muhasebe.db");
        private static readonly string ConnectionString = "Data Source=" + DbPath + ";Version=3;foreign keys=true;";

        public static string GetConnectionString()
        {
            return ConnectionString;
        }

        public static SQLiteConnection CreateConnection()
        {
            var connection = new SQLiteConnection(ConnectionString);
            connection.Open();
            using (var cmd = new SQLiteCommand("PRAGMA foreign_keys = ON;", connection))
            {
                cmd.ExecuteNonQuery();
            }

            return connection;
        }

        public static void InitializeDatabase()
        {
            if (!File.Exists(DbPath))
            {
                SQLiteConnection.CreateFile(DbPath);
            }

            using (var connection = CreateConnection())
            using (var transaction = connection.BeginTransaction())
            {
                try
                {
                    CreateTables(connection, transaction);
                    SeedData(connection, transaction);
                    transaction.Commit();
                }
                catch
                {
                    transaction.Rollback();
                    throw;
                }
            }
        }

        private static void CreateTables(SQLiteConnection connection, SQLiteTransaction transaction)
        {
            var commands = new List<string>
            {
                @"CREATE TABLE IF NOT EXISTS Companies (
                    Id INTEGER PRIMARY KEY AUTOINCREMENT,
                    Name TEXT NOT NULL,
                    TaxNumber TEXT NOT NULL
                );",
                @"CREATE TABLE IF NOT EXISTS Periods (
                    Id INTEGER PRIMARY KEY AUTOINCREMENT,
                    CompanyId INTEGER NOT NULL,
                    Year INTEGER NOT NULL,
                    Month INTEGER NOT NULL,
                    IsClosed INTEGER NOT NULL DEFAULT 0,
                    FOREIGN KEY (CompanyId) REFERENCES Companies(Id) ON DELETE CASCADE
                );",
                @"CREATE TABLE IF NOT EXISTS Accounts (
                    Id INTEGER PRIMARY KEY AUTOINCREMENT,
                    CompanyId INTEGER NOT NULL,
                    Code TEXT NOT NULL,
                    Name TEXT NOT NULL,
                    Type TEXT NOT NULL,
                    ParentAccountId INTEGER NULL,
                    FOREIGN KEY (CompanyId) REFERENCES Companies(Id) ON DELETE CASCADE,
                    FOREIGN KEY (ParentAccountId) REFERENCES Accounts(Id) ON DELETE SET NULL
                );",
                @"CREATE TABLE IF NOT EXISTS VoucherTypes (
                    Id INTEGER PRIMARY KEY AUTOINCREMENT,
                    Name TEXT NOT NULL
                );",
                @"CREATE TABLE IF NOT EXISTS Vouchers (
                    Id INTEGER PRIMARY KEY AUTOINCREMENT,
                    CompanyId INTEGER NOT NULL,
                    PeriodId INTEGER NOT NULL,
                    VoucherTypeId INTEGER NOT NULL,
                    Date TEXT NOT NULL,
                    Description TEXT,
                    FOREIGN KEY (CompanyId) REFERENCES Companies(Id) ON DELETE CASCADE,
                    FOREIGN KEY (PeriodId) REFERENCES Periods(Id) ON DELETE RESTRICT,
                    FOREIGN KEY (VoucherTypeId) REFERENCES VoucherTypes(Id) ON DELETE RESTRICT
                );",
                @"CREATE TABLE IF NOT EXISTS VoucherLines (
                    Id INTEGER PRIMARY KEY AUTOINCREMENT,
                    VoucherId INTEGER NOT NULL,
                    AccountId INTEGER NOT NULL,
                    Debit REAL NOT NULL DEFAULT 0,
                    Credit REAL NOT NULL DEFAULT 0,
                    Description TEXT,
                    FOREIGN KEY (VoucherId) REFERENCES Vouchers(Id) ON DELETE CASCADE,
                    FOREIGN KEY (AccountId) REFERENCES Accounts(Id) ON DELETE RESTRICT
                );",
                @"CREATE TABLE IF NOT EXISTS Customers (
                    Id INTEGER PRIMARY KEY AUTOINCREMENT,
                    CompanyId INTEGER NOT NULL,
                    Name TEXT NOT NULL,
                    TaxNumber TEXT,
                    Phone TEXT,
                    FOREIGN KEY (CompanyId) REFERENCES Companies(Id) ON DELETE CASCADE
                );",
                @"CREATE TABLE IF NOT EXISTS Suppliers (
                    Id INTEGER PRIMARY KEY AUTOINCREMENT,
                    CompanyId INTEGER NOT NULL,
                    Name TEXT NOT NULL,
                    TaxNumber TEXT,
                    Phone TEXT,
                    FOREIGN KEY (CompanyId) REFERENCES Companies(Id) ON DELETE CASCADE
                );",
                @"CREATE TABLE IF NOT EXISTS Users (
                    Id INTEGER PRIMARY KEY AUTOINCREMENT,
                    Username TEXT NOT NULL UNIQUE,
                    PasswordHash TEXT NOT NULL
                );",
                @"CREATE TABLE IF NOT EXISTS TransactionLogs (
                    Id INTEGER PRIMARY KEY AUTOINCREMENT,
                    VoucherId INTEGER NOT NULL,
                    UserId INTEGER NOT NULL,
                    CreatedDate TEXT NOT NULL,
                    FOREIGN KEY (VoucherId) REFERENCES Vouchers(Id) ON DELETE CASCADE,
                    FOREIGN KEY (UserId) REFERENCES Users(Id) ON DELETE RESTRICT
                );",
                @"CREATE TABLE IF NOT EXISTS Invoices (
                    Id INTEGER PRIMARY KEY AUTOINCREMENT,
                    CompanyId INTEGER NOT NULL,
                    CustomerId INTEGER NOT NULL,
                    InvoiceNumber TEXT NOT NULL UNIQUE,
                    Date TEXT NOT NULL,
                    TotalAmount REAL NOT NULL DEFAULT 0,
                    Description TEXT,
                    FOREIGN KEY (CompanyId) REFERENCES Companies(Id) ON DELETE CASCADE,
                    FOREIGN KEY (CustomerId) REFERENCES Customers(Id) ON DELETE RESTRICT
                );",
                @"CREATE TABLE IF NOT EXISTS InvoiceLines (
                    Id INTEGER PRIMARY KEY AUTOINCREMENT,
                    InvoiceId INTEGER NOT NULL,
                    ItemName TEXT NOT NULL,
                    Quantity REAL NOT NULL DEFAULT 0,
                    UnitPrice REAL NOT NULL DEFAULT 0,
                    LineTotal REAL NOT NULL DEFAULT 0,
                    FOREIGN KEY (InvoiceId) REFERENCES Invoices(Id) ON DELETE CASCADE
                );",
                @"CREATE TABLE IF NOT EXISTS CurrentAccounts (
                    Id INTEGER PRIMARY KEY AUTOINCREMENT,
                    CompanyId INTEGER NOT NULL,
                    CustomerId INTEGER NOT NULL UNIQUE,
                    Debit REAL NOT NULL DEFAULT 0,
                    Credit REAL NOT NULL DEFAULT 0,
                    Balance REAL NOT NULL DEFAULT 0,
                    FOREIGN KEY (CompanyId) REFERENCES Companies(Id) ON DELETE CASCADE,
                    FOREIGN KEY (CustomerId) REFERENCES Customers(Id) ON DELETE CASCADE
                );",
                @"CREATE TABLE IF NOT EXISTS CurrentAccountTransactions (
                    Id INTEGER PRIMARY KEY AUTOINCREMENT,
                    CompanyId INTEGER NOT NULL,
                    CustomerId INTEGER NOT NULL,
                    TransactionType TEXT NOT NULL,
                    Amount REAL NOT NULL DEFAULT 0,
                    ReferenceNo TEXT,
                    Date TEXT NOT NULL,
                    Description TEXT,
                    FOREIGN KEY (CompanyId) REFERENCES Companies(Id) ON DELETE CASCADE,
                    FOREIGN KEY (CustomerId) REFERENCES Customers(Id) ON DELETE CASCADE
                );",
                @"CREATE INDEX IF NOT EXISTS IDX_Accounts_CompanyId ON Accounts(CompanyId);",
                @"CREATE INDEX IF NOT EXISTS IDX_Vouchers_CompanyId ON Vouchers(CompanyId);",
                @"CREATE INDEX IF NOT EXISTS IDX_VoucherLines_VoucherId ON VoucherLines(VoucherId);",
                @"CREATE INDEX IF NOT EXISTS IDX_VoucherLines_AccountId ON VoucherLines(AccountId);",
                @"CREATE INDEX IF NOT EXISTS IDX_Periods_CompanyId ON Periods(CompanyId);",
                @"CREATE INDEX IF NOT EXISTS IDX_TransactionLogs_VoucherId ON TransactionLogs(VoucherId);",
                @"CREATE INDEX IF NOT EXISTS IDX_Invoices_CompanyId ON Invoices(CompanyId);",
                @"CREATE INDEX IF NOT EXISTS IDX_InvoiceLines_InvoiceId ON InvoiceLines(InvoiceId);",
                @"CREATE INDEX IF NOT EXISTS IDX_CurrentAccounts_CustomerId ON CurrentAccounts(CustomerId);",
                @"CREATE INDEX IF NOT EXISTS IDX_CurrentAccountTransactions_CustomerId ON CurrentAccountTransactions(CustomerId);"
            };

            foreach (var sql in commands)
            {
                using (var cmd = new SQLiteCommand(sql, connection, transaction))
                {
                    cmd.ExecuteNonQuery();
                }
            }
        }

        private static void SeedData(SQLiteConnection connection, SQLiteTransaction transaction)
        {
            if (GetScalarInt(connection, transaction, "SELECT COUNT(1) FROM Companies;") == 0)
            {
                using (var cmd = new SQLiteCommand("INSERT INTO Companies (Name, TaxNumber) VALUES (@Name, @Tax);", connection, transaction))
                {
                    cmd.Parameters.AddWithValue("@Name", "Mini Muhasebe A.Ş.");
                    cmd.Parameters.AddWithValue("@Tax", "1234567890");
                    cmd.ExecuteNonQuery();
                }
            }

            var companyId = GetScalarInt(connection, transaction, "SELECT Id FROM Companies ORDER BY Id LIMIT 1;");
            if (companyId <= 0)
            {
                throw new InvalidOperationException("Şirket seed işlemi başarısız.");
            }

            if (GetScalarInt(connection, transaction, "SELECT COUNT(1) FROM Periods WHERE CompanyId = @CompanyId;", new SQLiteParameter("@CompanyId", companyId)) == 0)
            {
                var now = DateTime.Now;
                using (var cmd = new SQLiteCommand("INSERT INTO Periods (CompanyId, Year, Month, IsClosed) VALUES (@CompanyId, @Year, @Month, 0);", connection, transaction))
                {
                    cmd.Parameters.AddWithValue("@CompanyId", companyId);
                    cmd.Parameters.AddWithValue("@Year", now.Year);
                    cmd.Parameters.AddWithValue("@Month", now.Month);
                    cmd.ExecuteNonQuery();
                }
            }

            if (GetScalarInt(connection, transaction, "SELECT COUNT(1) FROM Accounts WHERE CompanyId = @CompanyId;", new SQLiteParameter("@CompanyId", companyId)) == 0)
            {
                InsertAccount(connection, transaction, companyId, "100", "Kasa", "Asset", null);
                InsertAccount(connection, transaction, companyId, "102", "Banka", "Asset", null);
                InsertAccount(connection, transaction, companyId, "600", "Gelirler", "Income", null);
                InsertAccount(connection, transaction, companyId, "700", "Giderler", "Expense", null);
            }

            if (GetScalarInt(connection, transaction, "SELECT COUNT(1) FROM VoucherTypes;") == 0)
            {
                var types = new[] { "Mahsup Fişi", "Tahsilat Fişi", "Tediye Fişi", "Açılış Fişi" };
                foreach (var type in types)
                {
                    using (var cmd = new SQLiteCommand("INSERT INTO VoucherTypes (Name) VALUES (@Name);", connection, transaction))
                    {
                        cmd.Parameters.AddWithValue("@Name", type);
                        cmd.ExecuteNonQuery();
                    }
                }
            }

            if (GetScalarInt(connection, transaction, "SELECT COUNT(1) FROM Customers WHERE CompanyId = @CompanyId;", new SQLiteParameter("@CompanyId", companyId)) == 0)
            {
                using (var cmd = new SQLiteCommand("INSERT INTO Customers (CompanyId, Name, TaxNumber, Phone) VALUES (@CompanyId, @Name, @TaxNumber, @Phone);", connection, transaction))
                {
                    cmd.Parameters.AddWithValue("@CompanyId", companyId);
                    cmd.Parameters.AddWithValue("@Name", "Örnek Müşteri Ltd.");
                    cmd.Parameters.AddWithValue("@TaxNumber", "1112223334");
                    cmd.Parameters.AddWithValue("@Phone", "0555 000 00 01");
                    cmd.ExecuteNonQuery();
                }
            }

            if (GetScalarInt(connection, transaction, "SELECT COUNT(1) FROM Suppliers WHERE CompanyId = @CompanyId;", new SQLiteParameter("@CompanyId", companyId)) == 0)
            {
                using (var cmd = new SQLiteCommand("INSERT INTO Suppliers (CompanyId, Name, TaxNumber, Phone) VALUES (@CompanyId, @Name, @TaxNumber, @Phone);", connection, transaction))
                {
                    cmd.Parameters.AddWithValue("@CompanyId", companyId);
                    cmd.Parameters.AddWithValue("@Name", "Örnek Tedarikçi A.Ş.");
                    cmd.Parameters.AddWithValue("@TaxNumber", "9998887776");
                    cmd.Parameters.AddWithValue("@Phone", "0555 000 00 02");
                    cmd.ExecuteNonQuery();
                }
            }

            if (GetScalarInt(connection, transaction, "SELECT COUNT(1) FROM Users;") == 0)
            {
                var hash = SecurityHelper.ComputeSha256("admin123");
                using (var cmd = new SQLiteCommand("INSERT INTO Users (Username, PasswordHash) VALUES (@Username, @PasswordHash);", connection, transaction))
                {
                    cmd.Parameters.AddWithValue("@Username", "admin");
                    cmd.Parameters.AddWithValue("@PasswordHash", hash);
                    cmd.ExecuteNonQuery();
                }
            }

            using (var cmd = new SQLiteCommand(@"
                INSERT OR IGNORE INTO CurrentAccounts (CompanyId, CustomerId, Debit, Credit, Balance)
                SELECT CompanyId, Id, 0, 0, 0 FROM Customers WHERE CompanyId = @CompanyId;", connection, transaction))
            {
                cmd.Parameters.AddWithValue("@CompanyId", companyId);
                cmd.ExecuteNonQuery();
            }
        }

        private static void InsertAccount(SQLiteConnection connection, SQLiteTransaction transaction, int companyId, string code, string name, string type, int? parentId)
        {
            using (var cmd = new SQLiteCommand(@"INSERT INTO Accounts (CompanyId, Code, Name, Type, ParentAccountId)
                                                VALUES (@CompanyId, @Code, @Name, @Type, @ParentAccountId);", connection, transaction))
            {
                cmd.Parameters.AddWithValue("@CompanyId", companyId);
                cmd.Parameters.AddWithValue("@Code", code);
                cmd.Parameters.AddWithValue("@Name", name);
                cmd.Parameters.AddWithValue("@Type", type);
                cmd.Parameters.AddWithValue("@ParentAccountId", (object)parentId ?? DBNull.Value);
                cmd.ExecuteNonQuery();
            }
        }

        private static int GetScalarInt(SQLiteConnection connection, SQLiteTransaction transaction, string sql, params SQLiteParameter[] parameters)
        {
            using (var cmd = new SQLiteCommand(sql, connection, transaction))
            {
                if (parameters != null && parameters.Any())
                {
                    cmd.Parameters.AddRange(parameters);
                }

                var result = cmd.ExecuteScalar();
                if (result == null || result == DBNull.Value)
                {
                    return 0;
                }

                return Convert.ToInt32(result);
            }
        }
    }

    #endregion

    #region Infrastructure

    public static class DataMapper
    {
        public static int GetInt(SQLiteDataReader reader, string name)
        {
            var value = reader[name];
            return value == DBNull.Value ? 0 : Convert.ToInt32(value);
        }

        public static int? GetNullableInt(SQLiteDataReader reader, string name)
        {
            var value = reader[name];
            return value == DBNull.Value ? (int?)null : Convert.ToInt32(value);
        }

        public static decimal GetDecimal(SQLiteDataReader reader, string name)
        {
            var value = reader[name];
            return value == DBNull.Value ? 0m : Convert.ToDecimal(value);
        }

        public static string GetString(SQLiteDataReader reader, string name)
        {
            var value = reader[name];
            return value == DBNull.Value ? string.Empty : Convert.ToString(value);
        }

        public static DateTime GetDate(SQLiteDataReader reader, string name)
        {
            var str = GetString(reader, name);
            DateTime date;
            if (DateTime.TryParse(str, out date))
            {
                return date;
            }

            return DateTime.MinValue;
        }

        public static bool GetBool(SQLiteDataReader reader, string name)
        {
            var value = reader[name];
            if (value == DBNull.Value)
            {
                return false;
            }

            return Convert.ToInt32(value) == 1;
        }
    }

    public static class SecurityHelper
    {
        public static string ComputeSha256(string value)
        {
            if (value == null)
            {
                value = string.Empty;
            }

            using (var sha256 = SHA256.Create())
            {
                var bytes = Encoding.UTF8.GetBytes(value);
                var hash = sha256.ComputeHash(bytes);
                var sb = new StringBuilder();
                foreach (var b in hash)
                {
                    sb.Append(b.ToString("x2"));
                }

                return sb.ToString();
            }
        }
    }

    public static class Guard
    {
        public static void AgainstNull(object value, string message)
        {
            if (value == null)
            {
                throw new ArgumentNullException(message);
            }
        }

        public static void AgainstNullOrWhiteSpace(string value, string message)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                throw new ArgumentException(message);
            }
        }

        public static void AgainstNegative(decimal value, string message)
        {
            if (value < 0)
            {
                throw new ArgumentException(message);
            }
        }
    }

    #endregion

    #region Repositories

    public interface ICompanyRepository
    {
        List<Company> GetAll();
        Company GetById(int id);
    }

    public class CompanyRepository : ICompanyRepository
    {
        public List<Company> GetAll()
        {
            var result = new List<Company>();
            using (var connection = DatabaseHelper.CreateConnection())
            using (var cmd = new SQLiteCommand("SELECT Id, Name, TaxNumber FROM Companies ORDER BY Id;", connection))
            using (var reader = cmd.ExecuteReader())
            {
                while (reader.Read())
                {
                    result.Add(new Company
                    {
                        Id = DataMapper.GetInt(reader, "Id"),
                        Name = DataMapper.GetString(reader, "Name"),
                        TaxNumber = DataMapper.GetString(reader, "TaxNumber")
                    });
                }
            }

            return result;
        }

        public Company GetById(int id)
        {
            using (var connection = DatabaseHelper.CreateConnection())
            using (var cmd = new SQLiteCommand("SELECT Id, Name, TaxNumber FROM Companies WHERE Id = @Id;", connection))
            {
                cmd.Parameters.AddWithValue("@Id", id);
                using (var reader = cmd.ExecuteReader())
                {
                    if (reader.Read())
                    {
                        return new Company
                        {
                            Id = DataMapper.GetInt(reader, "Id"),
                            Name = DataMapper.GetString(reader, "Name"),
                            TaxNumber = DataMapper.GetString(reader, "TaxNumber")
                        };
                    }
                }
            }

            return null;
        }
    }

    public interface IPeriodRepository
    {
        List<Period> GetByCompanyId(int companyId);
        Period GetById(int id);
        Period GetCurrent(int companyId);
    }

    public class PeriodRepository : IPeriodRepository
    {
        public List<Period> GetByCompanyId(int companyId)
        {
            var result = new List<Period>();
            using (var connection = DatabaseHelper.CreateConnection())
            using (var cmd = new SQLiteCommand("SELECT Id, CompanyId, Year, Month, IsClosed FROM Periods WHERE CompanyId = @CompanyId ORDER BY Year DESC, Month DESC;", connection))
            {
                cmd.Parameters.AddWithValue("@CompanyId", companyId);
                using (var reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        result.Add(Map(reader));
                    }
                }
            }

            return result;
        }

        public Period GetById(int id)
        {
            using (var connection = DatabaseHelper.CreateConnection())
            using (var cmd = new SQLiteCommand("SELECT Id, CompanyId, Year, Month, IsClosed FROM Periods WHERE Id = @Id;", connection))
            {
                cmd.Parameters.AddWithValue("@Id", id);
                using (var reader = cmd.ExecuteReader())
                {
                    if (reader.Read())
                    {
                        return Map(reader);
                    }
                }
            }

            return null;
        }

        public Period GetCurrent(int companyId)
        {
            using (var connection = DatabaseHelper.CreateConnection())
            using (var cmd = new SQLiteCommand(@"SELECT Id, CompanyId, Year, Month, IsClosed FROM Periods 
                                                WHERE CompanyId = @CompanyId ORDER BY Year DESC, Month DESC LIMIT 1;", connection))
            {
                cmd.Parameters.AddWithValue("@CompanyId", companyId);
                using (var reader = cmd.ExecuteReader())
                {
                    if (reader.Read())
                    {
                        return Map(reader);
                    }
                }
            }

            return null;
        }

        private Period Map(SQLiteDataReader reader)
        {
            return new Period
            {
                Id = DataMapper.GetInt(reader, "Id"),
                CompanyId = DataMapper.GetInt(reader, "CompanyId"),
                Year = DataMapper.GetInt(reader, "Year"),
                Month = DataMapper.GetInt(reader, "Month"),
                IsClosed = DataMapper.GetBool(reader, "IsClosed")
            };
        }
    }

    public interface IAccountRepository
    {
        List<Account> GetByCompany(int companyId);
        Account GetById(int id);
        int Insert(Account account);
        void Update(Account account);
        void Delete(int id);
    }

    public class AccountRepository : IAccountRepository
    {
        public List<Account> GetByCompany(int companyId)
        {
            var result = new List<Account>();
            using (var connection = DatabaseHelper.CreateConnection())
            using (var cmd = new SQLiteCommand(@"SELECT Id, CompanyId, Code, Name, Type, ParentAccountId
                                                FROM Accounts WHERE CompanyId = @CompanyId ORDER BY Code;", connection))
            {
                cmd.Parameters.AddWithValue("@CompanyId", companyId);
                using (var reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        result.Add(Map(reader));
                    }
                }
            }

            return result;
        }

        public Account GetById(int id)
        {
            using (var connection = DatabaseHelper.CreateConnection())
            using (var cmd = new SQLiteCommand(@"SELECT Id, CompanyId, Code, Name, Type, ParentAccountId
                                                FROM Accounts WHERE Id = @Id;", connection))
            {
                cmd.Parameters.AddWithValue("@Id", id);
                using (var reader = cmd.ExecuteReader())
                {
                    if (reader.Read())
                    {
                        return Map(reader);
                    }
                }
            }

            return null;
        }

        public int Insert(Account account)
        {
            Guard.AgainstNull(account, "Hesap boş olamaz.");
            using (var connection = DatabaseHelper.CreateConnection())
            using (var cmd = new SQLiteCommand(@"INSERT INTO Accounts (CompanyId, Code, Name, Type, ParentAccountId)
                                                VALUES (@CompanyId, @Code, @Name, @Type, @ParentAccountId);
                                                SELECT last_insert_rowid();", connection))
            {
                cmd.Parameters.AddWithValue("@CompanyId", account.CompanyId);
                cmd.Parameters.AddWithValue("@Code", account.Code);
                cmd.Parameters.AddWithValue("@Name", account.Name);
                cmd.Parameters.AddWithValue("@Type", account.Type);
                cmd.Parameters.AddWithValue("@ParentAccountId", (object)account.ParentAccountId ?? DBNull.Value);
                var result = cmd.ExecuteScalar();
                return Convert.ToInt32(result);
            }
        }

        public void Update(Account account)
        {
            Guard.AgainstNull(account, "Hesap boş olamaz.");
            using (var connection = DatabaseHelper.CreateConnection())
            using (var cmd = new SQLiteCommand(@"UPDATE Accounts SET Code=@Code, Name=@Name, Type=@Type, ParentAccountId=@ParentAccountId
                                                WHERE Id=@Id;", connection))
            {
                cmd.Parameters.AddWithValue("@Code", account.Code);
                cmd.Parameters.AddWithValue("@Name", account.Name);
                cmd.Parameters.AddWithValue("@Type", account.Type);
                cmd.Parameters.AddWithValue("@ParentAccountId", (object)account.ParentAccountId ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@Id", account.Id);
                cmd.ExecuteNonQuery();
            }
        }

        public void Delete(int id)
        {
            using (var connection = DatabaseHelper.CreateConnection())
            using (var cmd = new SQLiteCommand("DELETE FROM Accounts WHERE Id=@Id;", connection))
            {
                cmd.Parameters.AddWithValue("@Id", id);
                cmd.ExecuteNonQuery();
            }
        }

        private Account Map(SQLiteDataReader reader)
        {
            return new Account
            {
                Id = DataMapper.GetInt(reader, "Id"),
                CompanyId = DataMapper.GetInt(reader, "CompanyId"),
                Code = DataMapper.GetString(reader, "Code"),
                Name = DataMapper.GetString(reader, "Name"),
                Type = DataMapper.GetString(reader, "Type"),
                ParentAccountId = DataMapper.GetNullableInt(reader, "ParentAccountId")
            };
        }
    }

    public interface IVoucherTypeRepository
    {
        List<VoucherType> GetAll();
    }

    public class VoucherTypeRepository : IVoucherTypeRepository
    {
        public List<VoucherType> GetAll()
        {
            var result = new List<VoucherType>();
            using (var connection = DatabaseHelper.CreateConnection())
            using (var cmd = new SQLiteCommand("SELECT Id, Name FROM VoucherTypes ORDER BY Name;", connection))
            using (var reader = cmd.ExecuteReader())
            {
                while (reader.Read())
                {
                    result.Add(new VoucherType
                    {
                        Id = DataMapper.GetInt(reader, "Id"),
                        Name = DataMapper.GetString(reader, "Name")
                    });
                }
            }

            return result;
        }
    }

    public interface IVoucherRepository
    {
        int InsertVoucherWithLinesAndLog(Voucher voucher, List<VoucherLine> lines, TransactionLog log);
        List<Voucher> GetByCompany(int companyId);
        List<VoucherLine> GetLinesByVoucher(int voucherId);
    }

    public class VoucherRepository : IVoucherRepository
    {
        public int InsertVoucherWithLinesAndLog(Voucher voucher, List<VoucherLine> lines, TransactionLog log)
        {
            Guard.AgainstNull(voucher, "Fiş boş olamaz.");
            Guard.AgainstNull(lines, "Fiş satırları boş olamaz.");
            Guard.AgainstNull(log, "Log boş olamaz.");

            using (var connection = DatabaseHelper.CreateConnection())
            using (var transaction = connection.BeginTransaction())
            {
                try
                {
                    int voucherId;
                    using (var cmd = new SQLiteCommand(@"INSERT INTO Vouchers (CompanyId, PeriodId, VoucherTypeId, Date, Description)
                                                        VALUES (@CompanyId, @PeriodId, @VoucherTypeId, @Date, @Description);
                                                        SELECT last_insert_rowid();", connection, transaction))
                    {
                        cmd.Parameters.AddWithValue("@CompanyId", voucher.CompanyId);
                        cmd.Parameters.AddWithValue("@PeriodId", voucher.PeriodId);
                        cmd.Parameters.AddWithValue("@VoucherTypeId", voucher.VoucherTypeId);
                        cmd.Parameters.AddWithValue("@Date", voucher.Date.ToString("yyyy-MM-dd"));
                        cmd.Parameters.AddWithValue("@Description", voucher.Description ?? string.Empty);
                        voucherId = Convert.ToInt32(cmd.ExecuteScalar());
                    }

                    foreach (var line in lines)
                    {
                        using (var lineCmd = new SQLiteCommand(@"INSERT INTO VoucherLines (VoucherId, AccountId, Debit, Credit, Description)
                                                                VALUES (@VoucherId, @AccountId, @Debit, @Credit, @Description);", connection, transaction))
                        {
                            lineCmd.Parameters.AddWithValue("@VoucherId", voucherId);
                            lineCmd.Parameters.AddWithValue("@AccountId", line.AccountId);
                            lineCmd.Parameters.AddWithValue("@Debit", line.Debit);
                            lineCmd.Parameters.AddWithValue("@Credit", line.Credit);
                            lineCmd.Parameters.AddWithValue("@Description", line.Description ?? string.Empty);
                            lineCmd.ExecuteNonQuery();
                        }
                    }

                    using (var logCmd = new SQLiteCommand(@"INSERT INTO TransactionLogs (VoucherId, UserId, CreatedDate)
                                                           VALUES (@VoucherId, @UserId, @CreatedDate);", connection, transaction))
                    {
                        logCmd.Parameters.AddWithValue("@VoucherId", voucherId);
                        logCmd.Parameters.AddWithValue("@UserId", log.UserId);
                        logCmd.Parameters.AddWithValue("@CreatedDate", log.CreatedDate.ToString("yyyy-MM-dd HH:mm:ss"));
                        logCmd.ExecuteNonQuery();
                    }

                    transaction.Commit();
                    return voucherId;
                }
                catch
                {
                    transaction.Rollback();
                    throw;
                }
            }
        }

        public List<Voucher> GetByCompany(int companyId)
        {
            var result = new List<Voucher>();
            using (var connection = DatabaseHelper.CreateConnection())
            using (var cmd = new SQLiteCommand(@"SELECT Id, CompanyId, PeriodId, VoucherTypeId, Date, Description
                                                FROM Vouchers WHERE CompanyId=@CompanyId ORDER BY Date DESC, Id DESC;", connection))
            {
                cmd.Parameters.AddWithValue("@CompanyId", companyId);
                using (var reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        result.Add(new Voucher
                        {
                            Id = DataMapper.GetInt(reader, "Id"),
                            CompanyId = DataMapper.GetInt(reader, "CompanyId"),
                            PeriodId = DataMapper.GetInt(reader, "PeriodId"),
                            VoucherTypeId = DataMapper.GetInt(reader, "VoucherTypeId"),
                            Date = DataMapper.GetDate(reader, "Date"),
                            Description = DataMapper.GetString(reader, "Description")
                        });
                    }
                }
            }

            return result;
        }

        public List<VoucherLine> GetLinesByVoucher(int voucherId)
        {
            var result = new List<VoucherLine>();
            using (var connection = DatabaseHelper.CreateConnection())
            using (var cmd = new SQLiteCommand(@"SELECT Id, VoucherId, AccountId, Debit, Credit, Description
                                                FROM VoucherLines WHERE VoucherId=@VoucherId ORDER BY Id;", connection))
            {
                cmd.Parameters.AddWithValue("@VoucherId", voucherId);
                using (var reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        result.Add(new VoucherLine
                        {
                            Id = DataMapper.GetInt(reader, "Id"),
                            VoucherId = DataMapper.GetInt(reader, "VoucherId"),
                            AccountId = DataMapper.GetInt(reader, "AccountId"),
                            Debit = DataMapper.GetDecimal(reader, "Debit"),
                            Credit = DataMapper.GetDecimal(reader, "Credit"),
                            Description = DataMapper.GetString(reader, "Description")
                        });
                    }
                }
            }

            return result;
        }
    }

    public interface ICustomerRepository
    {
        List<Customer> GetByCompany(int companyId);
        int Insert(Customer customer);
        void Update(Customer customer);
        void Delete(int id);
    }

    public class CustomerRepository : ICustomerRepository
    {
        public List<Customer> GetByCompany(int companyId)
        {
            var result = new List<Customer>();
            using (var connection = DatabaseHelper.CreateConnection())
            using (var cmd = new SQLiteCommand(@"SELECT Id, CompanyId, Name, TaxNumber, Phone FROM Customers
                                                WHERE CompanyId=@CompanyId ORDER BY Name;", connection))
            {
                cmd.Parameters.AddWithValue("@CompanyId", companyId);
                using (var reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        result.Add(new Customer
                        {
                            Id = DataMapper.GetInt(reader, "Id"),
                            CompanyId = DataMapper.GetInt(reader, "CompanyId"),
                            Name = DataMapper.GetString(reader, "Name"),
                            TaxNumber = DataMapper.GetString(reader, "TaxNumber"),
                            Phone = DataMapper.GetString(reader, "Phone")
                        });
                    }
                }
            }

            return result;
        }

        public int Insert(Customer customer)
        {
            using (var connection = DatabaseHelper.CreateConnection())
            using (var transaction = connection.BeginTransaction())
            {
                try
                {
                    int customerId;
                    using (var cmd = new SQLiteCommand(@"INSERT INTO Customers (CompanyId, Name, TaxNumber, Phone)
                                                         VALUES (@CompanyId, @Name, @TaxNumber, @Phone);
                                                         SELECT last_insert_rowid();", connection, transaction))
                    {
                        cmd.Parameters.AddWithValue("@CompanyId", customer.CompanyId);
                        cmd.Parameters.AddWithValue("@Name", customer.Name);
                        cmd.Parameters.AddWithValue("@TaxNumber", customer.TaxNumber ?? string.Empty);
                        cmd.Parameters.AddWithValue("@Phone", customer.Phone ?? string.Empty);
                        customerId = Convert.ToInt32(cmd.ExecuteScalar());
                    }

                    using (var ca = new SQLiteCommand(@"INSERT OR IGNORE INTO CurrentAccounts (CompanyId, CustomerId, Debit, Credit, Balance)
                                                        VALUES (@CompanyId, @CustomerId, 0, 0, 0);", connection, transaction))
                    {
                        ca.Parameters.AddWithValue("@CompanyId", customer.CompanyId);
                        ca.Parameters.AddWithValue("@CustomerId", customerId);
                        ca.ExecuteNonQuery();
                    }

                    transaction.Commit();
                    return customerId;
                }
                catch
                {
                    transaction.Rollback();
                    throw;
                }
            }
        }

        public void Update(Customer customer)
        {
            using (var connection = DatabaseHelper.CreateConnection())
            using (var cmd = new SQLiteCommand(@"UPDATE Customers SET Name=@Name, TaxNumber=@TaxNumber, Phone=@Phone
                                                WHERE Id=@Id;", connection))
            {
                cmd.Parameters.AddWithValue("@Name", customer.Name);
                cmd.Parameters.AddWithValue("@TaxNumber", customer.TaxNumber ?? string.Empty);
                cmd.Parameters.AddWithValue("@Phone", customer.Phone ?? string.Empty);
                cmd.Parameters.AddWithValue("@Id", customer.Id);
                cmd.ExecuteNonQuery();
            }
        }

        public void Delete(int id)
        {
            using (var connection = DatabaseHelper.CreateConnection())
            using (var cmd = new SQLiteCommand("DELETE FROM Customers WHERE Id=@Id;", connection))
            {
                cmd.Parameters.AddWithValue("@Id", id);
                cmd.ExecuteNonQuery();
            }
        }
    }

    public interface ISupplierRepository
    {
        List<Supplier> GetByCompany(int companyId);
        int Insert(Supplier supplier);
        void Update(Supplier supplier);
        void Delete(int id);
    }

    public class SupplierRepository : ISupplierRepository
    {
        public List<Supplier> GetByCompany(int companyId)
        {
            var result = new List<Supplier>();
            using (var connection = DatabaseHelper.CreateConnection())
            using (var cmd = new SQLiteCommand(@"SELECT Id, CompanyId, Name, TaxNumber, Phone FROM Suppliers
                                                WHERE CompanyId=@CompanyId ORDER BY Name;", connection))
            {
                cmd.Parameters.AddWithValue("@CompanyId", companyId);
                using (var reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        result.Add(new Supplier
                        {
                            Id = DataMapper.GetInt(reader, "Id"),
                            CompanyId = DataMapper.GetInt(reader, "CompanyId"),
                            Name = DataMapper.GetString(reader, "Name"),
                            TaxNumber = DataMapper.GetString(reader, "TaxNumber"),
                            Phone = DataMapper.GetString(reader, "Phone")
                        });
                    }
                }
            }

            return result;
        }

        public int Insert(Supplier supplier)
        {
            using (var connection = DatabaseHelper.CreateConnection())
            using (var cmd = new SQLiteCommand(@"INSERT INTO Suppliers (CompanyId, Name, TaxNumber, Phone)
                                                VALUES (@CompanyId, @Name, @TaxNumber, @Phone);
                                                SELECT last_insert_rowid();", connection))
            {
                cmd.Parameters.AddWithValue("@CompanyId", supplier.CompanyId);
                cmd.Parameters.AddWithValue("@Name", supplier.Name);
                cmd.Parameters.AddWithValue("@TaxNumber", supplier.TaxNumber ?? string.Empty);
                cmd.Parameters.AddWithValue("@Phone", supplier.Phone ?? string.Empty);
                return Convert.ToInt32(cmd.ExecuteScalar());
            }
        }

        public void Update(Supplier supplier)
        {
            using (var connection = DatabaseHelper.CreateConnection())
            using (var cmd = new SQLiteCommand(@"UPDATE Suppliers SET Name=@Name, TaxNumber=@TaxNumber, Phone=@Phone
                                                WHERE Id=@Id;", connection))
            {
                cmd.Parameters.AddWithValue("@Name", supplier.Name);
                cmd.Parameters.AddWithValue("@TaxNumber", supplier.TaxNumber ?? string.Empty);
                cmd.Parameters.AddWithValue("@Phone", supplier.Phone ?? string.Empty);
                cmd.Parameters.AddWithValue("@Id", supplier.Id);
                cmd.ExecuteNonQuery();
            }
        }

        public void Delete(int id)
        {
            using (var connection = DatabaseHelper.CreateConnection())
            using (var cmd = new SQLiteCommand("DELETE FROM Suppliers WHERE Id=@Id;", connection))
            {
                cmd.Parameters.AddWithValue("@Id", id);
                cmd.ExecuteNonQuery();
            }
        }
    }

    public interface IUserRepository
    {
        List<User> GetAll();
        int Insert(User user);
        void Update(User user);
        void Delete(int id);
    }

    public class UserRepository : IUserRepository
    {
        public List<User> GetAll()
        {
            var result = new List<User>();
            using (var connection = DatabaseHelper.CreateConnection())
            using (var cmd = new SQLiteCommand("SELECT Id, Username, PasswordHash FROM Users ORDER BY Username;", connection))
            using (var reader = cmd.ExecuteReader())
            {
                while (reader.Read())
                {
                    result.Add(new User
                    {
                        Id = DataMapper.GetInt(reader, "Id"),
                        Username = DataMapper.GetString(reader, "Username"),
                        PasswordHash = DataMapper.GetString(reader, "PasswordHash")
                    });
                }
            }

            return result;
        }

        public int Insert(User user)
        {
            using (var connection = DatabaseHelper.CreateConnection())
            using (var cmd = new SQLiteCommand(@"INSERT INTO Users (Username, PasswordHash) VALUES (@Username, @PasswordHash);
                                                SELECT last_insert_rowid();", connection))
            {
                cmd.Parameters.AddWithValue("@Username", user.Username);
                cmd.Parameters.AddWithValue("@PasswordHash", user.PasswordHash);
                return Convert.ToInt32(cmd.ExecuteScalar());
            }
        }

        public void Update(User user)
        {
            using (var connection = DatabaseHelper.CreateConnection())
            using (var cmd = new SQLiteCommand("UPDATE Users SET Username=@Username, PasswordHash=@PasswordHash WHERE Id=@Id;", connection))
            {
                cmd.Parameters.AddWithValue("@Username", user.Username);
                cmd.Parameters.AddWithValue("@PasswordHash", user.PasswordHash);
                cmd.Parameters.AddWithValue("@Id", user.Id);
                cmd.ExecuteNonQuery();
            }
        }

        public void Delete(int id)
        {
            using (var connection = DatabaseHelper.CreateConnection())
            using (var cmd = new SQLiteCommand("DELETE FROM Users WHERE Id=@Id;", connection))
            {
                cmd.Parameters.AddWithValue("@Id", id);
                cmd.ExecuteNonQuery();
            }
        }
    }

    public interface IInvoiceRepository
    {
        int InsertInvoiceWithLines(Invoice invoice, List<InvoiceLine> lines);
        List<Invoice> GetByCompany(int companyId);
        List<InvoiceLine> GetLines(int invoiceId);
    }

    public class InvoiceRepository : IInvoiceRepository
    {
        private readonly ICurrentAccountRepository _currentAccountRepository;

        public InvoiceRepository()
        {
            _currentAccountRepository = new CurrentAccountRepository();
        }

        public int InsertInvoiceWithLines(Invoice invoice, List<InvoiceLine> lines)
        {
            Guard.AgainstNull(invoice, "Fatura boş olamaz.");
            Guard.AgainstNull(lines, "Fatura satırları boş olamaz.");
            using (var connection = DatabaseHelper.CreateConnection())
            using (var transaction = connection.BeginTransaction())
            {
                try
                {
                    int invoiceId;
                    using (var cmd = new SQLiteCommand(@\"INSERT INTO Invoices (CompanyId, CustomerId, InvoiceNumber, Date, TotalAmount, Description)
                                                        VALUES (@CompanyId, @CustomerId, @InvoiceNumber, @Date, @TotalAmount, @Description);
                                                        SELECT last_insert_rowid();\", connection, transaction))
                    {
                        cmd.Parameters.AddWithValue("@CompanyId", invoice.CompanyId);
                        cmd.Parameters.AddWithValue("@CustomerId", invoice.CustomerId);
                        cmd.Parameters.AddWithValue("@InvoiceNumber", invoice.InvoiceNumber);
                        cmd.Parameters.AddWithValue("@Date", invoice.Date.ToString("yyyy-MM-dd"));
                        cmd.Parameters.AddWithValue("@TotalAmount", invoice.TotalAmount);
                        cmd.Parameters.AddWithValue("@Description", invoice.Description ?? string.Empty);
                        invoiceId = Convert.ToInt32(cmd.ExecuteScalar());
                    }

                    foreach (var line in lines)
                    {
                        using (var cmd = new SQLiteCommand(@\"INSERT INTO InvoiceLines (InvoiceId, ItemName, Quantity, UnitPrice, LineTotal)
                                                            VALUES (@InvoiceId, @ItemName, @Quantity, @UnitPrice, @LineTotal);\", connection, transaction))
                        {
                            cmd.Parameters.AddWithValue("@InvoiceId", invoiceId);
                            cmd.Parameters.AddWithValue("@ItemName", line.ItemName);
                            cmd.Parameters.AddWithValue("@Quantity", line.Quantity);
                            cmd.Parameters.AddWithValue("@UnitPrice", line.UnitPrice);
                            cmd.Parameters.AddWithValue("@LineTotal", line.LineTotal);
                            cmd.ExecuteNonQuery();
                        }
                    }

                    _currentAccountRepository.PostSale(invoice.CompanyId, invoice.CustomerId, invoice.TotalAmount, invoice.InvoiceNumber, "Fatura satışı", connection, transaction);

                    transaction.Commit();
                    return invoiceId;
                }
                catch
                {
                    transaction.Rollback();
                    throw;
                }
            }
        }

        public List<Invoice> GetByCompany(int companyId)
        {
            var list = new List<Invoice>();
            using (var connection = DatabaseHelper.CreateConnection())
            using (var cmd = new SQLiteCommand(@\"SELECT Id, CompanyId, CustomerId, InvoiceNumber, Date, TotalAmount, Description
                                                FROM Invoices WHERE CompanyId=@CompanyId ORDER BY Date DESC, Id DESC;\", connection))
            {
                cmd.Parameters.AddWithValue("@CompanyId", companyId);
                using (var reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        list.Add(new Invoice
                        {
                            Id = DataMapper.GetInt(reader, "Id"),
                            CompanyId = DataMapper.GetInt(reader, "CompanyId"),
                            CustomerId = DataMapper.GetInt(reader, "CustomerId"),
                            InvoiceNumber = DataMapper.GetString(reader, "InvoiceNumber"),
                            Date = DataMapper.GetDate(reader, "Date"),
                            TotalAmount = DataMapper.GetDecimal(reader, "TotalAmount"),
                            Description = DataMapper.GetString(reader, "Description")
                        });
                    }
                }
            }

            return list;
        }

        public List<InvoiceLine> GetLines(int invoiceId)
        {
            var list = new List<InvoiceLine>();
            using (var connection = DatabaseHelper.CreateConnection())
            using (var cmd = new SQLiteCommand(@\"SELECT Id, InvoiceId, ItemName, Quantity, UnitPrice, LineTotal
                                                FROM InvoiceLines WHERE InvoiceId=@InvoiceId ORDER BY Id;\", connection))
            {
                cmd.Parameters.AddWithValue("@InvoiceId", invoiceId);
                using (var reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        list.Add(new InvoiceLine
                        {
                            Id = DataMapper.GetInt(reader, "Id"),
                            InvoiceId = DataMapper.GetInt(reader, "InvoiceId"),
                            ItemName = DataMapper.GetString(reader, "ItemName"),
                            Quantity = DataMapper.GetDecimal(reader, "Quantity"),
                            UnitPrice = DataMapper.GetDecimal(reader, "UnitPrice"),
                            LineTotal = DataMapper.GetDecimal(reader, "LineTotal")
                        });
                    }
                }
            }

            return list;
        }
    }

    public interface ICurrentAccountRepository
    {
        void EnsureCurrentAccount(int companyId, int customerId, SQLiteConnection connection, SQLiteTransaction transaction);
        void PostSale(int companyId, int customerId, decimal amount, string referenceNo, string description, SQLiteConnection connection, SQLiteTransaction transaction);
        void PostCollection(int companyId, int customerId, decimal amount, string referenceNo, string description);
        List<CurrentAccount> GetByCompany(int companyId);
        List<CurrentAccountTransaction> GetTransactions(int companyId, int customerId);
    }

    public class CurrentAccountRepository : ICurrentAccountRepository
    {
        public void EnsureCurrentAccount(int companyId, int customerId, SQLiteConnection connection, SQLiteTransaction transaction)
        {
            using (var cmd = new SQLiteCommand(@"INSERT OR IGNORE INTO CurrentAccounts (CompanyId, CustomerId, Debit, Credit, Balance)
                                                 VALUES (@CompanyId, @CustomerId, 0, 0, 0);", connection, transaction))
            {
                cmd.Parameters.AddWithValue("@CompanyId", companyId);
                cmd.Parameters.AddWithValue("@CustomerId", customerId);
                cmd.ExecuteNonQuery();
            }
        }

        public void PostSale(int companyId, int customerId, decimal amount, string referenceNo, string description, SQLiteConnection connection, SQLiteTransaction transaction)
        {
            EnsureCurrentAccount(companyId, customerId, connection, transaction);
            using (var update = new SQLiteCommand(@"UPDATE CurrentAccounts
                                                    SET Debit = Debit + @Amount,
                                                        Balance = (Debit + @Amount) - Credit
                                                    WHERE CompanyId=@CompanyId AND CustomerId=@CustomerId;", connection, transaction))
            {
                update.Parameters.AddWithValue("@Amount", amount);
                update.Parameters.AddWithValue("@CompanyId", companyId);
                update.Parameters.AddWithValue("@CustomerId", customerId);
                update.ExecuteNonQuery();
            }

            using (var tx = new SQLiteCommand(@"INSERT INTO CurrentAccountTransactions
                                                (CompanyId, CustomerId, TransactionType, Amount, ReferenceNo, Date, Description)
                                                VALUES (@CompanyId, @CustomerId, 'SATIS', @Amount, @ReferenceNo, @Date, @Description);", connection, transaction))
            {
                tx.Parameters.AddWithValue("@CompanyId", companyId);
                tx.Parameters.AddWithValue("@CustomerId", customerId);
                tx.Parameters.AddWithValue("@Amount", amount);
                tx.Parameters.AddWithValue("@ReferenceNo", referenceNo ?? string.Empty);
                tx.Parameters.AddWithValue("@Date", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));
                tx.Parameters.AddWithValue("@Description", description ?? string.Empty);
                tx.ExecuteNonQuery();
            }
        }

        public void PostCollection(int companyId, int customerId, decimal amount, string referenceNo, string description)
        {
            using (var connection = DatabaseHelper.CreateConnection())
            using (var transaction = connection.BeginTransaction())
            {
                try
                {
                    EnsureCurrentAccount(companyId, customerId, connection, transaction);
                    using (var update = new SQLiteCommand(@"UPDATE CurrentAccounts
                                                            SET Credit = Credit + @Amount,
                                                                Balance = Debit - (Credit + @Amount)
                                                            WHERE CompanyId=@CompanyId AND CustomerId=@CustomerId;", connection, transaction))
                    {
                        update.Parameters.AddWithValue("@Amount", amount);
                        update.Parameters.AddWithValue("@CompanyId", companyId);
                        update.Parameters.AddWithValue("@CustomerId", customerId);
                        update.ExecuteNonQuery();
                    }

                    using (var tx = new SQLiteCommand(@"INSERT INTO CurrentAccountTransactions
                                                        (CompanyId, CustomerId, TransactionType, Amount, ReferenceNo, Date, Description)
                                                        VALUES (@CompanyId, @CustomerId, 'TAHSILAT', @Amount, @ReferenceNo, @Date, @Description);", connection, transaction))
                    {
                        tx.Parameters.AddWithValue("@CompanyId", companyId);
                        tx.Parameters.AddWithValue("@CustomerId", customerId);
                        tx.Parameters.AddWithValue("@Amount", amount);
                        tx.Parameters.AddWithValue("@ReferenceNo", referenceNo ?? string.Empty);
                        tx.Parameters.AddWithValue("@Date", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));
                        tx.Parameters.AddWithValue("@Description", description ?? string.Empty);
                        tx.ExecuteNonQuery();
                    }

                    transaction.Commit();
                }
                catch
                {
                    transaction.Rollback();
                    throw;
                }
            }
        }

        public List<CurrentAccount> GetByCompany(int companyId)
        {
            var list = new List<CurrentAccount>();
            using (var connection = DatabaseHelper.CreateConnection())
            using (var cmd = new SQLiteCommand(@"SELECT Id, CompanyId, CustomerId, Debit, Credit, Balance
                                                 FROM CurrentAccounts WHERE CompanyId=@CompanyId ORDER BY CustomerId;", connection))
            {
                cmd.Parameters.AddWithValue("@CompanyId", companyId);
                using (var reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        list.Add(new CurrentAccount
                        {
                            Id = DataMapper.GetInt(reader, "Id"),
                            CompanyId = DataMapper.GetInt(reader, "CompanyId"),
                            CustomerId = DataMapper.GetInt(reader, "CustomerId"),
                            Debit = DataMapper.GetDecimal(reader, "Debit"),
                            Credit = DataMapper.GetDecimal(reader, "Credit"),
                            Balance = DataMapper.GetDecimal(reader, "Balance")
                        });
                    }
                }
            }

            return list;
        }

        public List<CurrentAccountTransaction> GetTransactions(int companyId, int customerId)
        {
            var list = new List<CurrentAccountTransaction>();
            using (var connection = DatabaseHelper.CreateConnection())
            using (var cmd = new SQLiteCommand(@"SELECT Id, CompanyId, CustomerId, TransactionType, Amount, ReferenceNo, Date, Description
                                                 FROM CurrentAccountTransactions
                                                 WHERE CompanyId=@CompanyId AND CustomerId=@CustomerId
                                                 ORDER BY Date DESC, Id DESC;", connection))
            {
                cmd.Parameters.AddWithValue("@CompanyId", companyId);
                cmd.Parameters.AddWithValue("@CustomerId", customerId);
                using (var reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        list.Add(new CurrentAccountTransaction
                        {
                            Id = DataMapper.GetInt(reader, "Id"),
                            CompanyId = DataMapper.GetInt(reader, "CompanyId"),
                            CustomerId = DataMapper.GetInt(reader, "CustomerId"),
                            TransactionType = DataMapper.GetString(reader, "TransactionType"),
                            Amount = DataMapper.GetDecimal(reader, "Amount"),
                            ReferenceNo = DataMapper.GetString(reader, "ReferenceNo"),
                            Date = DataMapper.GetDate(reader, "Date"),
                            Description = DataMapper.GetString(reader, "Description")
                        });
                    }
                }
            }

            return list;
        }
    }

    public interface IReportRepository
    {
        List<TrialBalanceItem> GetTrialBalance(int companyId);
        List<LedgerItem> GetLedger(int companyId, int accountId);
        List<IncomeStatementItem> GetIncomeStatement(int companyId);
    }

    public class ReportRepository : IReportRepository
    {
        public List<TrialBalanceItem> GetTrialBalance(int companyId)
        {
            var result = new List<TrialBalanceItem>();
            using (var connection = DatabaseHelper.CreateConnection())
            using (var cmd = new SQLiteCommand(@"SELECT A.Code AS AccountCode, A.Name AS AccountName,
                                                       IFNULL(SUM(VL.Debit),0) AS TotalDebit,
                                                       IFNULL(SUM(VL.Credit),0) AS TotalCredit
                                                FROM Accounts A
                                                LEFT JOIN VoucherLines VL ON VL.AccountId = A.Id
                                                LEFT JOIN Vouchers V ON V.Id = VL.VoucherId
                                                WHERE A.CompanyId = @CompanyId
                                                GROUP BY A.Id, A.Code, A.Name
                                                ORDER BY A.Code;", connection))
            {
                cmd.Parameters.AddWithValue("@CompanyId", companyId);
                using (var reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        result.Add(new TrialBalanceItem
                        {
                            AccountCode = DataMapper.GetString(reader, "AccountCode"),
                            AccountName = DataMapper.GetString(reader, "AccountName"),
                            TotalDebit = DataMapper.GetDecimal(reader, "TotalDebit"),
                            TotalCredit = DataMapper.GetDecimal(reader, "TotalCredit")
                        });
                    }
                }
            }

            return result;
        }

        public List<LedgerItem> GetLedger(int companyId, int accountId)
        {
            var result = new List<LedgerItem>();
            using (var connection = DatabaseHelper.CreateConnection())
            using (var cmd = new SQLiteCommand(@"SELECT V.Date, V.Description AS VoucherDescription,
                                                       VL.Description AS LineDescription, VL.Debit, VL.Credit
                                                FROM VoucherLines VL
                                                INNER JOIN Vouchers V ON V.Id = VL.VoucherId
                                                INNER JOIN Accounts A ON A.Id = VL.AccountId
                                                WHERE V.CompanyId = @CompanyId AND VL.AccountId = @AccountId
                                                ORDER BY V.Date, VL.Id;", connection))
            {
                cmd.Parameters.AddWithValue("@CompanyId", companyId);
                cmd.Parameters.AddWithValue("@AccountId", accountId);
                using (var reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        result.Add(new LedgerItem
                        {
                            Date = DataMapper.GetDate(reader, "Date"),
                            VoucherDescription = DataMapper.GetString(reader, "VoucherDescription"),
                            LineDescription = DataMapper.GetString(reader, "LineDescription"),
                            Debit = DataMapper.GetDecimal(reader, "Debit"),
                            Credit = DataMapper.GetDecimal(reader, "Credit")
                        });
                    }
                }
            }

            return result;
        }

        public List<IncomeStatementItem> GetIncomeStatement(int companyId)
        {
            var items = new List<IncomeStatementItem>();
            using (var connection = DatabaseHelper.CreateConnection())
            using (var cmd = new SQLiteCommand(@"SELECT SUBSTR(A.Code,1,1) AS Prefix,
                                                       SUM(VL.Credit - VL.Debit) AS Amount
                                                FROM Accounts A
                                                LEFT JOIN VoucherLines VL ON VL.AccountId = A.Id
                                                LEFT JOIN Vouchers V ON V.Id = VL.VoucherId
                                                WHERE A.CompanyId = @CompanyId AND (A.Code LIKE '6%' OR A.Code LIKE '7%')
                                                GROUP BY Prefix;", connection))
            {
                cmd.Parameters.AddWithValue("@CompanyId", companyId);
                using (var reader = cmd.ExecuteReader())
                {
                    decimal income = 0m;
                    decimal expense = 0m;
                    while (reader.Read())
                    {
                        var prefix = DataMapper.GetString(reader, "Prefix");
                        var amount = DataMapper.GetDecimal(reader, "Amount");
                        if (prefix == "6")
                        {
                            income = amount;
                        }
                        else if (prefix == "7")
                        {
                            expense = Math.Abs(amount);
                        }
                    }

                    items.Add(new IncomeStatementItem { Category = "Toplam Gelir", Amount = income });
                    items.Add(new IncomeStatementItem { Category = "Toplam Gider", Amount = expense });
                    items.Add(new IncomeStatementItem { Category = "Net Kar", Amount = income - expense });
                }
            }

            return items;
        }
    }

    #endregion

    #region Services

    public interface IAccountingService
    {
        int CreateVoucher(VoucherCreateRequest request);
        List<Voucher> GetVouchers(int companyId);
        List<VoucherLine> GetVoucherLines(int voucherId);
    }

    public class AccountingService : IAccountingService
    {
        private readonly IVoucherRepository _voucherRepository;
        private readonly IPeriodRepository _periodRepository;

        public AccountingService(IVoucherRepository voucherRepository, IPeriodRepository periodRepository)
        {
            _voucherRepository = voucherRepository;
            _periodRepository = periodRepository;
        }

        public int CreateVoucher(VoucherCreateRequest request)
        {
            Guard.AgainstNull(request, "Fiş talebi boş olamaz.");
            Guard.AgainstNull(request.Lines, "Fiş satırları boş olamaz.");

            if (request.Lines.Count < 2)
            {
                throw new InvalidOperationException("Fiş en az 2 satır içermelidir.");
            }

            var period = _periodRepository.GetById(request.PeriodId);
            if (period == null)
            {
                throw new InvalidOperationException("Dönem bulunamadı.");
            }

            if (period.IsClosed)
            {
                throw new InvalidOperationException("Dönem kapalı, fiş kaydedilemez.");
            }

            var totalDebit = request.Lines.Sum(x => x.Debit);
            var totalCredit = request.Lines.Sum(x => x.Credit);

            if (totalDebit <= 0 || totalCredit <= 0)
            {
                throw new InvalidOperationException("Toplam borç ve alacak sıfırdan büyük olmalıdır.");
            }

            if (Math.Abs(totalDebit - totalCredit) > 0.0001m)
            {
                throw new InvalidOperationException("Toplam Borç ve Alacak eşit olmalıdır.");
            }

            foreach (var line in request.Lines)
            {
                Guard.AgainstNegative(line.Debit, "Borç negatif olamaz.");
                Guard.AgainstNegative(line.Credit, "Alacak negatif olamaz.");

                if (line.Debit > 0 && line.Credit > 0)
                {
                    throw new InvalidOperationException("Bir satırda hem borç hem alacak dolu olamaz.");
                }

                if (line.Debit == 0 && line.Credit == 0)
                {
                    throw new InvalidOperationException("Satırda borç veya alacaktan biri sıfırdan büyük olmalıdır.");
                }
            }

            var voucher = new Voucher
            {
                CompanyId = request.CompanyId,
                PeriodId = request.PeriodId,
                VoucherTypeId = request.VoucherTypeId,
                Date = request.Date,
                Description = request.Description
            };

            var lines = request.Lines.Select(x => new VoucherLine
            {
                AccountId = x.AccountId,
                Debit = x.Debit,
                Credit = x.Credit,
                Description = x.Description
            }).ToList();

            var log = new TransactionLog
            {
                UserId = request.UserId,
                CreatedDate = DateTime.Now
            };

            return _voucherRepository.InsertVoucherWithLinesAndLog(voucher, lines, log);
        }

        public List<Voucher> GetVouchers(int companyId)
        {
            return _voucherRepository.GetByCompany(companyId);
        }

        public List<VoucherLine> GetVoucherLines(int voucherId)
        {
            return _voucherRepository.GetLinesByVoucher(voucherId);
        }
    }

    public interface IAccountService
    {
        List<Account> GetAccounts(int companyId);
        int AddAccount(Account account);
        void UpdateAccount(Account account);
        void DeleteAccount(int accountId);
        List<AccountTreeNode> BuildTree(int companyId);
    }

    public class AccountTreeNode
    {
        public Account Account { get; set; }
        public List<AccountTreeNode> Children { get; set; }

        public AccountTreeNode()
        {
            Children = new List<AccountTreeNode>();
        }
    }

    public class AccountService : IAccountService
    {
        private readonly IAccountRepository _accountRepository;

        public AccountService(IAccountRepository accountRepository)
        {
            _accountRepository = accountRepository;
        }

        public List<Account> GetAccounts(int companyId)
        {
            return _accountRepository.GetByCompany(companyId);
        }

        public int AddAccount(Account account)
        {
            ValidateAccount(account);
            return _accountRepository.Insert(account);
        }

        public void UpdateAccount(Account account)
        {
            ValidateAccount(account);
            _accountRepository.Update(account);
        }

        public void DeleteAccount(int accountId)
        {
            _accountRepository.Delete(accountId);
        }

        public List<AccountTreeNode> BuildTree(int companyId)
        {
            var accounts = _accountRepository.GetByCompany(companyId);
            var dict = accounts.ToDictionary(
                x => x.Id,
                x => new AccountTreeNode
                {
                    Account = x,
                    Children = new List<AccountTreeNode>()
                });

            var roots = new List<AccountTreeNode>();
            foreach (var node in dict.Values)
            {
                if (node.Account.ParentAccountId.HasValue && dict.ContainsKey(node.Account.ParentAccountId.Value))
                {
                    dict[node.Account.ParentAccountId.Value].Children.Add(node);
                }
                else
                {
                    roots.Add(node);
                }
            }

            roots = roots.OrderBy(x => x.Account.Code).ToList();
            foreach (var root in roots)
            {
                SortChildrenRecursive(root);
            }

            return roots;
        }

        private void SortChildrenRecursive(AccountTreeNode node)
        {
            node.Children = node.Children.OrderBy(x => x.Account.Code).ToList();
            foreach (var child in node.Children)
            {
                SortChildrenRecursive(child);
            }
        }

        private void ValidateAccount(Account account)
        {
            Guard.AgainstNull(account, "Hesap boş olamaz.");
            Guard.AgainstNullOrWhiteSpace(account.Code, "Hesap kodu zorunludur.");
            Guard.AgainstNullOrWhiteSpace(account.Name, "Hesap adı zorunludur.");
            Guard.AgainstNullOrWhiteSpace(account.Type, "Hesap türü zorunludur.");
        }
    }

    public interface ICustomerService
    {
        List<Customer> GetCustomers(int companyId);
        int AddCustomer(Customer customer);
        void UpdateCustomer(Customer customer);
        void DeleteCustomer(int customerId);
    }

    public class CustomerService : ICustomerService
    {
        private readonly ICustomerRepository _customerRepository;

        public CustomerService(ICustomerRepository customerRepository)
        {
            _customerRepository = customerRepository;
        }

        public List<Customer> GetCustomers(int companyId)
        {
            return _customerRepository.GetByCompany(companyId);
        }

        public int AddCustomer(Customer customer)
        {
            Validate(customer);
            return _customerRepository.Insert(customer);
        }

        public void UpdateCustomer(Customer customer)
        {
            Validate(customer);
            _customerRepository.Update(customer);
        }

        public void DeleteCustomer(int customerId)
        {
            _customerRepository.Delete(customerId);
        }

        private void Validate(Customer customer)
        {
            Guard.AgainstNull(customer, "Müşteri boş olamaz.");
            Guard.AgainstNullOrWhiteSpace(customer.Name, "Müşteri adı zorunludur.");
        }
    }

    public interface ISupplierService
    {
        List<Supplier> GetSuppliers(int companyId);
        int AddSupplier(Supplier supplier);
        void UpdateSupplier(Supplier supplier);
        void DeleteSupplier(int supplierId);
    }

    public class SupplierService : ISupplierService
    {
        private readonly ISupplierRepository _supplierRepository;

        public SupplierService(ISupplierRepository supplierRepository)
        {
            _supplierRepository = supplierRepository;
        }

        public List<Supplier> GetSuppliers(int companyId)
        {
            return _supplierRepository.GetByCompany(companyId);
        }

        public int AddSupplier(Supplier supplier)
        {
            Validate(supplier);
            return _supplierRepository.Insert(supplier);
        }

        public void UpdateSupplier(Supplier supplier)
        {
            Validate(supplier);
            _supplierRepository.Update(supplier);
        }

        public void DeleteSupplier(int supplierId)
        {
            _supplierRepository.Delete(supplierId);
        }

        private void Validate(Supplier supplier)
        {
            Guard.AgainstNull(supplier, "Tedarikçi boş olamaz.");
            Guard.AgainstNullOrWhiteSpace(supplier.Name, "Tedarikçi adı zorunludur.");
        }
    }

    public interface IUserService
    {
        List<User> GetUsers();
        int AddUser(string username, string password);
        void UpdateUser(int id, string username, string password);
        void DeleteUser(int id);
    }

    public class UserService : IUserService
    {
        private readonly IUserRepository _userRepository;

        public UserService(IUserRepository userRepository)
        {
            _userRepository = userRepository;
        }

        public List<User> GetUsers()
        {
            return _userRepository.GetAll();
        }

        public int AddUser(string username, string password)
        {
            Guard.AgainstNullOrWhiteSpace(username, "Kullanıcı adı zorunludur.");
            Guard.AgainstNullOrWhiteSpace(password, "Parola zorunludur.");

            var user = new User
            {
                Username = username.Trim(),
                PasswordHash = SecurityHelper.ComputeSha256(password)
            };

            return _userRepository.Insert(user);
        }

        public void UpdateUser(int id, string username, string password)
        {
            Guard.AgainstNullOrWhiteSpace(username, "Kullanıcı adı zorunludur.");

            var users = _userRepository.GetAll();
            var existing = users.FirstOrDefault(x => x.Id == id);
            if (existing == null)
            {
                throw new InvalidOperationException("Kullanıcı bulunamadı.");
            }

            existing.Username = username.Trim();
            if (!string.IsNullOrWhiteSpace(password))
            {
                existing.PasswordHash = SecurityHelper.ComputeSha256(password);
            }

            _userRepository.Update(existing);
        }

        public void DeleteUser(int id)
        {
            _userRepository.Delete(id);
        }
    }

    public interface IInvoiceService
    {
        int CreateInvoice(Invoice invoice, List<InvoiceLine> lines);
        List<Invoice> GetInvoices(int companyId);
        List<InvoiceLine> GetInvoiceLines(int invoiceId);
    }

    public class InvoiceService : IInvoiceService
    {
        private readonly IInvoiceRepository _invoiceRepository;

        public InvoiceService(IInvoiceRepository invoiceRepository)
        {
            _invoiceRepository = invoiceRepository;
        }

        public int CreateInvoice(Invoice invoice, List<InvoiceLine> lines)
        {
            Guard.AgainstNull(invoice, "Fatura boş olamaz.");
            Guard.AgainstNull(lines, "Fatura satırları boş olamaz.");
            Guard.AgainstNullOrWhiteSpace(invoice.InvoiceNumber, "Fatura numarası zorunludur.");
            if (invoice.CustomerId <= 0)
            {
                throw new InvalidOperationException("Müşteri seçimi zorunludur.");
            }

            if (lines.Count == 0)
            {
                throw new InvalidOperationException("En az bir satır girilmelidir.");
            }

            foreach (var line in lines)
            {
                Guard.AgainstNullOrWhiteSpace(line.ItemName, "Satır ürün/hizmet adı zorunludur.");
                if (line.Quantity <= 0)
                {
                    throw new InvalidOperationException("Miktar sıfırdan büyük olmalıdır.");
                }

                if (line.UnitPrice < 0)
                {
                    throw new InvalidOperationException("Birim fiyat negatif olamaz.");
                }

                line.LineTotal = line.Quantity * line.UnitPrice;
            }

            invoice.TotalAmount = lines.Sum(x => x.LineTotal);
            return _invoiceRepository.InsertInvoiceWithLines(invoice, lines);
        }

        public List<Invoice> GetInvoices(int companyId)
        {
            return _invoiceRepository.GetByCompany(companyId);
        }

        public List<InvoiceLine> GetInvoiceLines(int invoiceId)
        {
            return _invoiceRepository.GetLines(invoiceId);
        }
    }

    public interface ICurrentAccountService
    {
        List<CurrentAccount> GetCurrentAccounts(int companyId);
        List<CurrentAccountTransaction> GetTransactions(int companyId, int customerId);
        void PostCollection(int companyId, int customerId, decimal amount, string referenceNo, string description);
    }

    public class CurrentAccountService : ICurrentAccountService
    {
        private readonly ICurrentAccountRepository _repository;

        public CurrentAccountService(ICurrentAccountRepository repository)
        {
            _repository = repository;
        }

        public List<CurrentAccount> GetCurrentAccounts(int companyId)
        {
            return _repository.GetByCompany(companyId);
        }

        public List<CurrentAccountTransaction> GetTransactions(int companyId, int customerId)
        {
            return _repository.GetTransactions(companyId, customerId);
        }

        public void PostCollection(int companyId, int customerId, decimal amount, string referenceNo, string description)
        {
            if (amount <= 0)
            {
                throw new InvalidOperationException("Tahsilat tutarı sıfırdan büyük olmalıdır.");
            }

            _repository.PostCollection(companyId, customerId, amount, referenceNo, description);
        }
    }

    public interface IReportService
    {
        List<TrialBalanceItem> GetTrialBalance(int companyId);
        List<LedgerItem> GetLedger(int companyId, int accountId);
        List<IncomeStatementItem> GetIncomeStatement(int companyId);
    }

    public class ReportService : IReportService
    {
        private readonly IReportRepository _reportRepository;

        public ReportService(IReportRepository reportRepository)
        {
            _reportRepository = reportRepository;
        }

        public List<TrialBalanceItem> GetTrialBalance(int companyId)
        {
            return _reportRepository.GetTrialBalance(companyId);
        }

        public List<LedgerItem> GetLedger(int companyId, int accountId)
        {
            return _reportRepository.GetLedger(companyId, accountId);
        }

        public List<IncomeStatementItem> GetIncomeStatement(int companyId)
        {
            return _reportRepository.GetIncomeStatement(companyId);
        }
    }

    public static class AppContextFactory
    {
        public static AppContext Create()
        {
            var companyRepo = new CompanyRepository();
            var periodRepo = new PeriodRepository();
            var userRepo = new UserRepository();

            var company = companyRepo.GetAll().FirstOrDefault();
            if (company == null)
            {
                throw new InvalidOperationException("Aktif şirket bulunamadı.");
            }

            var period = periodRepo.GetCurrent(company.Id);
            if (period == null)
            {
                throw new InvalidOperationException("Aktif dönem bulunamadı.");
            }

            var user = userRepo.GetAll().FirstOrDefault();
            if (user == null)
            {
                throw new InvalidOperationException("Aktif kullanıcı bulunamadı.");
            }

            return new AppContext
            {
                ActiveCompanyId = company.Id,
                ActivePeriodId = period.Id,
                ActiveUserId = user.Id
            };
        }
    }

    #endregion

    #region UI Common

    public static class UiHelper
    {
        public static DataGridView CreateReadOnlyGrid()
        {
            return new DataGridView
            {
                Dock = DockStyle.Fill,
                ReadOnly = true,
                AllowUserToAddRows = false,
                AllowUserToDeleteRows = false,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                MultiSelect = false,
                AutoGenerateColumns = true,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
                BackgroundColor = Color.White
            };
        }

        public static TextBox CreateTextBox(int width)
        {
            return new TextBox
            {
                Width = width,
                Margin = new Padding(6)
            };
        }

        public static Button CreateButton(string text, EventHandler click)
        {
            var button = new Button
            {
                Text = text,
                Width = 110,
                Height = 32,
                Margin = new Padding(6)
            };

            if (click != null)
            {
                button.Click += click;
            }

            return button;
        }

        public static ComboBox CreateComboBox(int width)
        {
            return new ComboBox
            {
                Width = width,
                DropDownStyle = ComboBoxStyle.DropDownList,
                Margin = new Padding(6)
            };
        }

        public static Label CreateLabel(string text, int width)
        {
            return new Label
            {
                Text = text,
                Width = width,
                TextAlign = ContentAlignment.MiddleLeft,
                Margin = new Padding(6)
            };
        }

        public static decimal ParseDecimal(string value)
        {
            decimal result;
            if (!decimal.TryParse(value, NumberStyles.Any, CultureInfo.CurrentCulture, out result))
            {
                if (!decimal.TryParse(value, NumberStyles.Any, CultureInfo.InvariantCulture, out result))
                {
                    result = 0m;
                }
            }

            return result;
        }
    }

    public static class ExportHelper
    {
        public static void ExportGridToCsv(DataGridView grid, IWin32Window owner, string defaultFileName)
        {
            if (grid == null || grid.Columns.Count == 0)
            {
                MessageBox.Show(owner, "Dışa aktarılacak veri yok.", "Bilgi", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            using (var dialog = new SaveFileDialog())
            {
                dialog.Filter = "Excel Uyumlu CSV (*.csv)|*.csv";
                dialog.FileName = defaultFileName + "_" + DateTime.Now.ToString("yyyyMMdd_HHmmss") + ".csv";
                if (dialog.ShowDialog(owner) != DialogResult.OK)
                {
                    return;
                }

                var lines = new List<string>();
                var headers = grid.Columns.Cast<DataGridViewColumn>().Select(c => EscapeCsv(c.HeaderText));
                lines.Add(string.Join(";", headers));

                foreach (DataGridViewRow row in grid.Rows)
                {
                    if (row.IsNewRow)
                    {
                        continue;
                    }

                    var cells = new List<string>();
                    foreach (DataGridViewColumn column in grid.Columns)
                    {
                        var value = row.Cells[column.Index].Value;
                        cells.Add(EscapeCsv(value == null ? string.Empty : Convert.ToString(value)));
                    }
                    lines.Add(string.Join(";", cells));
                }

                File.WriteAllLines(dialog.FileName, lines, Encoding.UTF8);
                MessageBox.Show(owner, "Excel (CSV) dışa aktarım tamamlandı.", "Başarılı", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
        }

        private static string EscapeCsv(string value)
        {
            if (string.IsNullOrEmpty(value))
            {
                return "\"\"";
            }

            var escaped = value.Replace("\"", "\"\"");
            return "\"" + escaped + "\"";
        }
    }

    #endregion

    #region Forms

    public class MainForm : Form
    {
        private readonly AppContext _appContext;
        private TabControl _tabs;

        public MainForm(AppContext appContext)
        {
            _appContext = appContext;
            InitializeUi();
        }

        private void InitializeUi()
        {
            Text = "Mini Muhasebe ERP - Tek Pencere Yönetim";
            Width = 1400;
            Height = 900;
            StartPosition = FormStartPosition.CenterScreen;
            WindowState = FormWindowState.Maximized;

            _tabs = new TabControl { Dock = DockStyle.Fill };

            AddFormTab("Hesaplar", new AccountForm(_appContext));
            AddFormTab("Fişler", new VoucherForm(_appContext));
            AddFormTab("Müşteriler", new CustomerForm(_appContext));
            AddFormTab("Tedarikçiler", new SupplierForm(_appContext));
            AddFormTab("Faturalar", new InvoiceForm(_appContext));
            AddFormTab("Cari Hesap", new CurrentAccountForm(_appContext));
            AddFormTab("Raporlar", new ReportForm(_appContext));
            AddFormTab("Kullanıcılar", new UserForm(_appContext));

            Controls.Add(_tabs);
        }

        private void AddFormTab(string title, Form form)
        {
            form.TopLevel = false;
            form.FormBorderStyle = FormBorderStyle.None;
            form.Dock = DockStyle.Fill;
            form.Visible = true;

            var tab = new TabPage(title);
            tab.Controls.Add(form);
            _tabs.TabPages.Add(tab);
        }
    }

    public class AccountForm : Form
    {
        private readonly AppContext _appContext;
        private readonly IAccountService _accountService;

        private DataGridView _grid;
        private TreeView _tree;
        private TextBox _txtCode;
        private TextBox _txtName;
        private ComboBox _cmbType;
        private ComboBox _cmbParent;
        private int? _selectedId;

        public AccountForm(AppContext appContext)
        {
            _appContext = appContext;
            _accountService = new AccountService(new AccountRepository());
            InitializeUi();
            LoadData();
        }

        private void InitializeUi()
        {
            Text = "Hesap Yönetimi";
            Width = 1100;
            Height = 700;
            StartPosition = FormStartPosition.CenterParent;

            var main = new SplitContainer
            {
                Dock = DockStyle.Fill,
                Orientation = Orientation.Vertical,
                SplitterDistance = 700
            };

            var leftPanel = new Panel { Dock = DockStyle.Fill };
            var rightPanel = new Panel { Dock = DockStyle.Fill };
            main.Panel1.Controls.Add(leftPanel);
            main.Panel2.Controls.Add(rightPanel);

            var formPanel = new FlowLayoutPanel
            {
                Dock = DockStyle.Top,
                Height = 130,
                FlowDirection = FlowDirection.LeftToRight,
                WrapContents = true
            };

            _txtCode = UiHelper.CreateTextBox(120);
            _txtName = UiHelper.CreateTextBox(180);
            _cmbType = UiHelper.CreateComboBox(120);
            _cmbParent = UiHelper.CreateComboBox(180);

            _cmbType.Items.AddRange(new object[] { "Asset", "Liability", "Equity", "Income", "Expense" });
            if (_cmbType.Items.Count > 0)
            {
                _cmbType.SelectedIndex = 0;
            }

            formPanel.Controls.Add(UiHelper.CreateLabel("Kod", 60));
            formPanel.Controls.Add(_txtCode);
            formPanel.Controls.Add(UiHelper.CreateLabel("Ad", 60));
            formPanel.Controls.Add(_txtName);
            formPanel.Controls.Add(UiHelper.CreateLabel("Tür", 60));
            formPanel.Controls.Add(_cmbType);
            formPanel.Controls.Add(UiHelper.CreateLabel("Üst Hesap", 80));
            formPanel.Controls.Add(_cmbParent);
            formPanel.Controls.Add(UiHelper.CreateButton("Ekle", OnAdd));
            formPanel.Controls.Add(UiHelper.CreateButton("Güncelle", OnUpdate));
            formPanel.Controls.Add(UiHelper.CreateButton("Sil", OnDelete));
            formPanel.Controls.Add(UiHelper.CreateButton("Temizle", OnClear));

            _grid = UiHelper.CreateReadOnlyGrid();
            _grid.Dock = DockStyle.Fill;
            _grid.SelectionChanged += OnGridSelectionChanged;

            leftPanel.Controls.Add(_grid);
            leftPanel.Controls.Add(formPanel);

            _tree = new TreeView
            {
                Dock = DockStyle.Fill,
                HideSelection = false
            };
            rightPanel.Controls.Add(_tree);

            Controls.Add(main);
        }

        private void LoadData()
        {
            var list = _accountService.GetAccounts(_appContext.ActiveCompanyId)
                .Select(x => new
                {
                    x.Id,
                    x.Code,
                    x.Name,
                    x.Type,
                    x.ParentAccountId
                })
                .ToList();

            _grid.DataSource = list;

            var parentItems = _accountService.GetAccounts(_appContext.ActiveCompanyId)
                .Select(x => new ComboItem<int?> { Value = x.Id, Text = x.Code + " - " + x.Name })
                .OrderBy(x => x.Text)
                .ToList();
            parentItems.Insert(0, new ComboItem<int?> { Value = null, Text = "(Yok)" });

            _cmbParent.DataSource = parentItems;
            _cmbParent.DisplayMember = "Text";
            _cmbParent.ValueMember = "Value";

            BuildTree();
        }

        private void BuildTree()
        {
            _tree.Nodes.Clear();
            var roots = _accountService.BuildTree(_appContext.ActiveCompanyId);

            foreach (var root in roots)
            {
                _tree.Nodes.Add(CreateTreeNode(root));
            }
            _tree.ExpandAll();
        }

        private TreeNode CreateTreeNode(AccountTreeNode node)
        {
            var treeNode = new TreeNode(node.Account.Code + " - " + node.Account.Name);
            foreach (var child in node.Children)
            {
                treeNode.Nodes.Add(CreateTreeNode(child));
            }

            return treeNode;
        }

        private void OnAdd(object sender, EventArgs e)
        {
            try
            {
                var account = new Account
                {
                    CompanyId = _appContext.ActiveCompanyId,
                    Code = _txtCode.Text.Trim(),
                    Name = _txtName.Text.Trim(),
                    Type = _cmbType.SelectedItem == null ? "Asset" : _cmbType.SelectedItem.ToString(),
                    ParentAccountId = GetSelectedParentId()
                };

                _accountService.AddAccount(account);
                LoadData();
                ClearInputs();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Hata", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void OnUpdate(object sender, EventArgs e)
        {
            try
            {
                if (!_selectedId.HasValue)
                {
                    MessageBox.Show("Güncellenecek hesap seçin.", "Bilgi", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    return;
                }

                var account = new Account
                {
                    Id = _selectedId.Value,
                    CompanyId = _appContext.ActiveCompanyId,
                    Code = _txtCode.Text.Trim(),
                    Name = _txtName.Text.Trim(),
                    Type = _cmbType.SelectedItem == null ? "Asset" : _cmbType.SelectedItem.ToString(),
                    ParentAccountId = GetSelectedParentId()
                };

                if (account.ParentAccountId.HasValue && account.ParentAccountId.Value == account.Id)
                {
                    MessageBox.Show("Hesap kendisinin üst hesabı olamaz.", "Uyarı", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                _accountService.UpdateAccount(account);
                LoadData();
                ClearInputs();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Hata", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void OnDelete(object sender, EventArgs e)
        {
            try
            {
                if (!_selectedId.HasValue)
                {
                    MessageBox.Show("Silinecek hesap seçin.", "Bilgi", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    return;
                }

                var confirm = MessageBox.Show("Seçili hesabı silmek istediğinize emin misiniz?", "Onay", MessageBoxButtons.YesNo, MessageBoxIcon.Question);
                if (confirm != DialogResult.Yes)
                {
                    return;
                }

                _accountService.DeleteAccount(_selectedId.Value);
                LoadData();
                ClearInputs();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Hata", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void OnClear(object sender, EventArgs e)
        {
            ClearInputs();
        }

        private void ClearInputs()
        {
            _selectedId = null;
            _txtCode.Text = string.Empty;
            _txtName.Text = string.Empty;
            if (_cmbType.Items.Count > 0)
            {
                _cmbType.SelectedIndex = 0;
            }

            if (_cmbParent.Items.Count > 0)
            {
                _cmbParent.SelectedIndex = 0;
            }
        }

        private int? GetSelectedParentId()
        {
            if (_cmbParent.SelectedItem == null)
            {
                return null;
            }

            var combo = _cmbParent.SelectedItem as ComboItem<int?>;
            return combo == null ? (int?)null : combo.Value;
        }

        private void OnGridSelectionChanged(object sender, EventArgs e)
        {
            if (_grid.SelectedRows.Count == 0)
            {
                return;
            }

            var row = _grid.SelectedRows[0];
            if (row == null || row.Cells["Id"] == null)
            {
                return;
            }

            int id;
            if (!int.TryParse(Convert.ToString(row.Cells["Id"].Value), out id))
            {
                return;
            }

            _selectedId = id;
            _txtCode.Text = Convert.ToString(row.Cells["Code"].Value);
            _txtName.Text = Convert.ToString(row.Cells["Name"].Value);

            var type = Convert.ToString(row.Cells["Type"].Value);
            if (!string.IsNullOrWhiteSpace(type) && _cmbType.Items.Contains(type))
            {
                _cmbType.SelectedItem = type;
            }

            var parentObj = row.Cells["ParentAccountId"].Value;
            int parentId;
            var hasParent = parentObj != null && int.TryParse(Convert.ToString(parentObj), out parentId);

            for (var i = 0; i < _cmbParent.Items.Count; i++)
            {
                var item = _cmbParent.Items[i] as ComboItem<int?>;
                if (item == null)
                {
                    continue;
                }

                if (!hasParent && !item.Value.HasValue)
                {
                    _cmbParent.SelectedIndex = i;
                    break;
                }

                if (hasParent && item.Value.HasValue && item.Value.Value == parentId)
                {
                    _cmbParent.SelectedIndex = i;
                    break;
                }
            }
        }
    }

    public class VoucherForm : Form
    {
        private readonly AppContext _appContext;
        private readonly IAccountingService _accountingService;
        private readonly IVoucherTypeRepository _voucherTypeRepository;
        private readonly IAccountRepository _accountRepository;

        private DateTimePicker _dtDate;
        private TextBox _txtDescription;
        private ComboBox _cmbVoucherType;
        private DataGridView _lineGrid;
        private DataGridView _voucherGrid;
        private Label _lblTotals;

        public VoucherForm(AppContext appContext)
        {
            _appContext = appContext;
            _accountingService = new AccountingService(new VoucherRepository(), new PeriodRepository());
            _voucherTypeRepository = new VoucherTypeRepository();
            _accountRepository = new AccountRepository();
            InitializeUi();
            LoadVoucherTypes();
            LoadVouchers();
            BuildLineGridColumns();
            AddLineRow();
        }

        private void InitializeUi()
        {
            Text = "Fiş Yönetimi";
            Width = 1200;
            Height = 760;
            StartPosition = FormStartPosition.CenterParent;

            var mainPanel = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 1,
                RowCount = 4
            };
            mainPanel.RowStyles.Add(new RowStyle(SizeType.Absolute, 100));
            mainPanel.RowStyles.Add(new RowStyle(SizeType.Percent, 45));
            mainPanel.RowStyles.Add(new RowStyle(SizeType.Absolute, 50));
            mainPanel.RowStyles.Add(new RowStyle(SizeType.Percent, 55));

            var headerPanel = new FlowLayoutPanel
            {
                Dock = DockStyle.Fill,
                FlowDirection = FlowDirection.LeftToRight,
                WrapContents = true
            };

            _dtDate = new DateTimePicker
            {
                Width = 160,
                Format = DateTimePickerFormat.Short,
                Value = DateTime.Now,
                Margin = new Padding(6)
            };

            _cmbVoucherType = UiHelper.CreateComboBox(200);
            _txtDescription = UiHelper.CreateTextBox(340);

            headerPanel.Controls.Add(UiHelper.CreateLabel("Tarih", 60));
            headerPanel.Controls.Add(_dtDate);
            headerPanel.Controls.Add(UiHelper.CreateLabel("Fiş Türü", 80));
            headerPanel.Controls.Add(_cmbVoucherType);
            headerPanel.Controls.Add(UiHelper.CreateLabel("Açıklama", 80));
            headerPanel.Controls.Add(_txtDescription);
            headerPanel.Controls.Add(UiHelper.CreateButton("Satır Ekle", OnAddLine));
            headerPanel.Controls.Add(UiHelper.CreateButton("Satır Sil", OnRemoveLine));
            headerPanel.Controls.Add(UiHelper.CreateButton("Kaydet", OnSaveVoucher));
            headerPanel.Controls.Add(UiHelper.CreateButton("Excel Export", OnExportVouchers));

            _lineGrid = new DataGridView
            {
                Dock = DockStyle.Fill,
                AllowUserToAddRows = false,
                AllowUserToDeleteRows = false,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                MultiSelect = false,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill
            };
            _lineGrid.CellValueChanged += OnLineGridChanged;
            _lineGrid.EditingControlShowing += OnLineGridEditingControlShowing;

            _lblTotals = new Label
            {
                Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleRight,
                Font = new Font(FontFamily.GenericSansSerif, 10, FontStyle.Bold),
                Padding = new Padding(10)
            };

            _voucherGrid = UiHelper.CreateReadOnlyGrid();
            _voucherGrid.SelectionChanged += OnVoucherSelectionChanged;

            mainPanel.Controls.Add(headerPanel, 0, 0);
            mainPanel.Controls.Add(_lineGrid, 0, 1);
            mainPanel.Controls.Add(_lblTotals, 0, 2);
            mainPanel.Controls.Add(_voucherGrid, 0, 3);

            Controls.Add(mainPanel);
            UpdateTotals();
        }

        private void BuildLineGridColumns()
        {
            _lineGrid.Columns.Clear();

            var accountColumn = new DataGridViewComboBoxColumn
            {
                Name = "AccountId",
                HeaderText = "Hesap",
                DataPropertyName = "AccountId",
                DisplayMember = "Text",
                ValueMember = "Value"
            };

            var accountItems = _accountRepository.GetByCompany(_appContext.ActiveCompanyId)
                .Select(a => new ComboItem<int>
                {
                    Value = a.Id,
                    Text = a.Code + " - " + a.Name
                })
                .OrderBy(x => x.Text)
                .ToList();

            accountColumn.DataSource = accountItems;

            _lineGrid.Columns.Add(accountColumn);
            _lineGrid.Columns.Add(new DataGridViewTextBoxColumn { Name = "Debit", HeaderText = "Borç" });
            _lineGrid.Columns.Add(new DataGridViewTextBoxColumn { Name = "Credit", HeaderText = "Alacak" });
            _lineGrid.Columns.Add(new DataGridViewTextBoxColumn { Name = "Description", HeaderText = "Satır Açıklama" });
        }

        private void LoadVoucherTypes()
        {
            var list = _voucherTypeRepository.GetAll()
                .Select(x => new ComboItem<int> { Value = x.Id, Text = x.Name })
                .ToList();

            _cmbVoucherType.DataSource = list;
            _cmbVoucherType.DisplayMember = "Text";
            _cmbVoucherType.ValueMember = "Value";
        }

        private void LoadVouchers()
        {
            var vouchers = _accountingService.GetVouchers(_appContext.ActiveCompanyId)
                .Select(x => new
                {
                    x.Id,
                    Date = x.Date.ToString("yyyy-MM-dd"),
                    x.Description,
                    x.VoucherTypeId,
                    x.PeriodId
                })
                .ToList();

            _voucherGrid.DataSource = vouchers;
        }

        private void OnAddLine(object sender, EventArgs e)
        {
            AddLineRow();
        }

        private void AddLineRow()
        {
            _lineGrid.Rows.Add();
            UpdateTotals();
        }

        private void OnRemoveLine(object sender, EventArgs e)
        {
            if (_lineGrid.SelectedRows.Count > 0)
            {
                _lineGrid.Rows.Remove(_lineGrid.SelectedRows[0]);
                UpdateTotals();
            }
        }

        private void OnSaveVoucher(object sender, EventArgs e)
        {
            try
            {
                var lines = ReadLineRequests();
                var typeItem = _cmbVoucherType.SelectedItem as ComboItem<int>;
                if (typeItem == null)
                {
                    MessageBox.Show("Fiş türü seçmelisiniz.", "Uyarı", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                var request = new VoucherCreateRequest
                {
                    CompanyId = _appContext.ActiveCompanyId,
                    PeriodId = _appContext.ActivePeriodId,
                    VoucherTypeId = typeItem.Value,
                    Date = _dtDate.Value.Date,
                    Description = _txtDescription.Text.Trim(),
                    UserId = _appContext.ActiveUserId,
                    Lines = lines
                };

                var id = _accountingService.CreateVoucher(request);
                MessageBox.Show("Fiş kaydedildi. Fiş No: " + id, "Bilgi", MessageBoxButtons.OK, MessageBoxIcon.Information);
                ClearVoucherInput();
                LoadVouchers();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Hata", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private List<VoucherLineCreateRequest> ReadLineRequests()
        {
            var lines = new List<VoucherLineCreateRequest>();
            foreach (DataGridViewRow row in _lineGrid.Rows)
            {
                if (row.IsNewRow)
                {
                    continue;
                }

                var accountObj = row.Cells["AccountId"].Value;
                if (accountObj == null)
                {
                    continue;
                }

                int accountId;
                if (!int.TryParse(Convert.ToString(accountObj), out accountId))
                {
                    continue;
                }

                var debit = UiHelper.ParseDecimal(Convert.ToString(row.Cells["Debit"].Value));
                var credit = UiHelper.ParseDecimal(Convert.ToString(row.Cells["Credit"].Value));
                var desc = Convert.ToString(row.Cells["Description"].Value);

                if (debit == 0m && credit == 0m)
                {
                    continue;
                }

                lines.Add(new VoucherLineCreateRequest
                {
                    AccountId = accountId,
                    Debit = debit,
                    Credit = credit,
                    Description = desc
                });
            }

            return lines;
        }

        private void ClearVoucherInput()
        {
            _dtDate.Value = DateTime.Now;
            _txtDescription.Text = string.Empty;
            _lineGrid.Rows.Clear();
            AddLineRow();
        }

        private void OnVoucherSelectionChanged(object sender, EventArgs e)
        {
            if (_voucherGrid.SelectedRows.Count == 0)
            {
                return;
            }

            var row = _voucherGrid.SelectedRows[0];
            if (row == null || row.Cells["Id"] == null)
            {
                return;
            }

            int voucherId;
            if (!int.TryParse(Convert.ToString(row.Cells["Id"].Value), out voucherId))
            {
                return;
            }

            var lines = _accountingService.GetVoucherLines(voucherId);
            var debit = lines.Sum(x => x.Debit);
            var credit = lines.Sum(x => x.Credit);

            _lblTotals.Text = string.Format("Seçili Fiş Toplam Borç: {0:N2} | Toplam Alacak: {1:N2}", debit, credit);
        }

        private void OnLineGridChanged(object sender, DataGridViewCellEventArgs e)
        {
            UpdateTotals();
        }

        private void OnLineGridEditingControlShowing(object sender, DataGridViewEditingControlShowingEventArgs e)
        {
            var textBox = e.Control as TextBox;
            if (textBox == null)
            {
                return;
            }

            textBox.KeyPress -= DecimalTextBox_KeyPress;
            var column = _lineGrid.CurrentCell == null ? null : _lineGrid.Columns[_lineGrid.CurrentCell.ColumnIndex];
            if (column != null && (column.Name == "Debit" || column.Name == "Credit"))
            {
                textBox.KeyPress += DecimalTextBox_KeyPress;
            }
        }

        private void DecimalTextBox_KeyPress(object sender, KeyPressEventArgs e)
        {
            if (char.IsControl(e.KeyChar))
            {
                return;
            }

            if (!char.IsDigit(e.KeyChar) && e.KeyChar != ',' && e.KeyChar != '.')
            {
                e.Handled = true;
            }
        }

        private void UpdateTotals()
        {
            var lines = ReadLineRequests();
            var debit = lines.Sum(x => x.Debit);
            var credit = lines.Sum(x => x.Credit);
            _lblTotals.Text = string.Format("Toplam Borç: {0:N2} | Toplam Alacak: {1:N2} | Fark: {2:N2}", debit, credit, debit - credit);
        }

        private void OnExportVouchers(object sender, EventArgs e)
        {
            ExportHelper.ExportGridToCsv(_voucherGrid, this, "Fisler");
        }
    }

    public class CustomerForm : Form
    {
        private readonly AppContext _appContext;
        private readonly ICustomerService _service;
        private DataGridView _grid;
        private TextBox _txtName;
        private TextBox _txtTax;
        private TextBox _txtPhone;
        private int? _selectedId;

        public CustomerForm(AppContext appContext)
        {
            _appContext = appContext;
            _service = new CustomerService(new CustomerRepository());
            InitializeUi();
            LoadData();
        }

        private void InitializeUi()
        {
            Text = "Müşteri Yönetimi";
            Width = 900;
            Height = 620;
            StartPosition = FormStartPosition.CenterParent;

            var panel = new Panel { Dock = DockStyle.Fill };
            var top = new FlowLayoutPanel
            {
                Dock = DockStyle.Top,
                Height = 90,
                WrapContents = true
            };

            _txtName = UiHelper.CreateTextBox(180);
            _txtTax = UiHelper.CreateTextBox(130);
            _txtPhone = UiHelper.CreateTextBox(130);

            top.Controls.Add(UiHelper.CreateLabel("Ad", 50));
            top.Controls.Add(_txtName);
            top.Controls.Add(UiHelper.CreateLabel("Vergi No", 70));
            top.Controls.Add(_txtTax);
            top.Controls.Add(UiHelper.CreateLabel("Telefon", 60));
            top.Controls.Add(_txtPhone);
            top.Controls.Add(UiHelper.CreateButton("Ekle", OnAdd));
            top.Controls.Add(UiHelper.CreateButton("Güncelle", OnUpdate));
            top.Controls.Add(UiHelper.CreateButton("Sil", OnDelete));
            top.Controls.Add(UiHelper.CreateButton("Temizle", OnClear));

            _grid = UiHelper.CreateReadOnlyGrid();
            _grid.SelectionChanged += OnSelectionChanged;

            panel.Controls.Add(_grid);
            panel.Controls.Add(top);

            Controls.Add(panel);
        }

        private void LoadData()
        {
            var data = _service.GetCustomers(_appContext.ActiveCompanyId)
                .Select(x => new { x.Id, x.Name, x.TaxNumber, x.Phone })
                .ToList();

            _grid.DataSource = data;
        }

        private void OnAdd(object sender, EventArgs e)
        {
            try
            {
                var customer = new Customer
                {
                    CompanyId = _appContext.ActiveCompanyId,
                    Name = _txtName.Text.Trim(),
                    TaxNumber = _txtTax.Text.Trim(),
                    Phone = _txtPhone.Text.Trim()
                };

                _service.AddCustomer(customer);
                LoadData();
                ClearInputs();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Hata", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void OnUpdate(object sender, EventArgs e)
        {
            try
            {
                if (!_selectedId.HasValue)
                {
                    MessageBox.Show("Güncellenecek müşteri seçin.", "Bilgi", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    return;
                }

                var customer = new Customer
                {
                    Id = _selectedId.Value,
                    CompanyId = _appContext.ActiveCompanyId,
                    Name = _txtName.Text.Trim(),
                    TaxNumber = _txtTax.Text.Trim(),
                    Phone = _txtPhone.Text.Trim()
                };

                _service.UpdateCustomer(customer);
                LoadData();
                ClearInputs();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Hata", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void OnDelete(object sender, EventArgs e)
        {
            try
            {
                if (!_selectedId.HasValue)
                {
                    MessageBox.Show("Silinecek müşteri seçin.", "Bilgi", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    return;
                }

                if (MessageBox.Show("Seçili müşteri silinsin mi?", "Onay", MessageBoxButtons.YesNo, MessageBoxIcon.Question) != DialogResult.Yes)
                {
                    return;
                }

                _service.DeleteCustomer(_selectedId.Value);
                LoadData();
                ClearInputs();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Hata", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void OnClear(object sender, EventArgs e)
        {
            ClearInputs();
        }

        private void OnSelectionChanged(object sender, EventArgs e)
        {
            if (_grid.SelectedRows.Count == 0)
            {
                return;
            }

            var row = _grid.SelectedRows[0];
            int id;
            if (!int.TryParse(Convert.ToString(row.Cells["Id"].Value), out id))
            {
                return;
            }

            _selectedId = id;
            _txtName.Text = Convert.ToString(row.Cells["Name"].Value);
            _txtTax.Text = Convert.ToString(row.Cells["TaxNumber"].Value);
            _txtPhone.Text = Convert.ToString(row.Cells["Phone"].Value);
        }

        private void ClearInputs()
        {
            _selectedId = null;
            _txtName.Text = string.Empty;
            _txtTax.Text = string.Empty;
            _txtPhone.Text = string.Empty;
        }
    }

    public class SupplierForm : Form
    {
        private readonly AppContext _appContext;
        private readonly ISupplierService _service;
        private DataGridView _grid;
        private TextBox _txtName;
        private TextBox _txtTax;
        private TextBox _txtPhone;
        private int? _selectedId;

        public SupplierForm(AppContext appContext)
        {
            _appContext = appContext;
            _service = new SupplierService(new SupplierRepository());
            InitializeUi();
            LoadData();
        }

        private void InitializeUi()
        {
            Text = "Tedarikçi Yönetimi";
            Width = 900;
            Height = 620;
            StartPosition = FormStartPosition.CenterParent;

            var panel = new Panel { Dock = DockStyle.Fill };
            var top = new FlowLayoutPanel
            {
                Dock = DockStyle.Top,
                Height = 90,
                WrapContents = true
            };

            _txtName = UiHelper.CreateTextBox(180);
            _txtTax = UiHelper.CreateTextBox(130);
            _txtPhone = UiHelper.CreateTextBox(130);

            top.Controls.Add(UiHelper.CreateLabel("Ad", 50));
            top.Controls.Add(_txtName);
            top.Controls.Add(UiHelper.CreateLabel("Vergi No", 70));
            top.Controls.Add(_txtTax);
            top.Controls.Add(UiHelper.CreateLabel("Telefon", 60));
            top.Controls.Add(_txtPhone);
            top.Controls.Add(UiHelper.CreateButton("Ekle", OnAdd));
            top.Controls.Add(UiHelper.CreateButton("Güncelle", OnUpdate));
            top.Controls.Add(UiHelper.CreateButton("Sil", OnDelete));
            top.Controls.Add(UiHelper.CreateButton("Temizle", OnClear));

            _grid = UiHelper.CreateReadOnlyGrid();
            _grid.SelectionChanged += OnSelectionChanged;

            panel.Controls.Add(_grid);
            panel.Controls.Add(top);
            Controls.Add(panel);
        }

        private void LoadData()
        {
            var data = _service.GetSuppliers(_appContext.ActiveCompanyId)
                .Select(x => new { x.Id, x.Name, x.TaxNumber, x.Phone })
                .ToList();
            _grid.DataSource = data;
        }

        private void OnAdd(object sender, EventArgs e)
        {
            try
            {
                var supplier = new Supplier
                {
                    CompanyId = _appContext.ActiveCompanyId,
                    Name = _txtName.Text.Trim(),
                    TaxNumber = _txtTax.Text.Trim(),
                    Phone = _txtPhone.Text.Trim()
                };

                _service.AddSupplier(supplier);
                LoadData();
                ClearInputs();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Hata", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void OnUpdate(object sender, EventArgs e)
        {
            try
            {
                if (!_selectedId.HasValue)
                {
                    MessageBox.Show("Güncellenecek tedarikçi seçin.", "Bilgi", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    return;
                }

                var supplier = new Supplier
                {
                    Id = _selectedId.Value,
                    CompanyId = _appContext.ActiveCompanyId,
                    Name = _txtName.Text.Trim(),
                    TaxNumber = _txtTax.Text.Trim(),
                    Phone = _txtPhone.Text.Trim()
                };

                _service.UpdateSupplier(supplier);
                LoadData();
                ClearInputs();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Hata", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void OnDelete(object sender, EventArgs e)
        {
            try
            {
                if (!_selectedId.HasValue)
                {
                    MessageBox.Show("Silinecek tedarikçi seçin.", "Bilgi", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    return;
                }

                if (MessageBox.Show("Seçili tedarikçi silinsin mi?", "Onay", MessageBoxButtons.YesNo, MessageBoxIcon.Question) != DialogResult.Yes)
                {
                    return;
                }

                _service.DeleteSupplier(_selectedId.Value);
                LoadData();
                ClearInputs();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Hata", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void OnClear(object sender, EventArgs e)
        {
            ClearInputs();
        }

        private void OnSelectionChanged(object sender, EventArgs e)
        {
            if (_grid.SelectedRows.Count == 0)
            {
                return;
            }

            var row = _grid.SelectedRows[0];
            int id;
            if (!int.TryParse(Convert.ToString(row.Cells["Id"].Value), out id))
            {
                return;
            }

            _selectedId = id;
            _txtName.Text = Convert.ToString(row.Cells["Name"].Value);
            _txtTax.Text = Convert.ToString(row.Cells["TaxNumber"].Value);
            _txtPhone.Text = Convert.ToString(row.Cells["Phone"].Value);
        }

        private void ClearInputs()
        {
            _selectedId = null;
            _txtName.Text = string.Empty;
            _txtTax.Text = string.Empty;
            _txtPhone.Text = string.Empty;
        }
    }

    public class ReportForm : Form
    {
        private readonly AppContext _appContext;
        private readonly IReportService _reportService;
        private readonly IAccountService _accountService;

        private TabControl _tabControl;
        private DataGridView _trialGrid;
        private DataGridView _ledgerGrid;
        private DataGridView _incomeGrid;
        private ComboBox _cmbLedgerAccount;

        public ReportForm(AppContext appContext)
        {
            _appContext = appContext;
            _reportService = new ReportService(new ReportRepository());
            _accountService = new AccountService(new AccountRepository());
            InitializeUi();
            LoadData();
        }

        private void InitializeUi()
        {
            Text = "Raporlar";
            Width = 1100;
            Height = 720;
            StartPosition = FormStartPosition.CenterParent;

            _tabControl = new TabControl { Dock = DockStyle.Fill };
            var tabTrial = new TabPage("Mizan");
            var tabLedger = new TabPage("Hesap Ekstresi");
            var tabIncome = new TabPage("Gelir Tablosu");

            _trialGrid = UiHelper.CreateReadOnlyGrid();
            tabTrial.Controls.Add(_trialGrid);

            var ledgerPanel = new Panel { Dock = DockStyle.Fill };
            var ledgerTop = new FlowLayoutPanel
            {
                Dock = DockStyle.Top,
                Height = 50,
                FlowDirection = FlowDirection.LeftToRight
            };
            _cmbLedgerAccount = UiHelper.CreateComboBox(260);
            ledgerTop.Controls.Add(UiHelper.CreateLabel("Hesap", 50));
            ledgerTop.Controls.Add(_cmbLedgerAccount);
            ledgerTop.Controls.Add(UiHelper.CreateButton("Getir", OnLoadLedger));
            ledgerTop.Controls.Add(UiHelper.CreateButton("Excel Export", OnExportActiveReport));

            _ledgerGrid = UiHelper.CreateReadOnlyGrid();
            ledgerPanel.Controls.Add(_ledgerGrid);
            ledgerPanel.Controls.Add(ledgerTop);
            tabLedger.Controls.Add(ledgerPanel);

            _incomeGrid = UiHelper.CreateReadOnlyGrid();
            tabIncome.Controls.Add(_incomeGrid);

            _tabControl.TabPages.Add(tabTrial);
            _tabControl.TabPages.Add(tabLedger);
            _tabControl.TabPages.Add(tabIncome);

            Controls.Add(_tabControl);
        }

        private void LoadData()
        {
            LoadTrialBalance();
            LoadLedgerAccounts();
            LoadIncomeStatement();
        }

        private void LoadTrialBalance()
        {
            var trial = _reportService.GetTrialBalance(_appContext.ActiveCompanyId)
                .Select(x => new
                {
                    HesapKodu = x.AccountCode,
                    HesapAdi = x.AccountName,
                    Borc = x.TotalDebit,
                    Alacak = x.TotalCredit
                })
                .ToList();

            _trialGrid.DataSource = trial;
        }

        private void LoadLedgerAccounts()
        {
            var accounts = _accountService.GetAccounts(_appContext.ActiveCompanyId)
                .Select(x => new ComboItem<int> { Value = x.Id, Text = x.Code + " - " + x.Name })
                .OrderBy(x => x.Text)
                .ToList();
            _cmbLedgerAccount.DataSource = accounts;
            _cmbLedgerAccount.DisplayMember = "Text";
            _cmbLedgerAccount.ValueMember = "Value";
        }

        private void OnLoadLedger(object sender, EventArgs e)
        {
            var selected = _cmbLedgerAccount.SelectedItem as ComboItem<int>;
            if (selected == null)
            {
                MessageBox.Show("Hesap seçiniz.", "Bilgi", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            var ledger = _reportService.GetLedger(_appContext.ActiveCompanyId, selected.Value)
                .Select(x => new
                {
                    Tarih = x.Date.ToString("yyyy-MM-dd"),
                    FisAciklama = x.VoucherDescription,
                    SatirAciklama = x.LineDescription,
                    Borc = x.Debit,
                    Alacak = x.Credit
                })
                .ToList();

            _ledgerGrid.DataSource = ledger;
        }

        private void LoadIncomeStatement()
        {
            var data = _reportService.GetIncomeStatement(_appContext.ActiveCompanyId)
                .Select(x => new
                {
                    Kategori = x.Category,
                    Tutar = x.Amount
                })
                .ToList();

            _incomeGrid.DataSource = data;
        }

        private void OnExportActiveReport(object sender, EventArgs e)
        {
            if (_tabControl.SelectedTab == null)
            {
                return;
            }

            if (_tabControl.SelectedTab.Text == "Mizan")
            {
                ExportHelper.ExportGridToCsv(_trialGrid, this, "Mizan");
            }
            else if (_tabControl.SelectedTab.Text == "Hesap Ekstresi")
            {
                ExportHelper.ExportGridToCsv(_ledgerGrid, this, "HesapEkstresi");
            }
            else
            {
                ExportHelper.ExportGridToCsv(_incomeGrid, this, "GelirTablosu");
            }
        }
    }

    public class CurrentAccountForm : Form
    {
        private readonly AppContext _appContext;
        private readonly ICurrentAccountService _currentService;
        private readonly ICustomerService _customerService;

        private DataGridView _balanceGrid;
        private DataGridView _txGrid;
        private ComboBox _cmbCustomer;
        private TextBox _txtAmount;
        private TextBox _txtReference;
        private TextBox _txtDescription;

        public CurrentAccountForm(AppContext appContext)
        {
            _appContext = appContext;
            _currentService = new CurrentAccountService(new CurrentAccountRepository());
            _customerService = new CustomerService(new CustomerRepository());
            InitializeUi();
            LoadCustomers();
            LoadBalances();
        }

        private void InitializeUi()
        {
            Text = "Cari Hesap Yönetimi";
            FormBorderStyle = FormBorderStyle.None;
            TopLevel = false;
            Dock = DockStyle.Fill;

            var main = new TableLayoutPanel { Dock = DockStyle.Fill, RowCount = 3, ColumnCount = 1 };
            main.RowStyles.Add(new RowStyle(SizeType.Absolute, 90));
            main.RowStyles.Add(new RowStyle(SizeType.Percent, 45));
            main.RowStyles.Add(new RowStyle(SizeType.Percent, 55));

            var top = new FlowLayoutPanel { Dock = DockStyle.Fill, WrapContents = true };
            _cmbCustomer = UiHelper.CreateComboBox(240);
            _txtAmount = UiHelper.CreateTextBox(100);
            _txtReference = UiHelper.CreateTextBox(120);
            _txtDescription = UiHelper.CreateTextBox(200);

            top.Controls.Add(UiHelper.CreateLabel("Müşteri", 55));
            top.Controls.Add(_cmbCustomer);
            top.Controls.Add(UiHelper.CreateLabel("Tahsilat", 55));
            top.Controls.Add(_txtAmount);
            top.Controls.Add(UiHelper.CreateLabel("Ref No", 45));
            top.Controls.Add(_txtReference);
            top.Controls.Add(UiHelper.CreateLabel("Açıklama", 60));
            top.Controls.Add(_txtDescription);
            top.Controls.Add(UiHelper.CreateButton("Tahsilat Kaydet", OnSaveCollection));
            top.Controls.Add(UiHelper.CreateButton("Excel Export", OnExportBalances));

            _balanceGrid = UiHelper.CreateReadOnlyGrid();
            _balanceGrid.SelectionChanged += OnBalanceSelected;
            _txGrid = UiHelper.CreateReadOnlyGrid();

            main.Controls.Add(top, 0, 0);
            main.Controls.Add(_balanceGrid, 0, 1);
            main.Controls.Add(_txGrid, 0, 2);
            Controls.Add(main);
        }

        private void LoadCustomers()
        {
            var customers = _customerService.GetCustomers(_appContext.ActiveCompanyId)
                .Select(x => new ComboItem<int> { Value = x.Id, Text = x.Name })
                .ToList();
            _cmbCustomer.DataSource = customers;
            _cmbCustomer.DisplayMember = "Text";
            _cmbCustomer.ValueMember = "Value";
        }

        private void LoadBalances()
        {
            var customers = _customerService.GetCustomers(_appContext.ActiveCompanyId).ToDictionary(x => x.Id, x => x.Name);
            var balances = _currentService.GetCurrentAccounts(_appContext.ActiveCompanyId)
                .Select(x => new
                {
                    x.CustomerId,
                    CustomerName = customers.ContainsKey(x.CustomerId) ? customers[x.CustomerId] : ("Müşteri #" + x.CustomerId),
                    Borc = x.Debit,
                    Alacak = x.Credit,
                    Bakiye = x.Balance
                })
                .OrderBy(x => x.CustomerName)
                .ToList();
            _balanceGrid.DataSource = balances;
        }

        private void OnSaveCollection(object sender, EventArgs e)
        {
            try
            {
                var customer = _cmbCustomer.SelectedItem as ComboItem<int>;
                if (customer == null)
                {
                    MessageBox.Show("Müşteri seçiniz.", "Bilgi", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    return;
                }

                var amount = UiHelper.ParseDecimal(_txtAmount.Text);
                _currentService.PostCollection(_appContext.ActiveCompanyId, customer.Value, amount, _txtReference.Text.Trim(), _txtDescription.Text.Trim());
                LoadBalances();
                LoadTransactions(customer.Value);
                _txtAmount.Text = string.Empty;
                _txtReference.Text = string.Empty;
                _txtDescription.Text = string.Empty;
                MessageBox.Show("Tahsilat kaydedildi, cari bakiye güncellendi.", "Başarılı", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Hata", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void OnBalanceSelected(object sender, EventArgs e)
        {
            if (_balanceGrid.SelectedRows.Count == 0)
            {
                return;
            }

            int customerId;
            if (!int.TryParse(Convert.ToString(_balanceGrid.SelectedRows[0].Cells["CustomerId"].Value), out customerId))
            {
                return;
            }

            LoadTransactions(customerId);
        }

        private void LoadTransactions(int customerId)
        {
            var txList = _currentService.GetTransactions(_appContext.ActiveCompanyId, customerId)
                .Select(x => new
                {
                    Tarih = x.Date.ToString("yyyy-MM-dd HH:mm"),
                    Islem = x.TransactionType,
                    Tutar = x.Amount,
                    Referans = x.ReferenceNo,
                    x.Description
                })
                .ToList();
            _txGrid.DataSource = txList;
        }

        private void OnExportBalances(object sender, EventArgs e)
        {
            ExportHelper.ExportGridToCsv(_balanceGrid, this, "CariBakiyeler");
        }
    }

    public class InvoiceForm : Form
    {
        private readonly AppContext _appContext;
        private readonly IInvoiceService _invoiceService;
        private readonly ICustomerService _customerService;

        private TextBox _txtInvoiceNo;
        private DateTimePicker _dtInvoiceDate;
        private ComboBox _cmbCustomer;
        private TextBox _txtDescription;
        private DataGridView _lineGrid;
        private DataGridView _invoiceGrid;
        private Label _lblTotal;

        public InvoiceForm(AppContext appContext)
        {
            _appContext = appContext;
            _invoiceService = new InvoiceService(new InvoiceRepository());
            _customerService = new CustomerService(new CustomerRepository());
            InitializeUi();
            LoadCustomers();
            LoadInvoices();
            BuildLineColumns();
            AddLine();
        }

        private void InitializeUi()
        {
            Text = "Fatura Yönetimi";
            Width = 1200;
            Height = 760;
            StartPosition = FormStartPosition.CenterParent;

            var main = new TableLayoutPanel { Dock = DockStyle.Fill, RowCount = 4, ColumnCount = 1 };
            main.RowStyles.Add(new RowStyle(SizeType.Absolute, 100));
            main.RowStyles.Add(new RowStyle(SizeType.Percent, 50));
            main.RowStyles.Add(new RowStyle(SizeType.Absolute, 50));
            main.RowStyles.Add(new RowStyle(SizeType.Percent, 50));

            var top = new FlowLayoutPanel { Dock = DockStyle.Fill, WrapContents = true };
            _txtInvoiceNo = UiHelper.CreateTextBox(130);
            _dtInvoiceDate = new DateTimePicker { Width = 140, Format = DateTimePickerFormat.Short, Value = DateTime.Now, Margin = new Padding(6) };
            _cmbCustomer = UiHelper.CreateComboBox(240);
            _txtDescription = UiHelper.CreateTextBox(260);

            top.Controls.Add(UiHelper.CreateLabel("Fatura No", 70));
            top.Controls.Add(_txtInvoiceNo);
            top.Controls.Add(UiHelper.CreateLabel("Tarih", 45));
            top.Controls.Add(_dtInvoiceDate);
            top.Controls.Add(UiHelper.CreateLabel("Müşteri", 55));
            top.Controls.Add(_cmbCustomer);
            top.Controls.Add(UiHelper.CreateLabel("Açıklama", 60));
            top.Controls.Add(_txtDescription);
            top.Controls.Add(UiHelper.CreateButton("Satır Ekle", OnAddLine));
            top.Controls.Add(UiHelper.CreateButton("Satır Sil", OnRemoveLine));
            top.Controls.Add(UiHelper.CreateButton("Kaydet", OnSaveInvoice));
            top.Controls.Add(UiHelper.CreateButton("Excel Export", OnExportInvoices));

            _lineGrid = new DataGridView
            {
                Dock = DockStyle.Fill,
                AllowUserToAddRows = false,
                AllowUserToDeleteRows = false,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                MultiSelect = false,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill
            };
            _lineGrid.CellValueChanged += OnLineChanged;
            _lineGrid.EditingControlShowing += OnLineEditingControlShowing;

            _lblTotal = new Label { Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleRight, Font = new Font(FontFamily.GenericSansSerif, 10, FontStyle.Bold), Padding = new Padding(8) };
            _invoiceGrid = UiHelper.CreateReadOnlyGrid();

            main.Controls.Add(top, 0, 0);
            main.Controls.Add(_lineGrid, 0, 1);
            main.Controls.Add(_lblTotal, 0, 2);
            main.Controls.Add(_invoiceGrid, 0, 3);
            Controls.Add(main);
            UpdateTotalLabel();
        }

        private void BuildLineColumns()
        {
            _lineGrid.Columns.Clear();
            _lineGrid.Columns.Add(new DataGridViewTextBoxColumn { Name = "ItemName", HeaderText = "Ürün/Hizmet" });
            _lineGrid.Columns.Add(new DataGridViewTextBoxColumn { Name = "Quantity", HeaderText = "Miktar" });
            _lineGrid.Columns.Add(new DataGridViewTextBoxColumn { Name = "UnitPrice", HeaderText = "Birim Fiyat" });
            _lineGrid.Columns.Add(new DataGridViewTextBoxColumn { Name = "LineTotal", HeaderText = "Tutar", ReadOnly = true });
        }

        private void LoadCustomers()
        {
            var customers = _customerService.GetCustomers(_appContext.ActiveCompanyId)
                .Select(x => new ComboItem<int> { Value = x.Id, Text = x.Name })
                .ToList();
            _cmbCustomer.DataSource = customers;
            _cmbCustomer.DisplayMember = "Text";
            _cmbCustomer.ValueMember = "Value";
        }

        private void LoadInvoices()
        {
            var invoices = _invoiceService.GetInvoices(_appContext.ActiveCompanyId)
                .Select(x => new
                {
                    x.Id,
                    x.InvoiceNumber,
                    Tarih = x.Date.ToString("yyyy-MM-dd"),
                    x.CustomerId,
                    Toplam = x.TotalAmount,
                    x.Description
                })
                .ToList();
            _invoiceGrid.DataSource = invoices;
        }

        private void OnAddLine(object sender, EventArgs e)
        {
            AddLine();
        }

        private void AddLine()
        {
            _lineGrid.Rows.Add();
            UpdateTotalLabel();
        }

        private void OnRemoveLine(object sender, EventArgs e)
        {
            if (_lineGrid.SelectedRows.Count > 0)
            {
                _lineGrid.Rows.Remove(_lineGrid.SelectedRows[0]);
                UpdateLineTotals();
            }
        }

        private void OnLineChanged(object sender, DataGridViewCellEventArgs e)
        {
            UpdateLineTotals();
        }

        private void OnLineEditingControlShowing(object sender, DataGridViewEditingControlShowingEventArgs e)
        {
            var box = e.Control as TextBox;
            if (box == null)
            {
                return;
            }

            box.KeyPress -= DecimalTextBox_KeyPress;
            var col = _lineGrid.CurrentCell == null ? null : _lineGrid.Columns[_lineGrid.CurrentCell.ColumnIndex];
            if (col != null && (col.Name == "Quantity" || col.Name == "UnitPrice"))
            {
                box.KeyPress += DecimalTextBox_KeyPress;
            }
        }

        private void DecimalTextBox_KeyPress(object sender, KeyPressEventArgs e)
        {
            if (char.IsControl(e.KeyChar))
            {
                return;
            }

            if (!char.IsDigit(e.KeyChar) && e.KeyChar != ',' && e.KeyChar != '.')
            {
                e.Handled = true;
            }
        }

        private void UpdateLineTotals()
        {
            foreach (DataGridViewRow row in _lineGrid.Rows)
            {
                if (row.IsNewRow)
                {
                    continue;
                }

                var qty = UiHelper.ParseDecimal(Convert.ToString(row.Cells["Quantity"].Value));
                var unit = UiHelper.ParseDecimal(Convert.ToString(row.Cells["UnitPrice"].Value));
                row.Cells["LineTotal"].Value = (qty * unit).ToString("N2");
            }

            UpdateTotalLabel();
        }

        private List<InvoiceLine> ReadLines()
        {
            var lines = new List<InvoiceLine>();
            foreach (DataGridViewRow row in _lineGrid.Rows)
            {
                if (row.IsNewRow)
                {
                    continue;
                }

                var itemName = Convert.ToString(row.Cells["ItemName"].Value);
                var qty = UiHelper.ParseDecimal(Convert.ToString(row.Cells["Quantity"].Value));
                var unit = UiHelper.ParseDecimal(Convert.ToString(row.Cells["UnitPrice"].Value));
                if (string.IsNullOrWhiteSpace(itemName) && qty == 0m && unit == 0m)
                {
                    continue;
                }

                lines.Add(new InvoiceLine
                {
                    ItemName = itemName,
                    Quantity = qty,
                    UnitPrice = unit,
                    LineTotal = qty * unit
                });
            }

            return lines;
        }

        private void UpdateTotalLabel()
        {
            var total = ReadLines().Sum(x => x.LineTotal);
            _lblTotal.Text = "Fatura Toplamı: " + total.ToString("N2");
        }

        private void OnSaveInvoice(object sender, EventArgs e)
        {
            try
            {
                var customer = _cmbCustomer.SelectedItem as ComboItem<int>;
                if (customer == null)
                {
                    MessageBox.Show("Müşteri seçiniz.", "Bilgi", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    return;
                }

                var invoice = new Invoice
                {
                    CompanyId = _appContext.ActiveCompanyId,
                    CustomerId = customer.Value,
                    InvoiceNumber = _txtInvoiceNo.Text.Trim(),
                    Date = _dtInvoiceDate.Value.Date,
                    Description = _txtDescription.Text.Trim()
                };

                var lines = ReadLines();
                var id = _invoiceService.CreateInvoice(invoice, lines);
                MessageBox.Show("Fatura kaydedildi. No: " + id, "Bilgi", MessageBoxButtons.OK, MessageBoxIcon.Information);
                ClearInput();
                LoadInvoices();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Hata", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void ClearInput()
        {
            _txtInvoiceNo.Text = string.Empty;
            _txtDescription.Text = string.Empty;
            _dtInvoiceDate.Value = DateTime.Now;
            _lineGrid.Rows.Clear();
            AddLine();
        }

        private void OnExportInvoices(object sender, EventArgs e)
        {
            ExportHelper.ExportGridToCsv(_invoiceGrid, this, "Faturalar");
        }
    }

    public class UserForm : Form
    {
        private readonly AppContext _appContext;
        private readonly IUserService _userService;

        private DataGridView _grid;
        private TextBox _txtUsername;
        private TextBox _txtPassword;
        private int? _selectedId;

        public UserForm(AppContext appContext)
        {
            _appContext = appContext;
            _userService = new UserService(new UserRepository());
            InitializeUi();
            LoadUsers();
        }

        private void InitializeUi()
        {
            Text = "Kullanıcı Yönetimi";
            Width = 840;
            Height = 560;
            StartPosition = FormStartPosition.CenterParent;

            var main = new Panel { Dock = DockStyle.Fill };
            var top = new FlowLayoutPanel
            {
                Dock = DockStyle.Top,
                Height = 80,
                FlowDirection = FlowDirection.LeftToRight
            };

            _txtUsername = UiHelper.CreateTextBox(180);
            _txtPassword = UiHelper.CreateTextBox(150);
            _txtPassword.PasswordChar = '*';

            top.Controls.Add(UiHelper.CreateLabel("Kullanıcı Adı", 85));
            top.Controls.Add(_txtUsername);
            top.Controls.Add(UiHelper.CreateLabel("Parola", 50));
            top.Controls.Add(_txtPassword);
            top.Controls.Add(UiHelper.CreateButton("Ekle", OnAdd));
            top.Controls.Add(UiHelper.CreateButton("Güncelle", OnUpdate));
            top.Controls.Add(UiHelper.CreateButton("Sil", OnDelete));
            top.Controls.Add(UiHelper.CreateButton("Temizle", OnClear));

            _grid = UiHelper.CreateReadOnlyGrid();
            _grid.SelectionChanged += OnSelectionChanged;

            var info = new Label
            {
                Dock = DockStyle.Bottom,
                Height = 35,
                Text = "Not: Güncellemede parola boş bırakılırsa mevcut parola korunur.",
                TextAlign = ContentAlignment.MiddleLeft,
                Padding = new Padding(8)
            };

            main.Controls.Add(_grid);
            main.Controls.Add(top);
            main.Controls.Add(info);
            Controls.Add(main);
        }

        private void LoadUsers()
        {
            var data = _userService.GetUsers()
                .Select(x => new
                {
                    x.Id,
                    x.Username
                })
                .ToList();
            _grid.DataSource = data;
        }

        private void OnAdd(object sender, EventArgs e)
        {
            try
            {
                _userService.AddUser(_txtUsername.Text.Trim(), _txtPassword.Text.Trim());
                LoadUsers();
                ClearInputs();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Hata", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void OnUpdate(object sender, EventArgs e)
        {
            try
            {
                if (!_selectedId.HasValue)
                {
                    MessageBox.Show("Güncellenecek kullanıcı seçin.", "Bilgi", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    return;
                }

                _userService.UpdateUser(_selectedId.Value, _txtUsername.Text.Trim(), _txtPassword.Text.Trim());
                LoadUsers();
                ClearInputs();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Hata", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void OnDelete(object sender, EventArgs e)
        {
            try
            {
                if (!_selectedId.HasValue)
                {
                    MessageBox.Show("Silinecek kullanıcı seçin.", "Bilgi", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    return;
                }

                if (_selectedId.Value == _appContext.ActiveUserId)
                {
                    MessageBox.Show("Aktif kullanıcı silinemez.", "Uyarı", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                if (MessageBox.Show("Seçili kullanıcı silinsin mi?", "Onay", MessageBoxButtons.YesNo, MessageBoxIcon.Question) != DialogResult.Yes)
                {
                    return;
                }

                _userService.DeleteUser(_selectedId.Value);
                LoadUsers();
                ClearInputs();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Hata", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void OnClear(object sender, EventArgs e)
        {
            ClearInputs();
        }

        private void OnSelectionChanged(object sender, EventArgs e)
        {
            if (_grid.SelectedRows.Count == 0)
            {
                return;
            }

            var row = _grid.SelectedRows[0];
            int id;
            if (!int.TryParse(Convert.ToString(row.Cells["Id"].Value), out id))
            {
                return;
            }

            _selectedId = id;
            _txtUsername.Text = Convert.ToString(row.Cells["Username"].Value);
            _txtPassword.Text = string.Empty;
        }

        private void ClearInputs()
        {
            _selectedId = null;
            _txtUsername.Text = string.Empty;
            _txtPassword.Text = string.Empty;
        }
    }

    public class ComboItem<T>
    {
        public T Value { get; set; }
        public string Text { get; set; }

        public override string ToString()
        {
            return Text;
        }
    }

    #endregion
}
