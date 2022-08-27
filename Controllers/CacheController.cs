using System.Diagnostics.CodeAnalysis;
using Microsoft.AspNetCore.Mvc;
using StackExchange.Redis;

namespace SimpleCacheApi.Controllers;

[ApiController]
[Route("[controller]")]
[SuppressMessage("ReSharper", "TemplateIsNotCompileTimeConstantProblem")]
public class CacheController : ControllerBase
{
    private readonly ILogger<CacheController> _logger;
    private readonly IConnectionMultiplexer _redis;

    public CacheController(ILogger<CacheController> logger, IConnectionMultiplexer redis)
    {
        _logger = logger;
        _redis = redis;
    }

    [HttpGet("read/{key}")]
    public async Task<IActionResult> ReadCache(string key)
    {
        _logger.LogTrace("Getting a connection to the database");
        var db = _redis.GetDatabase();
        
        _logger.LogTrace($"Checking to see if {key} exists in the database");
        var exists = await db.KeyExistsAsync(key);

        if (!exists)
        {
            _logger.LogDebug($"Key {key} not found in the database");
            return NotFound();
        }

        _logger.LogTrace($"Asking the database for {key}");
        var value = await db.StringGetAsync(key);

        var text = value.ToString();
        _logger.LogDebug($"Got value \"{text}\" for {key}");
        
        return Ok(text);
    }

    [HttpPost("write/{key}")]
    public async Task<IActionResult> WriteCache(string key, [FromBody] string value)
    {
        _logger.LogTrace("Getting a connection to the database");
        var db = _redis.GetDatabase();
        
        _logger.LogTrace($"Checking to see if {key} exists in the database");
        var exists = await db.KeyExistsAsync(key);

        if (exists)
        {
            _logger.LogWarning($"Key {key} found in the database; cannot POST a new value");
            return Conflict($"{key} already exists; use PUT");
        }

        _logger.LogTrace($"Adding {key} with value \"{value}\"");
        var result = await db.StringSetAsync(key, value);
        
        return Ok(result);
    }

    [HttpPut("update/{key}")]
    public async Task<IActionResult> UpdateCache(string key, [FromBody] string value)
    {
        _logger.LogTrace("Getting a connection to the database");
        var db = _redis.GetDatabase();
        
        _logger.LogTrace($"Checking to see if {key} exists in the database");
        var exists = await db.KeyExistsAsync(key);

        if (!exists)
        {
            _logger.LogDebug($"Key {key} not found in the database");
            return NotFound();
        }

        _logger.LogTrace($"Updating {key} with value \"{value}\"");
        var result = await db.StringSetAsync(key, value);
        
        return Ok(result);
    }

    [HttpDelete("delete/{key}")]
    public async Task<IActionResult> DeleteCache(string key)
    {
        _logger.LogTrace("Getting a connection to the database");
        var db = _redis.GetDatabase();
        
        _logger.LogTrace($"Checking to see if {key} exists in the database");
        var exists = await db.KeyExistsAsync(key);

        if (!exists)
        {
            _logger.LogDebug($"Key {key} not found in the database");
            return NotFound();
        }

        _logger.LogDebug($"Deleting {key}");
        var result = await db.KeyDeleteAsync(key);
        
        return Ok(result);
    }
}
