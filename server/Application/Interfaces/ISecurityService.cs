using Application.Models;
using Application.Models.Dtos;

namespace Application.Interfaces;

public interface ISecurityService
{

    public string GenerateJwt();
    public AuthResponseDto Login(AuthRequestDto dto);
    public void VerifyJwtOrThrow(string jwt);
}