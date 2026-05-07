using System;
using System.Collections.Generic;
using MiniMuhasebePro.Domain.Models;

namespace MiniMuhasebePro.Services.Interfaces
{
    public interface IBankIntegrationService
    {
        List<BankTransaction> FetchTransactions(int accountId, DateTime startDate, DateTime endDate);
        string SubmitTransfer(string iban, decimal amount, string currency, string description);
    }
}
