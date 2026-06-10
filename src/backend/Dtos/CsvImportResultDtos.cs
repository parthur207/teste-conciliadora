namespace Parking.Api.Dtos
{
    public class CsvImportResultDto
    {
        public int Processados { get; set; }
        public int Inseridos { get; set; }

        public List<CsvImportErrorDtos> Erros { get; set; } = [];

        public int TotalErros => Erros.Count;
    }
}
