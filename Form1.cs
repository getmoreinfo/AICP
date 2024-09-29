using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using System.Windows.Forms;
using Newtonsoft.Json.Linq;
using OpenQA.Selenium.Chrome;
using OpenQA.Selenium.Support.UI;
using OpenQA.Selenium;
using SeleniumExtras.WaitHelpers;
using static System.Windows.Forms.VisualStyles.VisualStyleElement;
using System.Xml.Linq;

namespace DU_AICP
{
    public partial class Form1 : Form
    {
        // Spotify API Client ID 및 Secret
        private static readonly string clientId = "27840cda88d14f4dab5dd0f29ea781ff";
        private static readonly string clientSecret = "981e7dd893bf4c02b001cf5a44c32984";

        public Form1()
        {
            InitializeComponent();
        }

        private async void button1_Click(object sender, EventArgs e)
        {
            string artistInput = textBox1.Text;
            if (!int.TryParse(textBox2.Text, out int bpm))
            {
                MessageBox.Show("BPM 값을 올바르게 입력하세요.");
                return;
            }

            // Spotify API에서 엑세스 토큰 가져오기
            string spotifyAccessToken = await GetSpotifyAccessToken(clientId, clientSecret);

            // 아티스트의 트랙 검색
            var tracks = await SearchTracksByArtist(artistInput, spotifyAccessToken);

            // BPM과 일치하는 트랙 필터링
            var matchingTracks = tracks.Where(t => Math.Abs((double)t["tempo"] - bpm) <= 8).ToList();

            if (matchingTracks.Any())
            {
                var trackInfo = matchingTracks.First();
                string trackName = trackInfo["name"].ToString();
                string spotifyTrackBPM = trackInfo["tempo"].ToString();
                string albumCoverUrl = trackInfo["album"]["images"].First["url"].ToString();

                // Chordify에서 해당 곡 검색
                string chordifyDetails = SearchChordify(artistInput, trackName);

                // Form2를 열고 데이터를 전달
                Form2 form2 = new Form2(artistInput, bpm.ToString(), trackName, spotifyTrackBPM, chordifyDetails, albumCoverUrl);
                form2.Show();
            }
            else
            {
                MessageBox.Show("해당 BPM에 맞는 트랙을 찾을 수 없습니다.");
            }
        }

        // Spotify API에서 엑세스 토큰을 가져오는 메서드
        private static async Task<string> GetSpotifyAccessToken(string clientId, string clientSecret)
        {
            using (var client = new HttpClient())
            {
                var authHeader = Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes($"{clientId}:{clientSecret}"));
                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", authHeader);

                var request = new HttpRequestMessage(HttpMethod.Post, "https://accounts.spotify.com/api/token");
                request.Content = new FormUrlEncodedContent(new[]
                {
                    new KeyValuePair<string, string>("grant_type", "client_credentials")
                });

                var response = await client.SendAsync(request);
                response.EnsureSuccessStatusCode();
                var responseBody = await response.Content.ReadAsStringAsync();
                var token = JObject.Parse(responseBody)["access_token"].ToString();
                return token;
            }
        }

        // 아티스트의 트랙을 검색하는 메서드
        private static async Task<JToken[]> SearchTracksByArtist(string artist, string accessToken)
        {
            using (var client = new HttpClient())
            {
                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
                var response = await client.GetStringAsync($"https://api.spotify.com/v1/search?q=artist:{artist}&type=track&limit=50");
                var jsonResponse = JObject.Parse(response);
                var tracks = jsonResponse["tracks"]["items"]
                    .Select(async track =>
                    {
                        string trackId = track["id"].ToString();
                        var audioFeatures = await GetAudioFeatures(trackId, accessToken);
                        track["tempo"] = audioFeatures["tempo"];
                        return track;
                    }).ToList();

                return await Task.WhenAll(tracks);
            }
        }

        // 트랙의 오디오 특성을 가져오는 메서드
        private static async Task<JObject> GetAudioFeatures(string trackId, string accessToken)
        {
            using (var client = new HttpClient())
            {
                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
                var response = await client.GetStringAsync($"https://api.spotify.com/v1/audio-features/{trackId}");
                return JObject.Parse(response);
            }
        }

        // Chordify에서 검색하고 Song Details 정보를 추출하는 메서드
        private static string SearchChordify(string artistName, string songTitle)
        {
            ChromeOptions options = new ChromeOptions();
            IWebDriver driver = new ChromeDriver(options);

            try
            {
                // Chordify 검색 페이지로 이동
                string searchUrl = $"https://chordify.net/search/{Uri.EscapeDataString(songTitle)}%20{Uri.EscapeDataString(artistName)}";
                driver.Navigate().GoToUrl(searchUrl);

                // 첫 번째 결과 클릭
                WebDriverWait wait = new WebDriverWait(driver, TimeSpan.FromSeconds(10));
                IWebElement firstResult = wait.Until(ExpectedConditions.ElementToBeClickable(By.CssSelector(".apm19lh")));
                firstResult.Click();

                // Song Details 섹션을 기다림
                wait.Until(ExpectedConditions.ElementIsVisible(By.CssSelector(".aqpm70f")));

                // Song Details 정보 추출
                IWebElement songDetails = driver.FindElement(By.CssSelector(".aqpm70f"));

                // Chords 추출
                var chords = songDetails.FindElements(By.CssSelector(".cbg1qdk"));
                string chordDetails = "Chords: ";
                foreach (var chord in chords)
                {
                    chordDetails += chord.Text + " ";
                }

                // Key 추출
                var key = songDetails.FindElement(By.XPath("//dt[text()='Key']/following-sibling::dd/div/span")).Text;

                // BPM 추출
                var bpm = songDetails.FindElement(By.XPath("//dt[text()='bpm']/following-sibling::dd")).Text;

                return $"{chordDetails}\nKey: {key}\nBPM: {bpm}";
            }
            catch (Exception ex)
            {
                return $"오류 발생: {ex.Message}";
            }
            finally
            {
                driver.Quit();
            }
        }

    }
}