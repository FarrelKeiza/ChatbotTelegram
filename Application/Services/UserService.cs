using Domain.Interfaces;
using Application.Dtos;
using Application.Mappers;
using Domain.Entities;

namespace Application.Services
{
    public class UserService
    {
        private readonly IUserRepository _repo;

        public UserService(IUserRepository repo)
        {
            _repo = repo;
        }

        public async Task<UserDto?> GetUserInfoAsync(string username)
        {
            var user = await _repo.GetByUsernameAsync(username);
            return user == null ? null : UserMapper.ToDto(user);
        }

        public async Task<List<UserDto>> GetAllUsersAsync()
        {
            var users = await _repo.GetAllAsync(); // Kita akan menambahkan GetAllAsync di IUserRepository dan UserRepository
            return users.Select(u => UserMapper.ToDto(u)).ToList();
        }

        public async Task CreateUserAsync(UserDto userDto)
        {
            try
            {
                var user = new User
                {
                    Name = userDto.Name,
                    BirthDate = DateTime.UtcNow.AddYears(-userDto.Age),
                    Hobby = userDto.Hobby,
                    Username = userDto.Name.ToLower().Replace(" ", "_"),
                    Email = $"{userDto.Name.ToLower().Replace(" ", "")}@example.com"
                };

                await _repo.AddAsync(user);
            }
            catch (Exception ex)
            {
                Console.WriteLine("❌ Gagal insert user:");
                Console.WriteLine(ex.ToString()); // tampilkan inner exception juga
                throw; // biar tetap error ke Telegram
            }
        }



    }
}
