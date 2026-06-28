using Api.Application.Features.Users.CreateUser;
using Api.Application.Features.Users.GetUserById;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace Api.Infrastructure.Api.Controllers;

/// <summary>
/// Controlador HTTP para la gestión de Usuarios en la capa de Infraestructura.
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class UsuariosController : ControllerBase
{
    private readonly ISender _sender;

    public UsuariosController(ISender sender)
    {
        _sender = sender;
    }

    /// <summary>
    /// Crea un nuevo usuario.
    /// </summary>
    [HttpPost]
    public async Task<ActionResult<CreateUserResponse>> Create(
        [FromBody] CreateUserCommand command,
        CancellationToken cancellationToken)
    {
        try
        {
            var response = await _sender.Send(command, cancellationToken);
            return CreatedAtAction(nameof(GetById), new { id = response.Id }, response);
        }
        catch (Vogen.ValueObjectValidationException ex)
        {
            return BadRequest(new { Error = "Validación de datos fallida", Detalle = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { Error = "Regla de negocio violada", Detalle = ex.Message });
        }
    }

    /// <summary>
    /// Obtiene un usuario por su identificador único.
    /// </summary>
    [HttpGet("{id:guid}")]
    public async Task<ActionResult<GetUserByIdResponse>> GetById(
        Guid id,
        CancellationToken cancellationToken)
    {
        try
        {
            var response = await _sender.Send(new GetUserByIdQuery(id), cancellationToken);
            return Ok(response);
        }
        catch (Vogen.ValueObjectValidationException ex)
        {
            return BadRequest(new { Error = "Identificador inválido", Detalle = ex.Message });
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { Error = "No encontrado", Detalle = ex.Message });
        }
    }
}
