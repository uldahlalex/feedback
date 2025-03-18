using System.ComponentModel.DataAnnotations;

namespace Application.Models.Dtos;

public class AuthRequestDto
{
    [MinLength(3)] [Required] public string ClientId { get; set; } = null!;
    [MinLength(4)] [Required] public string Password { get; set; } = null!;
}