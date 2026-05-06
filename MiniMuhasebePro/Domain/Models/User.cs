using MiniMuhasebePro.Domain.Enums;

namespace MiniMuhasebePro.Domain.Models
{
    public class User
    {
        public int Id { get; set; }
        public string Username { get; set; }
        public string PasswordHash { get; set; }
        public UserRole Role { get; set; }
        public bool IsActive { get; set; }
        public int FailedAttempts { get; set; }
    }
}
