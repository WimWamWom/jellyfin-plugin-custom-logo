using System;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using MediaBrowser.Common.Net;
using MediaBrowser.Controller.Configuration;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.Extensions.Logging;
using Microsoft.Net.Http.Headers;

namespace Jellyfin.Plugin.CustomLogo.Injection;

/// <summary>
/// Rewrites the web client's <c>index.html</c> on its way to the browser.
/// </summary>
/// <remarks>
/// <para>
/// Registered ahead of Jellyfin's pipeline (see <see cref="CustomLogoStartupFilter"/>), this
/// middleware buffers the response for <c>index.html</c>, lets the rest of the pipeline produce it,
/// and only then applies the branding. It deliberately does <b>not</b> short-circuit and serve its
/// own copy: other plugins modify the same file further down. File Transformation, which the Media
/// Bar and Home Screen Sections plugins depend on, swaps the static file provider so that
/// <c>index.html</c> is rewritten as it is read. Answering early would throw all of that away.
/// </para>
/// <para>
/// Requests for <c>/web</c> without a trailing slash are left alone so the server's default-files
/// middleware can issue its usual redirect; answering there would break the relative asset URLs
/// inside <c>index.html</c>.
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
    /// <param name="logger">The logger.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    public async Task InvokeAsync(
        HttpContext context,
        IndexHtmlTransformer transformer,
        IServerConfigurationManager serverConfigurationManager,
        ILogger<CustomLogoMiddleware> logger)
    {
        if (!IsIndexRequest(context, serverConfigurationManager) || !transformer.IsEnabled())
        {
            await _next(context).ConfigureAwait(false);
            return;
        }

        var request = context.Request;

        // Ask the rest of the pipeline for a plain, complete, uncompressed 200 so the body can be
        // rewritten: compression happens downstream of here, and a 304 would carry no body at all.
        request.Headers.Remove(HeaderNames.AcceptEncoding);
        request.Headers.Remove(HeaderNames.IfNoneMatch);
        request.Headers.Remove(HeaderNames.IfModifiedSince);
        request.Headers.Remove(HeaderNames.Range);
        request.Headers.Remove(HeaderNames.IfRange);

        var originalFeature = context.Features.Get<IHttpResponseBodyFeature>();

        using var buffer = new MemoryStream();

        // Replacing the whole feature rather than just Response.Body matters: the static file
        // middleware serves physical files with SendFileAsync, which bypasses Response.Body.
        context.Features.Set<IHttpResponseBodyFeature>(new StreamResponseBodyFeature(buffer));

        try
        {
            await _next(context).ConfigureAwait(false);
        }
        finally
        {
            context.Features.Set<IHttpResponseBodyFeature>(originalFeature);
        }

        var response = context.Response;
        var outputStream = originalFeature is null ? response.Body : originalFeature.Stream;

        if (response.StatusCode != StatusCodes.Status200OK || buffer.Length == 0)
        {
            await WriteBufferAsync(context, buffer, outputStream).ConfigureAwait(false);
            return;
        }

        string? transformed;
        try
        {
            var html = Encoding.UTF8.GetString(buffer.GetBuffer(), 0, (int)buffer.Length);
            transformed = transformer.Transform(html);
        }
#pragma warning disable CA1031 // Do not catch general exception types
        catch (Exception ex)
#pragma warning restore CA1031
        {
            // Branding is cosmetic. Whatever goes wrong in here, the web client still has to load,
            // so fall back to the page exactly as the rest of the pipeline produced it.
            logger.LogError(ex, "Failed to apply custom branding to the web client index");
            transformed = null;
        }

        if (transformed is null)
        {
            await WriteBufferAsync(context, buffer, outputStream).ConfigureAwait(false);
            return;
        }

        var bytes = Encoding.UTF8.GetBytes(transformed);

        // The body no longer matches the file on disk, so any validator the static file middleware
        // produced is now wrong.
        response.Headers.Remove(HeaderNames.ETag);
        response.Headers.Remove(HeaderNames.LastModified);
        response.Headers.CacheControl = "no-cache";
        response.ContentType = "text/html;charset=utf-8";
        response.ContentLength = bytes.Length;

        await outputStream.WriteAsync(bytes, context.RequestAborted).ConfigureAwait(false);
    }

    private static async Task WriteBufferAsync(HttpContext context, MemoryStream buffer, Stream outputStream)
    {
        if (buffer.Length == 0)
        {
            return;
        }

        context.Response.ContentLength = buffer.Length;
        buffer.Position = 0;
        await buffer.CopyToAsync(outputStream, context.RequestAborted).ConfigureAwait(false);
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
