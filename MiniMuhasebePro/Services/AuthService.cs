using System;
using System.Collections.Generic;
using MiniMuhasebePro.Domain.Enums;
using MiniMuhasebePro.Domain.Models;
using MiniMuhasebePro.Infrastructure.Security;
using MiniMuhasebePro.Services.Interfaces;

namespace MiniMuhasebePro.Services
{
    public class AuthService : IAuthService
    {
        private static readonly Dictionary<UserRole, List<string>> RolePermissions = new Dictionary<UserRole, List<string>>
        {
            { UserRole.Admin, new List<string> {"ALL"} },
            { UserRole.Muhasebe, new List<string> {"VOUCHER_VIEW", "VOUCHER_EDIT", "RECON"} },
            { UserRole.Finans, new List<string> {"TRANSFER_CREATE", "TRANSFER_APPROVE", "BANK_SYNC"} },
            { UserRole.Izleyici, new List<string> {"REPORT_VIEW"} }
        };

        public User Login(string username, string password)
        {
            if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(password)) return null;
            var sampleUser = new User { Id = 1, Username = "admin", PasswordHash = PasswordHasher.Hash("admin123"), Role = UserRole.Admin, IsActive = true };
            if (!sampleUser.IsActive) return null;
            return sampleUser.Username.Equals(username, StringComparison.OrdinalIgnoreCase) && PasswordHasher.Verify(password, sampleUser.PasswordHash)
                ? sampleUser
                : null;
        }

        public bool HasPermission(User user, string permission)
        {
            if (user == null) return false;
            var perms = RolePermissions[user.Role];
            return perms.Contains("ALL") || perms.Contains(permission);
        }
    }
}
