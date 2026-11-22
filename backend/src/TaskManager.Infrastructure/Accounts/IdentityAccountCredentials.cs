using Microsoft.AspNetCore.Identity;
using TaskManager.Application.Accounts;

namespace TaskManager.Infrastructure.Accounts;

internal sealed class IdentityAccountCredentials(UserManager<IdentityUser<Guid>> users) : IAccountCredentials
{
    public async Task<Guid?> VerifyAsync(string email, string password, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var user = await users.FindByNameAsync(email);
        if (user is null)
        {
            return null;
        }

        return await users.CheckPasswordAsync(user, password) ? user.Id : null;
    }
}
