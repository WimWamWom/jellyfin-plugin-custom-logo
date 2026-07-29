using System;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;

namespace Jellyfin.Plugin.CustomLogo.Injection;

/// <summary>
/// Places <see cref="CustomLogoMiddleware"/> at the very front of the request pipeline.
/// </summary>
/// <remarks>
/// ASP.NET Core resolves every registered <see cref="IStartupFilter"/> from the application's
/// service provider and applies it around <c>Startup.Configure</c>. Because Jellyfin registers
/// plugin services into that same container before the host is built, a plugin can use this to add
/// middleware without the server exposing an explicit hook for it, and without patching any files.
/// </remarks>
internal sealed class CustomLogoStartupFilter : IStartupFilter
{
    /// <inheritdoc />
    public Action<IApplicationBuilder> Configure(Action<IApplicationBuilder> next)
    {
        return app =>
        {
            app.UseMiddleware<CustomLogoMiddleware>();
            next(app);
        };
    }
}
