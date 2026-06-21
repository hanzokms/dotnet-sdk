using KMS.Sdk.Api;
using KMS.Sdk.Model;

namespace KMS.Sdk.Client;


public class UniversalAuth
{

  public UniversalAuth(ApiClient apiClient, Action<string> setAccessTokenFunc)
  {
    _apiClient = apiClient;
    _setAccessTokenFunc = setAccessTokenFunc;
  }

  public async Task<MachineIdentityCredential> LoginAsync(string clientId, string clientSecret)
  {
    try
    {
      var loginRequest = new UniversalAuthLoginRequest(clientId, clientSecret);

      var response = await _apiClient.PostAsync<UniversalAuthLoginRequest, MachineIdentityCredential>("/api/v1/auth/universal-auth/login", loginRequest).ConfigureAwait(false);
      _setAccessTokenFunc(response.AccessToken);
      return response;
    }
    catch (Exception e)
    {
      throw new KMSException("Failed to login", e);
    }
  }

  private readonly ApiClient _apiClient;
  private readonly Action<string> _setAccessTokenFunc;
}

public class LdapAuth
{

  public LdapAuth(ApiClient apiClient, Action<string> setAccessTokenFunc)
  {
    _apiClient = apiClient;
    _setAccessTokenFunc = setAccessTokenFunc;
  }

  public async Task<MachineIdentityCredential> LoginAsync(string identityId, string username, string password)
  {
    try
    {
      var loginRequest = new LdapAuthLoginRequest(identityId, username, password);

      var response = await _apiClient.PostAsync<LdapAuthLoginRequest, MachineIdentityCredential>("/api/v1/auth/ldap-auth/login", loginRequest).ConfigureAwait(false);
      _setAccessTokenFunc(response.AccessToken);
      return response;
    }
    catch (Exception e)
    {
      throw new KMSException("Failed to login", e);
    }
  }

  private readonly ApiClient _apiClient;
  private readonly Action<string> _setAccessTokenFunc;
}



public class AuthClient
{
  private readonly ApiClient _apiClient;
  UniversalAuth _universalAuth;
  LdapAuth _ldapAuth;
  private readonly Action<string> _setAccessTokenFunc;

  public AuthClient(ApiClient apiClient, Action<string> setAccessTokenFunc)
  {
    _apiClient = apiClient;
    _setAccessTokenFunc = setAccessTokenFunc;
    _universalAuth = new UniversalAuth(_apiClient, _setAccessTokenFunc);
    _ldapAuth = new LdapAuth(_apiClient, _setAccessTokenFunc);
  }

  public UniversalAuth UniversalAuth()
  {
    return _universalAuth;
  }

  public LdapAuth LdapAuth()
  {
    return _ldapAuth;
  }
}
