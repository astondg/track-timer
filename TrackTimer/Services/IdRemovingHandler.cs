namespace TrackTimer.Services
{
    using System;
    using System.Net.Http;
    using System.Text;
    using System.Threading;
    using System.Threading.Tasks;
    using Newtonsoft.Json.Linq;

    public class IdRemovingHandler : DelegatingHandler
    {
        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            if (request.Method.Method.Equals("POST", StringComparison.OrdinalIgnoreCase))
            {
                if (request.Content != null && request.Content.Headers.ContentType != null)
                {
                    if (request.Content.Headers.ContentType.MediaType.Equals("application/json", StringComparison.OrdinalIgnoreCase))
                    {
                        var json = await request.Content.ReadAsStringAsync();
                        var body = JObject.Parse(json);
                        if (body.Remove("id"))
                        {
                            request.Content = new StringContent(body.ToString(), Encoding.UTF8, "application/json");
                        }
                    }
                }
            }
            return await base.SendAsync(request, cancellationToken);
        }
    }
}