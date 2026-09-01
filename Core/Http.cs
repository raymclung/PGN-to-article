using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using System.Threading;

namespace ChessArticle
{
    /// <summary>
    /// What one HTTP call produced. Two failures are worth telling apart: the
    /// server answered with an error status (<see cref="Sent"/> is true, and
    /// <see cref="Status"/> plus <see cref="Body"/> say what went wrong), or the
    /// request never got an answer at all — connection refused, DNS failure,
    /// timeout (<see cref="TransportError"/> explains it).
    ///
    /// The retry logic in each client depends on that distinction: a 503 is
    /// worth retrying, a refused connection to a local server is not.
    /// </summary>
    public sealed class HttpOutcome
    {
        public bool Sent { get; set; }
        public int Status { get; set; }
        public string Body { get; set; }
        public string TransportError { get; set; }

        public HttpOutcome()
        {
            Body = "";
            TransportError = "";
        }

        public bool IsSuccess { get { return Sent && Status >= 200 && Status < 300; } }
    }

    /// <summary>
    /// Minimal synchronous JSON POST helper over <see cref="HttpClient"/>.
    ///
    /// One shared client is reused for every call: creating a new one per
    /// request exhausts sockets under load, which is the classic HttpClient
    /// mistake. Per-request timeouts come from a cancellation token rather than
    /// HttpClient.Timeout, because that property is shared by every caller.
    /// </summary>
    public static class Http
    {
        private static readonly HttpClient Shared = new HttpClient
        {
            // Effectively unlimited; each call sets its own deadline below.
            Timeout = Timeout.InfiniteTimeSpan,
        };

        public static HttpOutcome PostJson(string url, string json, int timeoutMs,
            IDictionary<string, string> headers = null)
        {
            var outcome = new HttpOutcome();
            try
            {
                using (var request = new HttpRequestMessage(HttpMethod.Post, url))
                using (var cts = new CancellationTokenSource(timeoutMs))
                {
                    request.Content = new StringContent(json, Encoding.UTF8, "application/json");
                    if (headers != null)
                    {
                        foreach (KeyValuePair<string, string> h in headers)
                            request.Headers.TryAddWithoutValidation(h.Key, h.Value);
                    }

                    using (HttpResponseMessage response =
                               Shared.SendAsync(request, cts.Token).GetAwaiter().GetResult())
                    {
                        outcome.Sent = true;
                        outcome.Status = (int)response.StatusCode;
                        outcome.Body = response.Content.ReadAsStringAsync()
                                               .GetAwaiter().GetResult();
                    }
                }
            }
            catch (OperationCanceledException)
            {
                outcome.TransportError = "Request timed out after " + timeoutMs + " ms";
            }
            catch (Exception ex)
            {
                Exception inner = ex.InnerException ?? ex;
                outcome.TransportError = inner.Message;
            }
            return outcome;
        }
    }
}
