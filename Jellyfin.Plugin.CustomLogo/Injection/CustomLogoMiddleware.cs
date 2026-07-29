using System;
using System.Text;
using System.Threading.Tasks;
using MediaBrowser.Common.Net;
using MediaBrowser.Controller.Configuration;
using Microsoft.AspNetCore.Http;

namespace Jellyfin.Plugin.CustomLogo.Injection;

/// <summary>
/// Serves a branded <c>index.html</c> in place of the web client's own copy.
/// </summary>
/// <remarks>
/// <para>
/// This middleware is registered ahead of Jellyfin's entire pipeline (see
/// <see cref="CustomLogoStartupFilter"/>), so it answers before the static file middleware ever
/// touches the request. The branded markup is built in memory; nothing on disk is modified, which
/// means the plugin survives web client updates and works on read-only container images.
/// </para>
/// <para>
/// Requests for <c>/web</c> without a trailing slash are deliberately left alone so that the
/// server's default-files middleware can issue its usual redirect. Answering there would break the
/// relative asset URLs inside <c>index.html</c>.
/// </para>
/// </remarks>
internal sealed class CustomLogoMiddleware
{
    private readonly RequestDelegate _next;

    /// <summary>
    /// Initializes a new instance of the <see cref="CustomLogoMiddleware"/> class.
    /// </summary>
    /// <param name="next">The next delegate in the pipeline.</param>
    public CustomLogoMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    /// <summary>
    /// Executes the middleware.
    /// </summary>
    /// <param name="context">The request context.</param>
    /// <param name="transformer">The index transformer.</param>
    /// <param name="serverConfigurationManager">The server configuration manager.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    public async Task InvokeAsync(
        HttpContext context,
        IndexHtmlTransformer transformer,
        IServerConfigurationManager serverConfigurationManager)
    {
        if (!IsIndexRequest(context, serverConfigurationManager))
        {
            await _next(context).ConfigureAwait(false);
            return;
        }

        var html = transformer.GetTransformedHtml();
        if (html is null)
        {
            // Nothing to inject, or the file was unreadable: let Jellyfin serve it normally.
            await _next(context).ConfigureAwait(false);
            return;
        }

        var response = context.Response;
        response.StatusCode = StatusCodes.Status200OK;
        response.ContentType = "text/html;charset=utf-8";

        // Matches the server's own handling of index.html.
        response.Headers.CacheControl = "no-cache";

        await response.WriteAsync(html, Encoding.UTF8, context.RequestAborted).ConfigureAwait(false);
    }

    private static bool IsIndexRequest(HttpContext context, IServerConfigurationManager serverConfigurationManager)
    {
        var request = context.Request;
        if (!HttpMethods.IsGet(request.Method) && !HttpMethods.IsHead(request.Method))
        {
            return false;
        }

        var path = request.Path.Value;
        if (string.IsNullOrEmpty(path))
        {
            return false;
        }

        // Normalized by the server to either be empty or to start with '/' and have no trailing slash.
        var baseUrl = serverConfigurationManager.GetNetworkConfiguration().BaseUrl ?? string.Empty;

        return string.Equals(path, baseUrl + "/web/", StringComparison.OrdinalIgnoreCase)
            || string.Equals(path, baseUrl + "/web/index.html", StringComparison.OrdinalIgnoreCase);
    }
}
