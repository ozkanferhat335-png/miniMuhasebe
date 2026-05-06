using System;
using System.Collections.Generic;
using MiniMuhasebePro.Domain.Models;

namespace MiniMuhasebePro.Banking
{
    public class MockBankAdapter : IBankAdapter
    {
        public string BankName => "MockBank";

        public List<BankTransaction> GetTransactions(string iban, DateTime startDate, DateTime endDate)
        {
            return new List<BankTransaction>
            {
                new BankTransaction{ AccountId=1, TrxDate=DateTime.Today, Amount=1000, Currency="TRY", Description="Tahsilat", ReferenceNo="REF1", UniqueHash="HASH1" }
            };
        }

        public string SendTransfer(string iban, decimal amount, string currency, string description, string idempotencyKey) => "BNKREF-001";
        public decimal GetExchangeRate(string fromCurrency, string toCurrency) => 38.45m;
    }
}
