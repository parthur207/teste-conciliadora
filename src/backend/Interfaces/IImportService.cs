using Parking.Api.Dtos;

namespace Parking.Api.Interfaces
{
    public interface IImportService
    {
        Task<CsvImportResultDto> ImportAsync(IFormFile file);
    }
}
