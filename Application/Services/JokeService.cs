// Application/Services/JokeService.cs
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;
using Application.Dtos; // Pastikan namespace ini benar sesuai lokasi JokeDto

namespace Application.Services
{
    public class JokeService
    {
        private readonly HttpClient _httpClient;
        private readonly string _jokeApiBaseUrl;

        public JokeService(HttpClient httpClient, string jokeApiBaseUrl)
        {
            _httpClient = httpClient;
            _jokeApiBaseUrl = jokeApiBaseUrl;

            // Penting: icanhazdadjoke.com memerlukan header Accept: application/json
            _httpClient.DefaultRequestHeaders.Add("Accept", "application/json");
        }

        public async Task<string?> GetRandomJokeAsync()
        {
            try
            {
                // Endpoint untuk joke acak
                var response = await _httpClient.GetFromJsonAsync<JokeDto>($"{_jokeApiBaseUrl}/");

                return response?.Joke;
            }
            catch (HttpRequestException e)
            {
                Console.WriteLine($"[JokeServiceError] Gagal terhubung ke API Joke: {e.Message}");
                return null;
            }
            catch (Exception e)
            {
                Console.WriteLine($"[JokeServiceError] Terjadi kesalahan saat mendapatkan joke: {e.Message}");
                return null;
            }
        }
    }
}