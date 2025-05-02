using System.Text.Json.Serialization;

namespace CW7.Models.DTOs;

public class TripClientGetDTO : TripGetDTO
{
    [JsonPropertyOrder(6)]
    public int RegisteredAt { get; set; }
    [JsonPropertyOrder(7)]
    public int? PaymentDate { get; set; }
}