
using CsvHelper;
using CsvHelper.Configuration;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Parking.Api.Data;
using Parking.Api.Interfaces;
using Parking.Api.Models;
using Parking.Api.Services;
using System.Globalization;
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

        [ApiController]
        [Route("api/import")]
        public class ImportController : ControllerBase
        {
            private readonly IImportService _importService;

            public ImportController(IImportService importService)
            {
                _importService = importService;
            }

            [HttpPost("csv")]
            public async Task<IActionResult> ImportCsv([FromForm] IFormFile file)
            {
                if (file is null || file.Length == 0)
                    return BadRequest("Envie um arquivo CSV no campo 'file'.");

                if (!Path.GetExtension(file.FileName)
                    .Equals(".csv", StringComparison.OrdinalIgnoreCase))
                {
                    return BadRequest("Arquivo inválido. Apenas arquivos .csv são permitidos.");
                }

                var resultado = await _importService.ImportAsync(file);

                return Ok(resultado);
            }
        }
    }
}
