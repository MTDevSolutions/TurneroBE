using Domain.Entities;

namespace Application.Interfaces;

public interface IPacienteRepository
{
    /// <summary>Página de pacientes de la cuenta, por apellido y nombre. 'buscar' filtra por nombre,
    /// apellido o celular.</summary>
    Task<(List<Paciente> Items, int Total)> ListarAsync(int page, int pageSize, string? buscar, bool incluirInactivos, CancellationToken cancellationToken = default);

    /// <summary>Paciente de la cuenta (trackeado); null si no existe o es de otra cuenta.</summary>
    Task<Paciente?> GetByIdAsync(int id, CancellationToken cancellationToken = default);

    /// <summary>True si otro paciente de la cuenta ya usa ese celular.</summary>
    Task<bool> CelularEnUsoAsync(string celular, int? excluirId, CancellationToken cancellationToken = default);

    void Agregar(Paciente paciente);
}
