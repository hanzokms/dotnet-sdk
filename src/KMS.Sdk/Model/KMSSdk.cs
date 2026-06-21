namespace KMS.Sdk.Model
{

  public enum KMSAuthMethod
  {
    Universal,
    Token,
  }

  public class KMSUniversalAuth
  {

    public KMSUniversalAuth(string clientId, string clientSecret)
    {
      _clientId = clientId;
      _clientSecret = clientSecret;
    }

    internal string _clientId;
    internal string _clientSecret;
  }

  public class KMSTokenAuth
  {
    public KMSTokenAuth(string token)
    {
      _token = token;
    }

    internal string _token;
  }

  public class KMSAuth
  {

    private KMSUniversalAuth? _universalAuth;
    private KMSTokenAuth? _tokenAuth;
    private KMSAuthMethod _authMethod;

    public KMSAuth(KMSUniversalAuth universalAuth)
    {
      _universalAuth = universalAuth;
      _authMethod = KMSAuthMethod.Universal;
    }

    public KMSAuth(KMSTokenAuth tokenAuth)
    {
      _tokenAuth = tokenAuth;
      _authMethod = KMSAuthMethod.Token;
    }

    internal KMSAuthMethod GetAuthMethod()
    {
      return _authMethod;
    }

    internal KMSUniversalAuth GetUniversalAuth()
    {
      if (_authMethod != KMSAuthMethod.Universal)
      {
        throw new Exception($"Unable to get universal auth details. Auth method is set to {_authMethod}");
      }

      if (_universalAuth == null)
      {
        throw new Exception("Universal auth details are not set");
      }

      return _universalAuth;
    }

    internal KMSTokenAuth GetTokenAuth()
    {
      if (_authMethod != KMSAuthMethod.Token)
      {
        throw new Exception($"Unable to get token auth details. Auth method is set to {_authMethod}");
      }

      if (_tokenAuth == null)
      {
        throw new Exception("Token auth details are not set");
      }

      return _tokenAuth;
    }

  }

  public class KMSSdkSettings
  {
    public string HostUri { get; internal set; } = "https://kms.hanzo.ai";

    internal KMSSdkSettings() { }
  }

  public class KMSSdkSettingsBuilder
  {
    private KMSSdkSettings _settings = new KMSSdkSettings();

    public KMSSdkSettingsBuilder WithHostUri(string hostUri)
    {
      _settings.HostUri = hostUri;
      return this;
    }

    public KMSSdkSettings Build()
    {
      // we return a new class to make it immutable
      return new KMSSdkSettings
      {
        HostUri = _settings.HostUri
      };
    }
  }
}