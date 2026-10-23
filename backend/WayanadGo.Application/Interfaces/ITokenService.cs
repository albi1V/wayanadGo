using WayanadGo.Application.Entities;

namespace WayanadGo.Application.Interfaces;

public interface ITokenService
{
    (string Token, DateTime ExpiresAtUtc) CreateToken(User user);
}
