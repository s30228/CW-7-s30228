using CW7.Services;
using Microsoft.AspNetCore.Mvc;

namespace CW7.Controllers;

[ApiController]
[Route("[controller]")]
public class TripsController(IDbService service) : ControllerBase
{
    // 1. GET http://localhost:port/trips
    [HttpGet]
    public async Task<IActionResult> GetAllTripsAndCountry()
    {
        return Ok(await service.GetTripsAndCountryAsync());
    }
}