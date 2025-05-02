using CW7.Exceptions;
using CW7.Models.DTOs;
using Microsoft.Data.SqlClient;

namespace CW7.Services;

public class DbService(IConfiguration config) : IDbService
{
    private readonly string? _connectionString = config.GetConnectionString("Default");
    
    // 1. GET http://localhost:port/trips
    public async Task<IEnumerable<TripCountryGetDTO>> GetTripsAndCountryAsync()
    {
        // establish connection
        await using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync();
        
        // joins Trip and Country tables to get trip info with country names
        var sql = @"
        SELECT t.IdTrip, t.Name, t.Description, t.DateFrom, t.DateTo, t.MaxPeople, c.Name
        FROM Trip t 
        JOIN Country_Trip ct ON t.IdTrip = ct.IdTrip 
        JOIN Country c ON ct.IdCountry = c.IdCountry
        ORDER BY t.IdTrip";
        
        await using var command = new SqlCommand(sql, connection); 
        await using var reader = await command.ExecuteReaderAsync();
        var tripMap = new Dictionary<int, TripCountryGetDTO>(); // helps to keep all countries for one trip
        while (await reader.ReadAsync())
        {
            int id = reader.GetInt32(0); // dict key
            if (!tripMap.TryGetValue(id, out var trip))
            {
                trip = new TripCountryGetDTO
                {
                    IdTrip = id,
                    Name = reader.GetString(1),
                    Description = reader.IsDBNull(2) ? null : reader.GetString(2),
                    DateFrom = reader.GetDateTime(3),
                    DateTo = reader.GetDateTime(4),
                    MaxPeople = reader.GetInt32(5),
                    Countries = new List<string>()
                };
                tripMap[id] = trip;
            }
            trip.Countries.Add(reader.GetString(6));
        }
        
        return tripMap.Values;
    }

    // 2. GET http://localhost:port/clients/2/trips
    public async Task<IEnumerable<TripClientGetDTO>> GetTripsByClientIdAsync(int clientId)
    {
        await using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync();
        
        // client validation
        // takes client with given id, checks if it exists later
        var sql = "SELECT 1 FROM Client WHERE IdClient = @clientId";
        
        await using var command = new SqlCommand(sql, connection);
        command.Parameters.AddWithValue("@clientId", clientId);
        await using (var reader = await command.ExecuteReaderAsync())
        {
            if (!reader.HasRows)
            {
                throw new NotFoundException($"Client with id {clientId} not found");
            }
        }
        
        var result = new List<TripClientGetDTO>();
        // joins Trip and Client_Trip tables to get trip info with registration info for a specified client
        string sql2 = @"
        SELECT t.IdTrip, t.Name, t.Description, t.DateFrom, t.DateTo, t.MaxPeople,
               ct.RegisteredAt, ct.PaymentDate
        FROM Trip t
        JOIN Client_Trip ct ON t.IdTrip = ct.IdTrip
        WHERE ct.IdClient = @clientId";
        
        await using var command2 = new SqlCommand(sql2, connection);
        command2.Parameters.AddWithValue("@clientId", clientId);
        await using var reader2 = await command2.ExecuteReaderAsync();
        
        while (await reader2.ReadAsync())
        {
            result.Add(new TripClientGetDTO
            {
                IdTrip = reader2.GetInt32(0),
                Name = reader2.GetString(1),
                Description = reader2.IsDBNull(2) ? null : reader2.GetString(2),
                DateFrom = reader2.GetDateTime(3),
                DateTo = reader2.GetDateTime(4),
                MaxPeople = reader2.GetInt32(5),
                RegisteredAt = reader2.GetInt32(6),
                PaymentDate = reader2.IsDBNull(7) ? null : reader2.GetInt32(7),
            });
        }

        if (result.Count == 0)
        {
            throw new NotFoundException($"Client has no trips");
        }
        
        return result;
    }

    // 3. POST http://localhost:port/clients
    public async Task<ClientGetDTO> CreateClientAsync(ClientCreateDTO client)
    {
        await using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync();
        
        // creates new client, gives it unique id
        var sql = @"
        INSERT INTO Client (FirstName, LastName, Email, Telephone, Pesel)
        VALUES (@FirstName, @LastName, @Email, @Telephone, @Pesel);
        SELECT SCOPE_IDENTITY()";
        
        await using var command = new SqlCommand(sql, connection);
        command.Parameters.AddWithValue("@FirstName", client.FirstName);
        command.Parameters.AddWithValue("@LastName", client.LastName);
        command.Parameters.AddWithValue("@Email", client.Email);
        command.Parameters.AddWithValue("@Telephone", client.Telephone ?? (object)DBNull.Value);
        command.Parameters.AddWithValue("@Pesel", client.Pesel ?? (object)DBNull.Value);
        
        var id = Convert.ToInt32(await command.ExecuteScalarAsync());

        return new ClientGetDTO
        {
            IdClient = id,
            FirstName = client.FirstName,
            LastName = client.LastName,
            Email = client.Email,
            Telephone = client.Telephone,
            Pesel = client.Pesel
        };
    }

    // 4. PUT http://localhost:port/clients/{id}/trips/{tripId}
    public async Task AddClientToTripAsync(int clientId, int tripId)
    {
        await using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync();

        // client validation
        // takes client with given id, checks if it exists later
        var clientQuery = "SELECT 1 FROM Client WHERE IdClient = @clientId";
        await using var clientCmd = new SqlCommand(clientQuery, connection);
        clientCmd.Parameters.AddWithValue("@clientId", clientId);
        await using (var clientReader = await clientCmd.ExecuteReaderAsync())
        {
            if (!clientReader.HasRows)
            {
                throw new NotFoundException($"Client with id {clientId} not found");
            }
        }

        // trip validation + max people count
        // gets MaxPeople fot the trip (used later), checks if it exists later
        var tripQuery = "SELECT MaxPeople FROM Trip WHERE IdTrip = @tripId";
        int maxPeople;
        await using var tripCmd = new SqlCommand(tripQuery, connection);
        tripCmd.Parameters.AddWithValue("@tripId", tripId);
        await using (var tripReader = await tripCmd.ExecuteReaderAsync())
        {
            if (!await tripReader.ReadAsync())
            {
                throw new NotFoundException($"Trip with id {tripId} not found");
            }
            maxPeople = tripReader.GetInt32(0);
        }

        // MaxPeople validation
        // counts registrations for the given trip
        var countQuery = "SELECT COUNT(*) FROM Client_Trip WHERE IdTrip = @tripId";
        await using var countCmd = new SqlCommand(countQuery, connection);
        countCmd.Parameters.AddWithValue("@tripId", tripId);
        var currentCount = Convert.ToInt32(await countCmd.ExecuteScalarAsync());

        if (currentCount >= maxPeople)
        {
            throw new BadRequestException("Maximum number of participants has been reached.");
        }
        
        // adds new registration for given client and trip
        var insertQuery = @"
        INSERT INTO Client_Trip (IdClient, IdTrip, RegisteredAt)
        VALUES (@clientId, @tripId, @registeredAt)";

        await using var insertCmd = new SqlCommand(insertQuery, connection);
        insertCmd.Parameters.AddWithValue("@clientId", clientId); 
        insertCmd.Parameters.AddWithValue("@tripId", tripId); 
        insertCmd.Parameters.AddWithValue("@registeredAt", "20250502");

        await insertCmd.ExecuteNonQueryAsync();
    }

    // 5. DELETE http://localhost:port/clients/{id}/trips/{tripId}
    public async Task DeleteClientFromTripAsync(int clientId, int tripId)
    {
        await using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync();
        
        // registration validation
        // gets registration for given client and trip
        var checkSql = @"
        SELECT 1 FROM Client_Trip 
        WHERE IdClient = @clientId AND IdTrip = @tripId";
        
        await using var checkCmd = new SqlCommand(checkSql, connection);
        checkCmd.Parameters.AddWithValue("@clientId", clientId);
        checkCmd.Parameters.AddWithValue("@tripId", tripId);
        await using (var tripReader = await checkCmd.ExecuteReaderAsync())
        {
            if (!tripReader.HasRows)
            {
                throw new NotFoundException("Registration does not exist");
            }
        }
        
        // deletes registration for given client and trip
        var deleteSql = @"
        DELETE FROM Client_Trip 
        WHERE IdClient = @clientId AND IdTrip = @tripId";
        
        await using var deleteCmd = new SqlCommand(deleteSql, connection);
        deleteCmd.Parameters.AddWithValue("@clientId", clientId);
        deleteCmd.Parameters.AddWithValue("@tripId", tripId);
        var rowsAffected = await deleteCmd.ExecuteNonQueryAsync();
        if (rowsAffected == 0)
        {
            throw new Exception("Failed to delete registration");
        }
    }
}