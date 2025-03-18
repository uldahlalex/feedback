using System.ComponentModel.DataAnnotations;

namespace Application.Models.Dtos;

public class AuthResponseDto
{
    [Required] public string Jwt { get; set; } = null!;
}