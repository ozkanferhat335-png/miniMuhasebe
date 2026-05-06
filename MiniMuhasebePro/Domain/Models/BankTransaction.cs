using System;

namespace MiniMuhasebePro.Domain.Models
{
    public class BankTransaction
    {
        public int Id { get; set; }
        public int AccountId { get; set; }
        public DateTime TrxDate { get; set; }
        public decimal Amount { get; set; }
        public string Currency { get; set; }
        public string Description { get; set; }
        public string ReferenceNo { get; set; }
        public string UniqueHash { get; set; }
        public bool IsMatched { get; set; }
    }
}
