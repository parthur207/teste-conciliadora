using Parking.Api.Models;

namespace Parking.Api.Interfaces
{
    public interface IFaturamentoService
    {
        Task<List<Fatura>> GerarAsync(string competencia, CancellationToken ct = default);
    }
}
