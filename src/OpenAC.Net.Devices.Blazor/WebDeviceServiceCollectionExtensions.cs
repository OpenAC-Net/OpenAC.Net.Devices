using Microsoft.Extensions.DependencyInjection;

namespace OpenAC.Net.Devices.Blazor;

public static class WebDeviceServiceCollectionExtensions
{
    /// <summary>Registra o serviço por circuito no Server e por aplicação no WebAssembly.</summary>
    public static IServiceCollection AddWebDevices(this IServiceCollection services)
    {
        services.AddScoped<WebDeviceService>();
        return services;
    }
}
