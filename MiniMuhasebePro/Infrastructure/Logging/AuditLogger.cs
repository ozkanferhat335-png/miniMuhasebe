using System;
using System.Diagnostics;

namespace MiniMuhasebePro.Infrastructure.Logging
{
    public class AuditLogger
    {
        public void Log(string user, string action, string entity, string detailMasked)
        {
            Debug.WriteLine($"[{DateTime.UtcNow:O}] user={user}, action={action}, entity={entity}, detail={detailMasked}");
        }

        public string MaskIban(string iban)
        {
            if (string.IsNullOrWhiteSpace(iban) || iban.Length < 8) return "****";
            return iban.Substring(0, 4) + new string('*', iban.Length - 8) + iban.Substring(iban.Length - 4);
        }
    }
}
