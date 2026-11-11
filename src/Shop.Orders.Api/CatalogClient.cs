namespace Shop.Orders.Api;

public sealed record CatalogProduct(int Id, string Name, decimal Price);

/// <summary>Вызов Catalog API по внутренней сети (в Azure — через VNet / Kubernetes Service).</summary>
public sealed class CatalogClient(HttpClient httpClient)
{
    public async Task<CatalogProduct?> GetProductAsync(int productId, CancellationToken cancellationToken)
    {
        using var response = await httpClient.GetAsync($"api/products/{productId}", cancellationToken);

        if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
        {
            return null;
        }

        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<CatalogProduct>(cancellationToken);
    }
}
