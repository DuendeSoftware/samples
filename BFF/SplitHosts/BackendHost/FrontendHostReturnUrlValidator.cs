using Duende.Bff;

namespace BackendHost;

class FrontendHostReturnUrlValidator : IReturnUrlValidator
{
    public Task<bool> IsValidAsync(string returnUrl)
    {
        var uri = new Uri(returnUrl);
        return Task.FromResult(uri.Host == "localhost" && uri.Port == 5011);
    }
}