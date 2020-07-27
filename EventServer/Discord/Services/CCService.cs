using EventShared.SimpleJSON;
using System.Net.Http;
using System.Threading.Tasks;

namespace EventServer.Discord.Services
{
    public class CCService
    {
        public JSONArray GetPlayers(JSONNode basicData)
        {
            return basicData["players"].AsArray;
        }

        public async Task<JSONNode> GetTeamData(string teamId)
        {
            HttpClientHandler httpClientHandler = new HttpClientHandler();
            httpClientHandler.AllowAutoRedirect = false;

            using (var client = new HttpClient(httpClientHandler))
            {
                client.DefaultRequestHeaders.Add("user-agent", "EventServer");

                var response = await client.GetAsync("https://cube.community/main/bswc/api/player_roster");
                var responseText = await response.Content.ReadAsStringAsync();
                var teamList = JSON.Parse(responseText);

                return teamList[teamId];
            }
        }
    }
}
