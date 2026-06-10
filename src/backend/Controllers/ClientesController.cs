
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Parking.Api.Data;
using Parking.Api.Dtos;
using Parking.Api.Models;

namespace Parking.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ClientesController : ControllerBase
    {
        private readonly AppDbContext _db;
        public ClientesController(AppDbContext db) => _db = db;

        [HttpGet]
        public async Task<IActionResult> List([FromQuery] int pagina = 1, [FromQuery] int tamanho = 10, [FromQuery] string? filtro = null, [FromQuery] string mensalista = "all")
        {
            var q = _db.Clientes.AsQueryable();

            if (!string.IsNullOrWhiteSpace(filtro))
                q = q.Where(c => c.Nome.Contains(filtro));

            if (mensalista == "true") q = q.Where(c => c.Mensalista);

            if (mensalista == "false") q = q.Where(c => !c.Mensalista);

            var total = await q.CountAsync();
            var itens = await q
                .OrderBy(c => c.Nome)
                .Skip((pagina - 1) * tamanho)
                .Take(tamanho)
                .ToListAsync();

            return itens is null ? NotFound("Nenhum cliente encontrado.") : Ok(new { total, itens });
        }

        [HttpPost]
        public async Task<IActionResult> Create([FromBody] ClienteCreateDto dto)
        {
            if (string.IsNullOrWhiteSpace(dto.Nome))
                return BadRequest("Nome é obrigatório.");

            var nomeFormatado = dto.Nome.ToUpper().Trim();
            var telefoneFormatado = dto.Telefone?.Trim();

            var existe = await _db.Clientes
                .AnyAsync(x => x.Nome.ToUpper().Trim() == nomeFormatado 
                && x.Telefone.Trim() == telefoneFormatado);

            if (existe) return Conflict("Já existe um cliente com esse nome e telefone.");

            var c = new Cliente
            {
                Nome = dto.Nome,
                Telefone = dto.Telefone?.Trim(),
                Endereco = dto.Endereco,
                Mensalista = dto.Mensalista,
                ValorMensalidade = dto.ValorMensalidade,
            };

            _db.Clientes.Add(c);
            await _db.SaveChangesAsync();
            return CreatedAtAction(nameof(GetById), new { id = c.Id }, c);
        }

        [HttpGet("{id:guid}")]
        public async Task<IActionResult> GetById([FromRoute] Guid id)
        {
            var c = await _db.Clientes
                .Include(x => x.Veiculos)
                .FirstOrDefaultAsync(x => x.Id == id);

            return c == null ? NotFound("Cliente não encontrado.") : Ok(c);
        }

        [HttpPut("{id:guid}")]
        public async Task<IActionResult> Update(Guid id, [FromBody] ClienteUpdateDto dto)
        {
            if (string.IsNullOrWhiteSpace(dto.Nome))
                return BadRequest("Nome é obrigatório.");

            var c = await _db.Clientes
                .FindAsync(id);

            if (c == null) return NotFound("Cliente não encontrado.");

            var nomeFormatado = dto.Nome.ToUpper().Trim();
            var telefoneFormatado = dto.Telefone?.Trim();

            var conflito = await _db.Clientes
                .AnyAsync(x =>x.Id != id 
                && x.Nome.ToUpper().Trim() == nomeFormatado 
                && x.Telefone == telefoneFormatado);

            if (conflito) return Conflict("Já existe outro cliente com esse nome e telefone.");

            c.Nome = dto.Nome;
            c.Telefone = dto.Telefone?.Trim();
            c.Endereco = dto.Endereco;
            c.Mensalista = dto.Mensalista;
            c.ValorMensalidade = dto.ValorMensalidade;

            await _db.SaveChangesAsync();
            return Ok(c);
        }

        [HttpDelete("{id:guid}")]
        public async Task<IActionResult> Delete(Guid id)
        {
            var c = await _db.Clientes
                .FindAsync(id);

            if (c == null) return NotFound("Cliente não encontrado.");

            var temVeiculos = await _db.Veiculos
                .AnyAsync(v => v.ClienteId == id);

            if (temVeiculos) return BadRequest("Cliente possui veículos associados. Transfira ou remova os veículos antes de excluir.");

            _db.Clientes.Remove(c);

            await _db.SaveChangesAsync();
            return NoContent();
        }
    }
}
