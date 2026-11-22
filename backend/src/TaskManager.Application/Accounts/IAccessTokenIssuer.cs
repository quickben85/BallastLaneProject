namespace TaskManager.Application.Accounts;

public interface IAccessTokenIssuer
{
    IssuedAccessToken Issue(Guid userId);
}
