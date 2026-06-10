
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Parking.Api.Data;
using Parking.Api.Models;
using Parking.Api.Services;
using System.Text;

namespace Parking.Api.Controllers
{
    [ApiController]
    [Route("api/import")]
    public class ImportController : ControllerBase
    {
        private readonly AppDbContext _db;
        private readonly PlacaService _placa;
        public ImportController(AppDbContext db, PlacaService placa) { _db = db; _placa = placa; }

        [HttpPost("csv")]
        public async Task<IActionResult> ImportCsv(IFormFile file)
        {
            if (file is null || file.Length == 0)
                return BadRequest("Envie um arquivo CSV no campo 'file'.");

            if (!Path.GetExtension(file.FileName)
                .Equals(".csv", StringComparison.OrdinalIgnoreCase))
            {
                return BadRequest("Arquivo inválido. Apenas arquivos .csv são permitidos.");
            }

            using var stream = file.OpenReadStream();
            using var reader = new StreamReader(stream, Encoding.UTF8);

            int linhaArquivo = 0;
            int processados = 0;
            int inseridos = 0;
            var erros = new List<object>();

            await reader.ReadLineAsync();

            while (!reader.EndOfStream)
            {
                linhaArquivo++;
                var raw = await reader.ReadLineAsync();
                if (string.IsNullOrWhiteSpace(raw)) continue;
                processados++;

                var cols = raw.Split(',');

                if (cols.Length < 9)
                {
                    erros.Add(new { linha = linhaArquivo, motivo = $"Número de colunas inválido (esperado 9, encontrado {cols.Length})." });
                    continue;
                }

                var placaRaw = cols[0].Trim();
                var modelo = cols[1].Trim();
                var anoStr = cols[2].Trim();
                var cliNome = cols[4].Trim();
                var cliTelRaw = cols[5].Trim();
                var cliEnd = cols[6].Trim();
                var mensalistaStr = cols[7].Trim();
                var valorMensStr = cols[8].Trim();

                var placa = _placa.Sanitizar(placaRaw);

                if (string.IsNullOrWhiteSpace(placa))
                {
                    erros.Add(new { linha = linhaArquivo, motivo = "Placa não informada." });
                    continue;
                }

                if (!_placa.EhValida(placa))
                {
                    erros.Add(new { linha = linhaArquivo, motivo = $"Placa inválida: '{placaRaw}'." });
                    continue;
                }

                if (await _db.Veiculos.AnyAsync(v => v.Placa == placa))
                {
                    erros.Add(new { linha = linhaArquivo, motivo = $"Placa '{placa}' já está cadastrada." });
                    continue;
                }

                if (string.IsNullOrWhiteSpace(cliNome))
                {
                    erros.Add(new { linha = linhaArquivo, motivo = "Nome do cliente não informado." });
                    continue;
                }

                int? ano = int.TryParse(anoStr, out var anoVal) ? anoVal : null;

                var cliTel = new string(cliTelRaw.Where(char.IsDigit).ToArray());

                bool mensalista = bool.TryParse(mensalistaStr, out var mBool) && mBool;

                decimal? valorMens = null;
                if (!string.IsNullOrWhiteSpace(valorMensStr))
                {
                    if (!decimal.TryParse(valorMensStr, System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out var vm))
                    {
                        erros.Add(new { linha = linhaArquivo, motivo = $"Valor de mensalidade inválido: '{valorMensStr}'." });
                        continue;
                    }
                    valorMens = vm;
                }

                try
                {
                    var cliente = await _db.Clientes
                        .FirstOrDefaultAsync(c => c.Nome == cliNome && c.Telefone == cliTel);

                    if (cliente == null)
                    {
                        cliente = new Cliente
                        {
                            Nome = cliNome,
                            Telefone = cliTel,
                            Endereco = cliEnd,
                            Mensalista = mensalista,
                            ValorMensalidade = valorMens
                        };
                        _db.Clientes.Add(cliente);
                        await _db.SaveChangesAsync();
                    }

                    var veiculo = new Veiculo { Placa = placa, Modelo = modelo, Ano = ano, ClienteId = cliente.Id };
                    _db.Veiculos.Add(veiculo);

                    _db.VeiculosHistorico.Add(new VeiculoHistorico
                    {
                        VeiculoId = veiculo.Id,
                        ClienteId = cliente.Id,
                        DataInicio = DateTime.UtcNow.Date
                    });

                    await _db.SaveChangesAsync();
                    inseridos++;
                }
                catch (Exception ex)
                {
                    erros.Add(new { linha = linhaArquivo, motivo = $"Erro inesperado: {ex.Message}" });
                }
            }

            return Ok(new { processados, inseridos, totalErros = erros.Count, erros });
        }
    }
}
