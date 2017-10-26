using System;
using System.Threading.Tasks;

namespace Microsoft.CodePlex.Migration.WorkItems
{
    internal interface IHttpClient : IDisposable
    {
        Task<string> DownloadStringAsync(string uri);
    }
}
