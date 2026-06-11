
using Microsoft.EntityFrameworkCore;
using Parking.Api.Data;
using Parking.Api.Interfaces;
using Parking.Api.Models;

namespace Parking.Api.Services
{
    public class FaturamentoService : IFaturamentoService
    {
        private readonly AppDbContext _db;
        public FaturamentoService(AppDbContext db) => _db = db;

   
        public async Task<List<Fatura>> GerarAsync(string competencia, CancellationToken ct = default)
        {
            var partes = competencia.Split('-');
            if (partes.Length != 2 || !int.TryParse(partes[0], out var ano) || !int.TryParse(partes[1], out var mes))
                throw new ArgumentException("Competência deve estar no formato yyyy-MM.");

            var diasNoMes = DateTime.DaysInMonth(ano, mes);
            var inicioMes = new DateTime(ano, mes, 1, 0, 0, 0, DateTimeKind.Utc);
            var inicioProxMes = inicioMes.AddMonths(1); 

            var mensalistas = await _db.Clientes
                .Where(c => c.Mensalista)
                .AsNoTracking()
                .ToListAsync(ct);

            var criadas = new List<Fatura>();

            foreach (var cli in mensalistas)
            {
                var existente = await _db.Faturas
                    .FirstOrDefaultAsync(f => f.ClienteId == cli.Id && f.Competencia == competencia, ct);
                if (existente != null) continue; 

                var historicos = await _db.VeiculosHistorico
                    .Where(h => h.ClienteId == cli.Id
                        && h.DataInicio < inicioProxMes
                        && (h.DataFim == null || h.DataFim > inicioMes))
                    .AsNoTracking()
                    .ToListAsync(ct);

                if (!historicos.Any()) continue;

                var taxaDiaria = (cli.ValorMensalidade ?? 0m) / diasNoMes;
                decimal valorTotal = 0m;
                var veiculoIds = new List<Guid>();

                foreach (var h in historicos)
                {
                    var inicioDia = (h.DataInicio.Date > inicioMes.Date ? h.DataInicio.Date : inicioMes.Date);
                    var fimDiaExclusivo = h.DataFim.HasValue
                        ? (h.DataFim.Value.Date < inicioProxMes.Date ? h.DataFim.Value.Date : inicioProxMes.Date)
                        : inicioProxMes.Date;

                    var dias = (int)(fimDiaExclusivo - inicioDia).TotalDays;
                    if (dias <= 0) continue;

                    valorTotal += taxaDiaria * dias;

                    if (!veiculoIds.Contains(h.VeiculoId))
                        veiculoIds.Add(h.VeiculoId);
                }

                if (valorTotal <= 0) continue;

                var fat = new Fatura
                {
                    Competencia = competencia,
                    ClienteId = cli.Id,
                    Valor = Math.Round(valorTotal, 2),
                    Observacao = $"Proporcional: {veiculoIds.Count} veículo(s) em {competencia}"
                };

                foreach (var vid in veiculoIds)
                    fat.Veiculos.Add(new FaturaVeiculo { FaturaId = fat.Id, VeiculoId = vid });

                _db.Faturas.Add(fat);
                criadas.Add(fat);
            }

            await _db.SaveChangesAsync(ct);
            return criadas;
        }
    }
}
