using Domain.Entities;
using Application.Dtos;

namespace Application.Mappers;

public static class UserMapper
{
    public static UserDto ToDto(User user)
    {
        return new UserDto
        {
            Name = user.Name,
            Age = DateTime.Now.Year - user.BirthDate.Year,
            Hobby = user.Hobby
        };
    }
}
