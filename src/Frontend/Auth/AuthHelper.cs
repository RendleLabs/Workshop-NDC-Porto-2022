namespace Frontend.Auth;

public class AuthHelper
{
    private readonly HttpClient _http;

    public AuthHelper(HttpClient http)
    {
        _http = http;
    }

    public async Task<string> GetTokenAsync()
    {
        var token = await _http.GetStringAsync("/generateJwt?name=frontend");
        return token;
    }
}