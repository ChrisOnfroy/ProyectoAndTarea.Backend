using proyectandtask.Domain.Common;
using proyectandtask.Domain.Entities;
using proyectandtask.Domain.Enums;
using proyectandtask.Domain.Exceptions;

namespace Proyectoandtask.Domain.Tests;

public sealed class DomainModelTests
{
    [Fact]
    public void BaseEntity_CreaIdentificadoresUnicosYFechaUtc()
    {
        var before = DateTime.UtcNow;
        var first = new TestEntity();
        var second = new TestEntity();
        var after = DateTime.UtcNow;

        Assert.NotEqual(first.Id, second.Id);
        Assert.InRange(first.CreatedAt, before, after);
        Assert.Equal(DateTimeKind.Utc, first.CreatedAt.Kind);
        Assert.Null(first.UpdatedAt);
    }

    [Fact]
    public void BaseEntity_SetUpdatedAt_AsignaFechaUtc()
    {
        var entity = new TestEntity();
        var before = DateTime.UtcNow;

        entity.SetUpdatedAt();

        Assert.NotNull(entity.UpdatedAt);
        Assert.InRange(entity.UpdatedAt.Value, before, DateTime.UtcNow);
        Assert.Equal(DateTimeKind.Utc, entity.UpdatedAt.Value.Kind);
    }

    [Fact]
    public void Tarea_PorDefectoIniciaPendienteYSinPrioridadNiFechaLimite()
    {
        var tarea = new Tarea();

        Assert.Equal(EstadoTarea.pendiente, tarea.EstadoTarea);
        Assert.Null(tarea.PrioridadTarea);
        Assert.Null(tarea.FechaLimite);
    }

    [Fact]
    public void Proyecto_ConstructorAsignaSusFechas()
    {
        var fechaCreacion = DateTime.UtcNow.AddDays(-2);
        var fechaInicio = DateTime.UtcNow;
        var fechaFinalizacion = fechaInicio.AddDays(10);

        var proyecto = new Proyecto("Proyecto", "Descripción", fechaCreacion, fechaInicio, fechaFinalizacion);

        Assert.Equal(fechaCreacion, proyecto.FechaCreacion);
        Assert.Equal(fechaInicio, proyecto.FechaInicio);
        Assert.Equal(fechaFinalizacion, proyecto.FechaFinalizacion);
    }

    [Fact]
    public void ValueObject_ComparaComponentesDeValor()
    {
        var first = new TestValueObject("Bogotá", 110111);
        var equal = new TestValueObject("Bogotá", 110111);
        var different = new TestValueObject("Medellín", 500001);

        Assert.Equal(first, equal);
        Assert.Equal(first.GetHashCode(), equal.GetHashCode());
        Assert.NotEqual(first, different);
    }

    [Fact]
    public void DomainException_ConservaMensajeYCausaInterna()
    {
        var cause = new InvalidOperationException("causa");

        var exception = new DomainException("regla de dominio", cause);

        Assert.Equal("regla de dominio", exception.Message);
        Assert.Same(cause, exception.InnerException);
    }

    private sealed class TestEntity : BaseEntity;

    private sealed class TestValueObject(string city, int postalCode) : ValueObject
    {
        protected override IEnumerable<object?> GetEqualityComponents()
        {
            yield return city;
            yield return postalCode;
        }
    }
}
