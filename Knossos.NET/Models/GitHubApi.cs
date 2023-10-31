using Knossos.NET.ViewModels;
using Knossos.NET.Views;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace Knossos.NET.Models
{
    public static class GitHubApi
    {
        private readonly static string GitHubRepoURL = "https://api.github.com/repos/Shivansps/Knossos.NET";

        public static async Task<GitHubRelease?> GetLastRelease()
        {

            using (HttpClient client = new HttpClient())
            {
                try
                {
                    client.Timeout = TimeSpan.FromSeconds(30);
                    client.DefaultRequestHeaders.UserAgent.Add(new ProductInfoHeaderValue("product", "1"));
                    HttpResponseMessage response = await client.GetAsync(GitHubRepoURL + "/releases/latest");
                    var json = await response.Content.ReadAsStringAsync();
                    return JsonSerializer.Deserialize<GitHubRelease>(json)!;
                }
                catch (Exception ex)
                {
                    Log.Add(Log.LogSeverity.Error, "GitHubApi.GetLastRelease()", ex);
                    return null;
                }
            }
        }
    }


    public class GitHubRelease
    {
        public string? url { get; set; }
        public string? assets_url { get; set; }
        public string? upload_url { get; set; }
        public string? html_url { get; set; }
        public int id { get; set; }
        public object? author { get; set; }
        public string? node_id { get; set; }
        public string? tag_name { get; set; }
        public string? target_commitish { get; set; }
        public string? name { get; set; }
        public bool draft { get; set; }
        public bool prerelease { get; set; }
        public string? created_at { get; set; }
        public string? published_at { get; set; }
        public GitHubReleaseAsset[]? assets { get; set; }
        public string? tarball_url { get; set; }
        public string? zipball_url { get; set; }
        public string? body { get; set; }
    }


    public class GitHubReleaseAsset
    {
        public string? url { get; set; }
        public int id { get; set; }
        public string? node_id { get; set; }
        public string? name { get; set; }
        public object? label { get; set; }
        public object? uploader { get; set; }
        public string? content_type { get; set; }
        public string? state { get; set; }
        public int size { get; set; }
        public int download_count { get; set; }
        public string? created_at { get; set; }
        public string? updated_at { get; set; }
        public string? browser_download_url { get; set; }
    }

}
