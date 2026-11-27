using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using proyectoandtask.Application.Dtos;
using Proyectoandtask.Application.Interfaces;

namespace Proyectandtask.Api.Controller;

[ApiController]
[Route("api/v1/proyectos")]
[Authorize]
public sealed class ProyectoController : ControllerBase
{
    private readonly IProyectoService _proyectoService;

    public ProyectoController(IProyectoService proyectoService)
    {
        _proyectoService = proyectoService;
    }

    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyList<ProyectoResponseDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<ProyectoResponseDto>>> GetAll(CancellationToken cancellationToken)
    {
        var proyectos = await _proyectoService.ObtenerTodosAsync(cancellationToken);
        return Ok(proyectos);
    }

    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(ProyectoResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ProyectoResponseDto>> GetById(Guid id, CancellationToken cancellationToken)
    {
        var proyecto = await _proyectoService.ObtenerPorIdAsync(id, cancellationToken);
        return proyecto is null ? NotFound() : Ok(proyecto);
    }

    [HttpGet("{id:guid}/avance")]
    [ProducesResponseType(typeof(decimal), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<decimal>> GetProgress(Guid id, CancellationToken cancellationToken)
    {
        var progress = await _proyectoService.ObtenerAvancePorcentajeAsync(id, cancellationToken);
        return progress is null ? NotFound() : Ok(progress.Value);
    }

    [HttpPost]
    [ProducesResponseType(typeof(ProyectoResponseDto), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<ProyectoResponseDto>> Create(
        [FromBody] ProyectoRequestDto request,
        CancellationToken cancellationToken)
    {
        var proyecto = await _proyectoService.CrearAsync(request, cancellationToken);
        return CreatedAtAction(nameof(GetById), new { id = proyecto.Id }, proyecto);
    }

    [HttpPut("{id:guid}")]
    [ProducesResponseType(typeof(ProyectoResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ProyectoResponseDto>> Update(
        Guid id,
        [FromBody] ProyectoRequestDto request,
        CancellationToken cancellationToken)
    {
        var proyecto = await _proyectoService.ActualizarAsync(id, request, cancellationToken);
        return proyecto is null ? NotFound() : Ok(proyecto);
    }

    [HttpDelete("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        var deleted = await _proyectoService.EliminarAsync(id, cancellationToken);
        return deleted ? NoContent() : NotFound();
    }
}
