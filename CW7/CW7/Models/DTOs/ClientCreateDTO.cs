using System.ComponentModel.DataAnnotations;

namespace CW7.Models.DTOs;

public class ClientCreateDTO
{
    [Length(1, 120)]
    public required string FirstName { get; set; }
    [Length(1, 120)]
    public required string LastName { get; set; }
    [Length(1, 120), EmailAddress]
    public required string Email { get; set; }
    [RegularExpression(@"^\+48\d{9}$", ErrorMessage = "Telephone must be in format +48XXXXXXXXX")]
    public string? Telephone { get; set; }
    [RegularExpression(@"^\d{11}$", ErrorMessage = "PESEL must be exactly 11 digits.")]
    public string? Pesel { get; set; }
}