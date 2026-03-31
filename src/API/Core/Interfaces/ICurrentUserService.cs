namespace API.Core.Interfaces;
using API.Core.Models;

public interface ICurrentUserService
{
    string? UserId { get; }
    string? Email { get; }
    string? DisplayName { get; }
    UserRole? Role { get; }
    bool IsAuthenticated { get; }
    bool IsAdmin { get; }
    bool IsOperator { get; }
    bool IsViewer { get; }
}
