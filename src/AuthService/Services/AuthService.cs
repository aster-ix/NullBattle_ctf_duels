
using NullBattle.Auth.DTOs;
using NullBattle.Auth.Exceptions;
using NullBattle.Auth.Messaging;
using NullBattle.Auth.Models;
using NullBattle.Auth.Repositories;

namespace NullBattle.Auth.Services;

public class AuthService
{
    private readonly UserRepository _userRepository;
    private readonly PasswordHasher _passwordHasher;
    private readonly JwtTokenService _tokenService;

    private readonly LogPublisher _logPublisher;

    public AuthService(
        UserRepository userRepository,
        PasswordHasher passwordHasher,
        JwtTokenService tokenService,
        LogPublisher logPublisher)
    {
        _userRepository = userRepository;
        _passwordHasher = passwordHasher;
        _tokenService = tokenService;
        _logPublisher = logPublisher;
    }

    public async Task<AuthResponse> RegisterAsync(AuthRequest dto)
    {
        var nickname = dto.Nickname.Trim();

        if (nickname.Length == 0)
        {
            throw new AppException(400, "ник не может быть пустым");
        }
        if (dto.Password.Length < 6)
        {
            throw new AppException(400, "пароль должен быть не короче 6 символов");
        }

        var existing = await _userRepository.GetByNicknameAsync(nickname);
        if (existing != null)
        {
            throw new AppException(409, "игрок с таким ником уже есть");
        }

        var user = new User
        {
            Nickname = nickname,
            PasswordHash = _passwordHasher.Hash(dto.Password)
        };
        await _userRepository.AddAsync(user, "Player");
        _logPublisher.Info($"Зарегистрирован игрок {nickname} (id {user.Id})");

        return _tokenService.GenerateToken(user, new List<string> { "Player" });
    }

    public async Task<AuthResponse> LoginAsync(AuthRequest dto)
    {
        var user = await _userRepository.GetByNicknameAsync(dto.Nickname.Trim());


        if (user == null || !_passwordHasher.Verify(dto.Password, user.PasswordHash))
        {
            throw new AppException(401, "неверный ник или пароль");
        }

        if (user.IsBanned)
        {
            _logPublisher.Warn($"Попытка входа заблокированного игрока {user.Id}");
            throw new AppException(403, "аккаунт заблокирован");
        }

        var roles = user.UserRoles.Select(ur => ur.Role.Name).ToList();
        return _tokenService.GenerateToken(user, roles);
    }

    public async Task<UserDto> GetUserAsync(long userId)
    {
        var user = await _userRepository.GetByIdAsync(userId);
        if (user == null)
        {
            throw new AppException(404, "Пользователь не найден");
        }

        return ToDto(user);
    }

    public async Task<List<UserDto>> GetAllUsersAsync()
    {
        var users = await _userRepository.GetAllAsync();
        return users.Select(ToDto).ToList();
    }

    public async Task SetBanAsync(long adminId, long userId, bool isBanned)
    {
        var user = await _userRepository.GetByIdAsync(userId);
        if (user == null)
        {
            throw new AppException(404, "Пользователь не найден");
        }

        var isAdmin = user.UserRoles.Any(ur => ur.Role.Name == "Admin");
        if (isBanned && isAdmin)
        {
            throw new AppException(400, "Нельзя заблокировать администратора");
        }

        user.IsBanned = isBanned;
        await _userRepository.UpdateAsync(user);

        var action = isBanned ? "заблокировал" : "разблокировал";
        _logPublisher.Info($"Админ {adminId} {action} игрока {userId}");
    }

    private static UserDto ToDto(User user)
    {
        return new UserDto
        {
            Id = user.Id,
            Nickname = user.Nickname,
            IsBanned = user.IsBanned,
            Roles = user.UserRoles.Select(ur => ur.Role.Name).ToList()
        };
    }
}