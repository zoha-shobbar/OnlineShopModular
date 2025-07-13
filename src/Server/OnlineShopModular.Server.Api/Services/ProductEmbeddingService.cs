using OnlineShopModular.Server.Api.Models.Products;

namespace OnlineShopModular.Server.Api.Services;

/// <summary>
/// This class stores vectorized products and provides methods to query/manage them.
/// </summary>
public partial class ProductEmbeddingService
{
    private const float SIMILARITY_THRESHOLD = 0.85f;

    [AutoInject] private AppDbContext dbContext = default!;
    [AutoInject] private IWebHostEnvironment env = default!;
    [AutoInject] private IServiceProvider serviceProvider = default!;

    public async Task<IQueryable<Product>> GetProductsBySearchQuery(string searchQuery, CancellationToken cancellationToken)
    {
        var embeddedUserQuery = await EmbedText(searchQuery, cancellationToken);
        if (embeddedUserQuery is null)
            return dbContext.Products.Where(p => p.Name!.Contains(searchQuery) || p.Category!.Name!.Contains(searchQuery));
        var value = embeddedUserQuery.Value.ToArray();
        return dbContext.Products
            .Where(p => p.Embedding != null && EF.Functions.VectorDistance("cosine", p.Embedding, value!) < SIMILARITY_THRESHOLD).OrderBy(p => EF.Functions.VectorDistance("cosine", p.Embedding!, value!));
    }

    public async Task Embed(Product product, CancellationToken cancellationToken)
    {
        await dbContext.Entry(product).Reference(p => p.Category).LoadAsync(cancellationToken);

        // TODO: Needs to be improved.
        var embedding = await EmbedText($"Name: {product.Name}, Manufacture: {product.Category!.Name}, Description: {product.DescriptionText}, Price: {product.Price}", cancellationToken);

        if (embedding.HasValue)
        {
            product.Embedding = embedding.Value.ToArray();
        }
    }

    private async Task<ReadOnlyMemory<float>?> EmbedText(string input, CancellationToken cancellationToken)
    {
        if (AppDbContext.IsEmbeddingEnabled is false)
            return null;
        var embeddingGenerator = serviceProvider.GetService<IEmbeddingGenerator<string, Embedding<float>>>();
        if (embeddingGenerator is null)
            return env.IsDevelopment() ? null : throw new InvalidOperationException("Embedding generator is not registered.");
        var embedding = await embeddingGenerator.GenerateVectorAsync(input, options: new() { }, cancellationToken);
        return embedding.ToArray();
    }
}
