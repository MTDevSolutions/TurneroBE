using Application.Interfaces;
using Domain.Entities;

namespace Tests.Fakes;

public class SucursalRepositoryFake : ISucursalRepository
{
    public List<Sucursal> Sucursales { get; } = [];

    public Sucursal Agregar(string nombre, bool activa = true)
    {
        var s = new Sucursal { Id = Sucursales.Count + 1, Nombre = nombre, Activa = activa };
        Sucursales.Add(s);
        return s;
    }

    public Task<List<Sucursal>> ListarAsync(CancellationToken cancellationToken = default)
        => Task.FromResult(Sucursales.ToList());

    public Task<Sucursal?> GetActivaAsync(int id, CancellationToken cancellationToken = default)
        => Task.FromResult(Sucursales.FirstOrDefault(s => s.Id == id && s.Activa));

    public Task<List<Sucursal>> ListarActivasAsync(CancellationToken cancellationToken = default)
        => Task.FromResult(Sucursales.Where(s => s.Activa).ToList());
}
