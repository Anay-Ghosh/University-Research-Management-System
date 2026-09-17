using System.Text.Json;

namespace LifeNetAssist.MVC.Services
{
    public class GitHubCommit
    {
        public string Sha { get; set; } = "";
        public string Message { get; set; } = "";
        public string Author { get; set; } = "";
        public DateTime Date { get; set; }
        public string Url { get; set; } = "";
    }

    public class GitHubService
    {
        private readonly HttpClient _http;

        public GitHubService(HttpClient http)
        {
            _http = http;
            if (!_http.DefaultRequestHeaders.Contains("User-Agent"))
                _http.DefaultRequestHeaders.Add("User-Agent", "URMS-App");
        }

        // Turns https://github.com/owner/repo into "owner/repo"
        public static string? ParseRepo(string? url)
        {
            if (string.IsNullOrWhiteSpace(url)) return null;

            var cleaned = url.Trim().TrimEnd('/');
            cleaned = cleaned.Replace("https://github.com/", "")
                             .Replace("http://github.com/", "")
                             .Replace("www.github.com/", "");

            if (cleaned.EndsWith(".git")) cleaned = cleaned.Substring(0, cleaned.Length - 4);

            var parts = cleaned.Split('/', StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length < 2) return null;

            return parts[0] + "/" + parts[1];
        }

        public async Task<(List<GitHubCommit> commits, string? error)> GetCommitsAsync(string? repoUrl, int limit = 20)
        {
            var repo = ParseRepo(repoUrl);
            if (repo == null)
                return (new List<GitHubCommit>(), "No valid GitHub repository URL is linked.");

            var api = "https://api.github.com/repos/" + repo + "/commits?per_page=" + limit;

            try
            {
                var response = await _http.GetAsync(api);
                var body = await response.Content.ReadAsStringAsync();

                if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
                    return (new List<GitHubCommit>(), "Repository not found. Make sure it is public and the URL is correct.");

                if (!response.IsSuccessStatusCode)
                    return (new List<GitHubCommit>(), "GitHub request failed (" + (int)response.StatusCode + ").");

                var list = new List<GitHubCommit>();
                using var doc = JsonDocument.Parse(body);

                foreach (var item in doc.RootElement.EnumerateArray())
                {
                    var commit = item.GetProperty("commit");
                    var author = commit.GetProperty("author");

                    list.Add(new GitHubCommit
                    {
                        Sha = item.GetProperty("sha").GetString() ?? "",
                        Message = commit.GetProperty("message").GetString() ?? "",
                        Author = author.GetProperty("name").GetString() ?? "",
                        Date = author.GetProperty("date").GetDateTime(),
                        Url = item.GetProperty("html_url").GetString() ?? ""
                    });
                }

                return (list, null);
            }
            catch (Exception ex)
            {
                return (new List<GitHubCommit>(), "GitHub error: " + ex.Message);
            }
        }
    }
}

