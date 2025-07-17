// Application/Dtos/JokeDto.cs
namespace Application.Dtos
{
    public class JokeDto
    {
        public string Joke { get; set; } = null!;
        // Properti lain seperti 'id' atau 'status' bisa ditambahkan jika diperlukan,
        // tapi untuk menampilkan joke, properti 'Joke' saja sudah cukup.
    }
}