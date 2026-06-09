
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Parking.Api.Data;
using Parking.Api.Dtos;
using Parking.Api.Models;
using Parking.Api.Services;

namespace Parking.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class VeiculosController : ControllerBase
    {
        private readonly AppDbContext _db;
        private readonly PlacaService _placa;
        public VeiculosController(AppDbContext db, PlacaService placa) { _db = db; _placa = placa; }

        [HttpGet]
        public async Task<IActionResult> List([FromQuery] Guid? clienteId = null)
        {
            var q = _db.Veiculos
                .Include(v => v.Cliente)
                .AsQueryable();
            if (clienteId.HasValue) q = q.Where(v => v.ClienteId == clienteId.Value);
            var list = await q
                .OrderBy(v => v.Placa)
                .Select(v => new {
                    v.Id, v.Placa, v.Modelo, v.Ano, v.ClienteId,
                    clienteNome = v.Cliente != null ? v.Cliente.Nome : null
                })
                .ToListAsync();
            return Ok(list);
        }

        [HttpPost]
        public async Task<IActionResult> Create([FromBody] VeiculoCreateDto dto)
        {
            var placa = _placa.Sanitizar(dto.Placa);
            if (!_placa.EhValida(placa)) return BadRequest("Placa inválida. Formatos aceitos: ABC1234 (antigo) ou ABC1D23 (Mercosul).");
            if (await _db.Veiculos.AnyAsync(v => v.Placa == placa)) return Conflict("Placa já cadastrada.");

            var clienteExiste = await _db.Clientes.AnyAsync(c => c.Id == dto.ClienteId);
            if (!clienteExiste) return BadRequest("Cliente não encontrado.");

            var v = new Veiculo { Placa = placa, Modelo = dto.Modelo, Ano = dto.Ano, ClienteId = dto.ClienteId };
            _db.Veiculos.Add(v);

            // Datas normalizadas ao dia (sem hora) para cálculo proporcional correto
            _db.VeiculosHistorico.Add(new VeiculoHistorico
            {
                VeiculoId = v.Id,
                ClienteId = dto.ClienteId,
                DataInicio = DateTime.UtcNow.Date
            });

            await _db.SaveChangesAsync();
            return CreatedAtAction(nameof(GetById), new { id = v.Id }, v);
        }

        [HttpGet("{id:guid}")]
        public async Task<IActionResult> GetById(Guid id)
        {
            var v = await _db.Veiculos.Include(x => x.Cliente).FirstOrDefaultAsync(x => x.Id == id);
            return v == null ? NotFound("Veículo não encontrado.") : Ok(v);
        }

        [HttpPut("{id:guid}")]
        public async Task<IActionResult> Update(Guid id, [FromBody] VeiculoUpdateDto dto)
        {
            var v = await _db.Veiculos.FindAsync(id);
            if (v == null) return NotFound("Veículo não encontrado.");

            var placa = _placa.Sanitizar(dto.Placa);
            if (!_placa.EhValida(placa)) return BadRequest("Placa inválida. Formatos aceitos: ABC1234 (antigo) ou ABC1D23 (Mercosul).");
            if (await _db.Veiculos.AnyAsync(x => x.Placa == placa && x.Id != id)) return Conflict("Placa já cadastrada.");

            var clienteExiste = await _db.Clientes.AnyAsync(c => c.Id == dto.ClienteId);
            if (!clienteExiste) return BadRequest("Cliente não encontrado.");

            if (v.ClienteId != dto.ClienteId)
            {
                // DataFim é exclusiva: o novo proprietário começa no mesmo dia
                var hoje = DateTime.UtcNow.Date;

                var histAtual = await _db.VeiculosHistorico
                    .Where(h => h.VeiculoId == id && h.DataFim == null)
                    .FirstOrDefaultAsync();

                if (histAtual != null)
                    histAtual.DataFim = hoje; // proprietário anterior: último período vai até ontem (hoje é exclusivo)

                _db.VeiculosHistorico.Add(new VeiculoHistorico
                {
                    VeiculoId = v.Id,
                    ClienteId = dto.ClienteId,
                    DataInicio = hoje // novo proprietário começa hoje (inclusivo)
                });
            }

            v.Placa = placa;
            v.Modelo = dto.Modelo;
            v.Ano = dto.Ano;
            v.ClienteId = dto.ClienteId;
            await _db.SaveChangesAsync();
            return Ok(v);
        }

        [HttpDelete("{id:guid}")]
        public async Task<IActionResult> Delete(Guid id)
        {
            var v = await _db.Veiculos.FindAsync(id);
            if (v == null) return NotFound("Veículo não encontrado.");
            _db.Veiculos.Remove(v);
            await _db.SaveChangesAsync();
            return NoContent();
        }
    }
}
