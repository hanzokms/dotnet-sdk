using System.Threading.Tasks;
using KMS.Sdk.Api;
using KMS.Sdk.Client;
using KMS.Sdk.Model;

namespace KMS.Sdk
{
  public class KMSClient
  {
    internal ApiClient _apiClient;
    private AuthClient _authClient;
    private SecretsClient _secretsClient;
    private PkiClient _pkiClient;
    public KMSClient(KMSSdkSettings settings)
    {
      _apiClient = new ApiClient(settings.HostUri);
      _secretsClient = new SecretsClient(_apiClient);
      _authClient = new AuthClient(_apiClient, (accessToken) => _apiClient.SetAccessToken(accessToken));
      _pkiClient = new PkiClient(_apiClient);
    }

    public AuthClient Auth()
    {
      return _authClient;
    }

    public SecretsClient Secrets()
    {
      return _secretsClient;
    }

    public PkiClient Pki()
    {
      return _pkiClient;
    }
  }
}