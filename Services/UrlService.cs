using System;
using System.Net.Http;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace yTodo.Services
{
    public class UrlService
    {
        private readonly HttpClient _httpClient;

        public UrlService()
        {
            _httpClient = new HttpClient();
            _httpClient.DefaultRequestHeaders.UserAgent.ParseAdd("Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/91.0.4472.124 Safari/537.36");
        }

        public async Task<string?> FetchTitleAsync(string url)
        {
            try
            {
                if (!url.StartsWith("http://") && !url.StartsWith("https://"))
                {
                    url = "https://" + url;
                }

                var response = await _httpClient.GetStringAsync(url);
                var match = Regex.Match(response, @"<title>\s*(.+?)\s*</title>", RegexOptions.IgnoreCase);

                if (match.Success)
                {
                    return System.Net.WebUtility.HtmlDecode(match.Groups[1].Value);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Failed to fetch title for {url}: {ex.Message}");
            }

            return null;
        }

        public string? ExtractUrl(string text)
        {
            var match = Regex.Match(text, @"(https?://[^\s]+)");
            return match.Success ? match.Value : null;
        }
    }
}
