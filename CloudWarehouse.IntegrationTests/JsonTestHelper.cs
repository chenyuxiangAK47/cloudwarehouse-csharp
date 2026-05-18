using System.Net.Http.Json;
using System.Text.Json;

namespace CloudWarehouse.IntegrationTests;

internal static class JsonTestHelper
{
    public static readonly JsonSerializerOptions Options = new()
    {
        PropertyNameCaseInsensitive = true
    };

    public static async Task<T?> ReadApiAsync<T>(HttpResponseMessage response)
    {
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<T>(Options);
    }
}
