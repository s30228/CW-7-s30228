using CW7.Models.DTOs;

namespace CW7.Services;

public interface IDbService
{
    public Task<IEnumerable<TripCountryGetDTO>> GetTripsAndCountryAsync();
    public Task<IEnumerable<TripClientGetDTO>> GetTripsByClientIdAsync(int clientId);
    public Task<ClientGetDTO> CreateClientAsync(ClientCreateDTO client);
    public Task AddClientToTripAsync(int clientId, int tripId);
    public Task DeleteClientFromTripAsync(int clientId, int tripId);
}