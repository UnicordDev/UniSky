using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using AngleSharp.Dom;
using AngleSharp.Io;
using HttpMethod = System.Net.Http.HttpMethod;

namespace UniSky.Helpers.AngleSharp;


/// <summary>
/// An HTTP requester based on <see cref="HttpClient"/>.
/// </summary>
public class CustomHttpClientRequester : BaseRequester
{
    #region Fields

    private readonly HttpClient _client;

    #endregion

    #region ctor

    /// <summary>
    /// Creates a new HTTP client request with a new HttpClient instance.
    /// </summary>
    public CustomHttpClientRequester()
        : this(new HttpClient())
    {
    }

    /// <summary>
    /// Creates a new HTTP client request.
    /// </summary>
    /// <param name="client">The HTTP client to use for requests.</param>
    public CustomHttpClientRequester(HttpClient client)
    {
        _client = client;
    }

    #endregion

    #region Properties

    /// <summary>
    /// Gets the used HttpClient for further manipulation.
    /// </summary>
    public HttpClient Client => _client;

    #endregion

    #region Methods

    /// <summary>
    /// Checks if the given protocol is supported.
    /// </summary>
    /// <param name="protocol">
    /// The protocol to check for, e.g., http.
    /// </param>
    /// <returns>
    /// True if the protocol is supported, otherwise false.
    /// </returns>
    public override Boolean SupportsProtocol(String protocol) =>
        protocol.Equals(ProtocolNames.Http, StringComparison.OrdinalIgnoreCase) ||
        protocol.Equals(ProtocolNames.Https, StringComparison.OrdinalIgnoreCase);

    /// <summary>
    /// Performs an asynchronous request that can be cancelled.
    /// </summary>
    /// <param name="request">The options to consider.</param>
    /// <param name="cancel">The token for cancelling the task.</param>
    /// <returns>
    /// The task that will eventually give the response data.
    /// </returns>
    protected override async Task<IResponse> PerformRequestAsync(Request request, CancellationToken cancel)
    {
        // create the request message
        var method = new HttpMethod(request.Method.ToString().ToUpperInvariant());
        var requestMessage = new HttpRequestMessage(method, request.Address);
        var contentHeaders = new List<KeyValuePair<String, String>>();

        foreach (var header in request.Headers)
        {
            // Source:
            // https://github.com/aspnet/Mvc/blob/02c36a1c4824936682b26b6c133d11bebee822a2/src/Microsoft.AspNet.Mvc.WebApiCompatShim/HttpRequestMessage/HttpRequestMessageFeature.cs
            if (!requestMessage.Headers.TryAddWithoutValidation(header.Key, header.Value))
            {
                contentHeaders.Add(new KeyValuePair<String, String>(header.Key, header.Value));
            }
        }

        // set up the content
        if (request.Content != null && method != HttpMethod.Get && method != HttpMethod.Head)
        {
            requestMessage.Content = new StreamContent(request.Content);

            foreach (var header in contentHeaders)
            {
                requestMessage.Content.Headers.TryAddWithoutValidation(header.Key, header.Value);
            }
        }

        try
        {
            // execute the request
            // WAM: Fix 1, like are you for real
            var responseMessage = await _client.SendAsync(requestMessage, HttpCompletionOption.ResponseHeadersRead, cancel).ConfigureAwait(false);

            // convert the response
            var response = new DefaultResponse
            {
                Headers = responseMessage.Headers.ToDictionary(p => p.Key, p => string.Join(", ", p.Value)),
                Address = Url.Convert(responseMessage.RequestMessage.RequestUri),
                StatusCode = responseMessage.StatusCode
            };

            // get the anticipated content
            var content = responseMessage.Content;
            if (content != null)
            {
                var contentType = content.Headers.ContentType;
               
                // WAM: Hack 1, only read HTML
                if (contentType is { MediaType: ("text/html" or "application/xml" or "application/xhtml+xml") })
                {
                    response.Content = await content.ReadAsStreamAsync()
                                                    .ConfigureAwait(false);

                    foreach (var pair in content.Headers)
                    {
                        response.Headers[pair.Key] = String.Join(", ", pair.Value);
                    }
                }
            }

            if (IsRedirected(response) && !response.Headers.ContainsKey(HeaderNames.SetCookie))
            {
                response.Headers[HeaderNames.SetCookie] = String.Empty;
            }

            return response;
        }
        catch (Exception)
        {
            // create a response to avoid failing (#28)
            return new DefaultResponse
            {
                Address = Url.Convert(request.Address),
                StatusCode = 0
            };
        }
    }

    private static Boolean IsRedirected(IResponse response)
    {
        var status = response.StatusCode;

        return status == HttpStatusCode.Redirect || status == HttpStatusCode.RedirectKeepVerb ||
               status == HttpStatusCode.RedirectMethod || status == HttpStatusCode.TemporaryRedirect ||
               status == HttpStatusCode.MovedPermanently || status == HttpStatusCode.MultipleChoices;
    }

    #endregion
}