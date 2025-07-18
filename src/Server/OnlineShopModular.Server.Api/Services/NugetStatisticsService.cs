using OnlineShopModular.Shared.Dtos.Statistics;

namespace OnlineShopModular.Server.Api.Services;

public partial class NugetStatisticsService
{
    [AutoInject] protected HttpClient httpClient = default!;
    [AutoInject] protected JsonSerializerOptions jsonSerializerOptions = default!;

    public virtual async ValueTask<NugetStatsDto> GetPackageStats(string packageId, CancellationToken cancellationToken)
    {
        var url = $"/query?q=packageid:{packageId}";

        var response = await httpClient.GetFromJsonAsync(url, jsonSerializerOptions.GetTypeInfo<NugetStatsDto>(), cancellationToken)
                                ?? throw new ResourceNotFoundException();

        return response;
    }
}
