// Application/Services/CatFactService.cs
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;
using Application.Dtos; // Pastikan namespace ini benar sesuai lokasi CatFactDto

namespace Application.Services
{
    public class CatFactService
    {
        private readonly HttpClient _httpClient;
        private readonly string _catFactsApiBaseUrl;

        public CatFactService(HttpClient httpClient, string catFactsApiBaseUrl)
        {
            _httpClient = httpClient;
            _catFactsApiBaseUrl = catFactsApiBaseUrl;
        }

        public async Task<string?> GetRandomCatFactAsync()
        {
            try
            {
                // Ganti dengan URL API Cat Facts Anda yang sesungguhnya
                var response = await _httpClient.GetFromJsonAsync<CatFactDto>($"{_catFactsApiBaseUrl}/facts/random");

                return response?.Text;
            }
            catch (HttpRequestException e)
            {
                Console.WriteLine($"[CatFactServiceError] Gagal terhubung ke API Cat Facts: {e.Message}");
                // Anda bisa log error ini lebih detail atau melemparkan exception kustom
                return null;
            }
            catch (Exception e)
            {
                Console.WriteLine($"[CatFactServiceError] Terjadi kesalahan saat mendapatkan fakta kucing: {e.Message}");
                return null;
            }
        }
    }
}