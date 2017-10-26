using System;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;

namespace Microsoft.CodePlex.Migration.WorkItems
{
    internal class HttpClientImpl : IHttpClient
    {
        private readonly HttpClient httpClient = new HttpClient();

        #region IHttpClient

        /// <summary>
        /// Gets the response string from a uri and returns it as a string.
        /// </summary>
        /// <remarks>
        /// This will throw a <see cref="HttpRequestFailedException"/> on Web and HttpRequest exceptions.
        /// This is to allow for callers to catch these types of exceptions and potentially retry them.
        /// The prupose of this method/class is to allow tests to build a mock and avoid the actual web request.
        /// </remarks>
        public async Task<string> DownloadStringAsync(string uri)
        {
            ArgValidate.IsNotNullNotEmptyNotWhiteSpace(uri, nameof(uri));

            try
            {
                return await httpClient.GetStringAsync(uri);
            }
            catch (Exception ex) when (ex is WebException || ex is HttpRequestException)
            {
                throw new HttpRequestFailedException($"Failed to get string from {uri}, error is {ex.Message}", ex);
            }
        }

        #region IDisposable

        public void Dispose() => httpClient.Dispose();

        #endregion

        #endregion
    }
}
