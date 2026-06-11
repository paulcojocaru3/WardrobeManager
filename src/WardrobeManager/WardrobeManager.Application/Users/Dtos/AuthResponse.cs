namespace WardrobeManager.Application.Users.Dtos;

// register/login response: JWT + the safe user projection
public record AuthResponse(string Token, UserDto User);
