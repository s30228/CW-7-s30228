using CW7.Exceptions;
using CW7.Models.DTOs;
using CW7.Services;
using Microsoft.AspNetCore.Mvc;

namespace CW7.Controllers;

[ApiController]
[Route("[controller]")]
public class ClientsController(IDbService service) : ControllerBase
{
    // 2. GET http://localhost:port/clients/2/trips
    [HttpGet("{id}/trips")]
    public async Task<IActionResult> GetTripsByClientId([FromRoute]int id)
    {
        try
        {
            return Ok(await service.GetTripsByClientIdAsync(id));
        }
        catch (NotFoundException e)
        {
            return NotFound(e.Message);
        }
    }

    // 3. POST http://localhost:port/clients
    [HttpPost]
    public async Task<IActionResult> CreateClient([FromBody] ClientCreateDTO body)
    {
        var client = await service.CreateClientAsync(body);
        return Created($"/clients/{client.IdClient}", client);
    }

    // 4. PUT http://localhost:port/clients/{id}/trips/{tripId}
    [HttpPut("{id}/trips/{tripId}")]
    public async Task<IActionResult> AddClientToTrip([FromRoute] int id, int tripId)
    {
        try
        {
            await service.AddClientToTripAsync(id, tripId);
            return NoContent();
        }
        catch (NotFoundException e)
        {
            return NotFound(e.Message);
        }
        catch (BadRequestException e)
        {
            return BadRequest(e.Message);
        }
    }
    
    // 5. DELETE http://localhost:port/clients/{id}/trips/{tripId}
    [HttpDelete("{id}/trips/{tripId}")]
    public async Task<IActionResult> DeleteClientFromTrip([FromRoute] int id, int tripId)
    {
        try
        {
            await service.DeleteClientFromTripAsync(id, tripId);
            return NoContent();
        }
        catch (NotFoundException e)
        {
            return NotFound(e.Message);
        }
    }
}