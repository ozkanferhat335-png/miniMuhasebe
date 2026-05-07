using System;
using System.Collections.Generic;
using MiniMuhasebePro.Domain.Models;

namespace MiniMuhasebePro.Banking
{
    public interface IBankAdapter
    {
        string BankName { get; }
        List<BankTransaction> GetTransactions(string iban, DateTime startDate, DateTime endDate);
        string SendTransfer(string iban, decimal amount, string currency, string description, string idempotencyKey);
        decimal GetExchangeRate(string fromCurrency, string toCurrency);
    }
}
