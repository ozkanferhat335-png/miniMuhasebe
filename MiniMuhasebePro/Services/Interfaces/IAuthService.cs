using MiniMuhasebePro.Domain.Models;

namespace MiniMuhasebePro.Services.Interfaces
{
    public interface IAuthService
    {
        User Login(string username, string password);
        bool HasPermission(User user, string permission);
    }
}
