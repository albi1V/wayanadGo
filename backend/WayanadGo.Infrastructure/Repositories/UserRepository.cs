using Microsoft.EntityFrameworkCore;
using WayanadGo.Application.Interfaces;
using WayanadGo.Infrastructure.Data;
using WayanadGo.Application.Entities;

namespace WayanadGo.Infrastructure.Repositories;

public sealed class UserRepository(AppDbContext db) : IUserRepository
{
    public Task<User?> GetByEmailAsync(string email, CancellationToken ct) =>
        db.Users.FirstOrDefaultAsync(x => x.Email == email, ct);

    public async Task<User> AddAsync(User user, CancellationToken ct)
    {
        db.Users.Add(user);
        await db.SaveChangesAsync(ct);
        return user;
    }
}
