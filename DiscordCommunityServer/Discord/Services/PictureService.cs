using System.IO;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using TeamSaberShared.SimpleJSON;

namespace TeamSaberServer.Discord.Services
{
    public class PictureService
    {
        private readonly HttpClient _http;

        public PictureService(HttpClient http)
            => _http = http;

        public async Task<Stream> GetCatPictureAsync()
        {
            var resp = await _http.GetAsync("https://cataas.com/cat");
            return await resp.Content.ReadAsStreamAsync();
        }

        public async Task<Stream> GetNekoPictureAsync()
        {
            var resp = await _http.GetAsync("https://nekos.life/api/v2/img/neko");
            var stringResp = await resp.Content.ReadAsStringAsync();

            JSONNode node = JSON.Parse(WebUtility.UrlDecode(stringResp));

            var pic = await _http.GetAsync(node["url"]);
            return await pic.Content.ReadAsStreamAsync();
        }
    }
}
