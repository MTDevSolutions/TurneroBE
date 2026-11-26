using Application.Interfaces;
using Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Data;

public class PacienteRepository(TurneroContext context) : IPacienteRepository
{
    // El global query filter limita todo al tenant del request.
    public async Task<(List<Paciente> Items, int Total)> ListarAsync(
        int page, int pageSize, string? buscar, bool incluirInactivos, CancellationToken cancellationToken = default)
    {
        var query = context.Pacientes.AsNoTracking();

        if (!incluirInactivos)
            query = query.Where(p => p.Activo);

        if (!string.IsNullOrWhiteSpace(buscar))
        {
            // % y _ que escribe el usuario son texto, no comodines (la barra invertida es el escape por defecto de ILIKE).
            var literal = buscar.Trim().Replace(@"\", @"\\").Replace("%", @"\%").Replace("_", @"\_");
            var patron = $"%{literal}%";
            var digitos = new string(buscar.Where(char.IsAsciiDigit).ToArray());
            // Sin acentos de los dos lados: "gomez" encuentra "Gómez" y "gómez" encuentra "Gomez".
            query = query.Where(p =>
                EF.Functions.ILike(EF.Functions.Unaccent(p.Nombre + " " + p.Apellido), EF.Functions.Unaccent(patron))
                || EF.Functions.ILike(EF.Functions.Unaccent(p.Apellido + " " + p.Nombre), EF.Functions.Unaccent(patron))
                || (digitos.Length >= 3 && p.Celular.Contains(digitos)));
        }

        var total = await query.CountAsync(cancellationToken);
        var items = await query
            .OrderBy(p => p.Apellido).ThenBy(p => p.Nombre).ThenBy(p => p.Id)
            .Skip((page - 1) * pageSize).Take(pageSize)
            .ToListAsync(cancellationToken);

        return (items, total);
    }

    public async Task<Paciente?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
        => await context.Pacientes.FirstOrDefaultAsync(p => p.Id == id, cancellationToken);

    public async Task<bool> CelularEnUsoAsync(string celular, int? excluirId, CancellationToken cancellationToken = default)
        => await context.Pacientes.AnyAsync(p => p.Celular == celular && p.Id != excluirId, cancellationToken);

    public void Agregar(Paciente paciente)
        => context.Pacientes.Add(paciente); // TenantId lo completa SaveChanges
}
