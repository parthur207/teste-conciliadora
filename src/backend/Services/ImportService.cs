using CsvHelper;
using CsvHelper.Configuration;
using Microsoft.EntityFrameworkCore;
using Parking.Api.Data;
using Parking.Api.Dtos;
using Parking.Api.Interfaces;
using Parking.Api.Models;
using System.Globalization;
using System.Text;

namespace Parking.Api.Services
{
    public class ImportService : IImportService
    {
        private readonly AppDbContext _db;
        private readonly IPlacaService _placa;

        public ImportService(
            AppDbContext db,
            IPlacaService placa)
        {
            _db = db;
            _placa = placa;
        }

        public async Task<CsvImportResultDto> ImportAsync(IFormFile file)
        {
            using var stream = file.OpenReadStream();
            using var reader = new StreamReader(stream, Encoding.UTF8);

            var config = new CsvConfiguration(CultureInfo.InvariantCulture)
            {
                HasHeaderRecord = true,
                TrimOptions = TrimOptions.Trim,
                IgnoreBlankLines = true,
                HeaderValidated = null,
                MissingFieldFound = null
            };

            using var csv = new CsvReader(reader, config);

            var resultado = new CsvImportResultDto();

            int linhaArquivo = 2;

            try
            {
                foreach (var registro in csv.GetRecords<VeiculoCsvDto>())
                {
                    resultado.Processados++;

                    var erro = await ProcessarRegistroAsync(
                        registro,
                        linhaArquivo);

                    if (erro is not null)
                    {
                        resultado.Erros.Add(erro);
                    }
                    else
                    {
                        resultado.Inseridos++;
                    }

                    linhaArquivo++;
                }
            }
            catch (Exception ex)
            {
                resultado.Erros.Add(new CsvImportErrorDtos
                {
                    Linha = linhaArquivo,
                    Motivo = ex.Message
                });
            }

            return resultado;
        }

        private async Task<CsvImportErrorDtos?> ProcessarRegistroAsync(
            VeiculoCsvDto registro,
            int linhaArquivo)
        {
            var placaRaw = registro.Placa?.Trim();
            var modelo = registro.Modelo?.Trim();
            var anoStr = registro.Ano?.Trim();

            var cliNome = registro.ClienteNome?.Trim();
            var cliTelRaw = registro.ClienteTelefone?.Trim();
            var cliEnd = registro.ClienteEndereco?.Trim();

            var mensalistaStr = registro.Mensalista?.Trim();
            var valorMensStr = registro.ValorMensalidade?.Trim();

            var placa = _placa.Sanitizar(placaRaw);

            if (string.IsNullOrWhiteSpace(placa))
            {
                return Erro(linhaArquivo, "Placa não informada.");
            }

            if (!_placa.EhValida(placa))
            {
                return Erro(linhaArquivo,
                    $"Placa inválida: '{placaRaw}'.");
            }

            if (await _db.Veiculos.AnyAsync(v => v.Placa == placa))
            {
                return Erro(linhaArquivo,
                    $"Placa '{placa}' já está cadastrada.");
            }

            if (string.IsNullOrWhiteSpace(cliNome))
            {
                return Erro(linhaArquivo,
                    "Nome do cliente não informado.");
            }

            int? ano =
                int.TryParse(anoStr, out var anoVal)
                    ? anoVal
                    : null;

            var cliTel = new string(
                (cliTelRaw ?? string.Empty)
                .Where(char.IsDigit)
                .ToArray());

            bool mensalista =
                mensalistaStr?.Equals(
                    "true",
                    StringComparison.OrdinalIgnoreCase) == true
                || mensalistaStr == "1";

            decimal? valorMens = null;

            if (!string.IsNullOrWhiteSpace(valorMensStr))
            {
                if (!decimal.TryParse(
                        valorMensStr,
                        NumberStyles.Any,
                        CultureInfo.InvariantCulture,
                        out var vm))
                {
                    return Erro(
                        linhaArquivo,
                        $"Valor de mensalidade inválido: '{valorMensStr}'.");
                }

                valorMens = vm;
            }

            await using var transaction =
                await _db.Database.BeginTransactionAsync();

            try
            {
                var cliente = await ObterOuCriarClienteAsync(
                    cliNome,
                    cliTel,
                    cliEnd,
                    mensalista,
                    valorMens);

                var veiculo = new Veiculo
                {
                    Placa = placa,
                    Modelo = modelo,
                    Ano = ano,
                    Cliente = cliente
                };

                _db.Veiculos.Add(veiculo);

                await _db.SaveChangesAsync();

                _db.VeiculosHistorico.Add(
                    new VeiculoHistorico
                    {
                        ClienteId = cliente.Id,
                        VeiculoId = veiculo.Id,
                        DataInicio = DateTime.UtcNow.Date
                    });

                await _db.SaveChangesAsync();

                await transaction.CommitAsync();

                return null;
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();

                return Erro(
                    linhaArquivo,
                    $"Erro inesperado: {ex.Message}");
            }
        }

        private async Task<Cliente> ObterOuCriarClienteAsync(
            string nome,
            string telefone,
            string endereco,
            bool mensalista,
            decimal? valorMensalidade)
        {
            var cliente = await _db.Clientes
                .FirstOrDefaultAsync(c =>
                    c.Nome == nome &&
                    c.Telefone == telefone);

            if (cliente is not null)
                return cliente;

            cliente = new Cliente
            {
                Nome = nome,
                Telefone = telefone,
                Endereco = endereco,
                Mensalista = mensalista,
                ValorMensalidade = valorMensalidade
            };

            _db.Clientes.Add(cliente);

            return cliente;
        }

        private static CsvImportErrorDtos Erro(
            int linha,
            string motivo)
        {
            return new CsvImportErrorDtos
            {
                Linha = linha,
                Motivo = motivo
            };
        }
    }
}
