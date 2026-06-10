
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Parking.Api.Data;
using Parking.Api.Dtos;
using Parking.Api.Services;

namespace Parking.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class FaturasController : ControllerBase
    {
        private readonly AppDbContext _db;
        private readonly FaturamentoService _fat;
        public FaturasController(AppDbContext db, FaturamentoService fat) { _db = db; _fat = fat; }

        [HttpPost("gerar")]
        public async Task<IActionResult> Gerar([FromBody] GerarFaturaRequest req, CancellationToken ct)
        {
            if (string.IsNullOrWhiteSpace(req.Competencia))
                return BadRequest("O formato informado não pode ser nulo. Opte por enviar conforme exemplo a seguir: (ano-mes) => 2026-06");

            var criadas = await _fat.GerarAsync(req.Competencia, ct);

            return Ok(new { criadas = criadas.Count });
        }

        [HttpGet]
        public async Task<IActionResult> List([FromQuery] string? competencia = null)
        {
            var q = _db.Faturas.AsQueryable();
            if (!string.IsNullOrWhiteSpace(competencia)) q = q.Where(f => f.Competencia == competencia);

            var list = await q
                .OrderByDescending(f => f.CriadaEm)
                .Join(_db.Clientes, f => f.ClienteId, c => c.Id, (f, c) => new {
                    f.Id,
                    f.Competencia,
                    f.ClienteId,
                    clienteNome = c.Nome,
                    f.Valor,
                    f.CriadaEm,
                    f.Observacao,
                    qtdVeiculos = f.Veiculos.Count
                })
                .ToListAsync();

            return Ok(list);
        }

        [HttpGet("{id:guid}/placas")]
        public async Task<IActionResult> Placas(Guid id)
        {
            var placas = await _db.FaturasVeiculos
                .Where(x => x.FaturaId == id)
                .Join(_db.Veiculos, fv => fv.VeiculoId, v => v.Id, (fv, v) => v.Placa)
                .ToListAsync();

            return Ok(placas);
        }
    }
}
