using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using proyectoandtask.Application.Dtos;
using proyectandtask.Domain.Enums;
using Proyectoandtask.Application.Interfaces;

namespace Proyectandtask.Api.Controller;

[ApiController]
[Route("api/tareas")]
[Authorize]
public sealed class TareaController : ControllerBase
{
    private readonly ITareaService _tareaService;

    public TareaController(ITareaService tareaService)
    {
        _tareaService = tareaService;
    }

    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(TareaResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<TareaResponseDto>> GetById(Guid id, CancellationToken cancellationToken)
    {
        var tarea = await _tareaService.ObtenerPorIdAsync(id, cancellationToken);
        return tarea is null ? NotFound() : Ok(tarea);
    }

    [HttpGet("proyecto/{proyectoId:guid}")]
    [ProducesResponseType(typeof(IReadOnlyList<TareaResponseDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<TareaResponseDto>>> GetByProject(
        Guid proyectoId,
        [FromQuery] string? name,
        CancellationToken cancellationToken)
    {
        var tareas = await _tareaService.ObtenerPorProyectoAsync(proyectoId, name, cancellationToken);
        return tareas is null ? NotFound() : Ok(tareas);
    }

    [HttpGet("proyecto/{proyectoId:guid}/estado")]
    [ProducesResponseType(typeof(IReadOnlyList<TareaResponseDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<IReadOnlyList<TareaResponseDto>>> GetByState(
        Guid proyectoId,
        [FromQuery, BindRequired] EstadoTarea estado,
        CancellationToken cancellationToken)
    {
        var tareas = await _tareaService.ObtenerPorEstadoAsync(proyectoId, estado, cancellationToken);
        return tareas is null ? NotFound() : Ok(tareas);
    }

    [HttpGet("proyecto/{proyectoId:guid}/prioridad")]
    [ProducesResponseType(typeof(IReadOnlyList<TareaResponseDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<IReadOnlyList<TareaResponseDto>>> GetByPriority(
        Guid proyectoId,
        [FromQuery, BindRequired] PrioridadTarea prioridad,
        CancellationToken cancellationToken)
    {
        var tareas = await _tareaService.ObtenerPorPrioridadAsync(proyectoId, prioridad, cancellationToken);
        return tareas is null ? NotFound() : Ok(tareas);
    }

    [HttpGet("proyecto/{proyectoId:guid}/vencidas")]
    [ProducesResponseType(typeof(IReadOnlyList<TareaResponseDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<IReadOnlyList<TareaResponseDto>>> GetOverdue(
        Guid proyectoId,
        [FromQuery] DateTime? fechaReferencia,
        CancellationToken cancellationToken)
    {
        var tareas = await _tareaService.ObtenerVencidasNoCompletadasAsync(
            proyectoId,
            fechaReferencia ?? DateTime.UtcNow,
            cancellationToken);
        return tareas is null ? NotFound() : Ok(tareas);
    }

    [HttpPost]
    [ProducesResponseType(typeof(TareaResponseDto), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<TareaResponseDto>> Create(
        [FromBody] TareaRequestDto request,
        CancellationToken cancellationToken)
    {
        var tarea = await _tareaService.CrearAsync(request, cancellationToken);
        return CreatedAtAction(nameof(GetById), new { id = tarea.Id }, tarea);
    }

    [HttpPut("{id:guid}")]
    [ProducesResponseType(typeof(TareaResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<TareaResponseDto>> Update(
        Guid id,
        [FromBody] TareaRequestDto request,
        CancellationToken cancellationToken)
    {
        var tarea = await _tareaService.ActualizarAsync(id, request, cancellationToken);
        return tarea is null ? NotFound() : Ok(tarea);
    }

    [HttpPatch("{id:guid}/estado")]
    [ProducesResponseType(typeof(TareaResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<TareaResponseDto>> ChangeState(
        Guid id,
        [FromBody] CambiarEstadoRequest request,
        CancellationToken cancellationToken)
    {
        var tarea = await _tareaService.CambiarEstadoAsync(id, request.Estado, cancellationToken);
        return tarea is null ? NotFound() : Ok(tarea);
    }

    [HttpDelete("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        var deleted = await _tareaService.EliminarAsync(id, cancellationToken);
        return deleted ? NoContent() : NotFound();
    }
}

public sealed class CambiarEstadoRequest
{
    public required EstadoTarea Estado { get; init; }
}
