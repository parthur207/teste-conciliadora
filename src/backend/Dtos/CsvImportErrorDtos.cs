namespace Parking.Api.Dtos
{
    public class CsvImportErrorDtos
    {
        public int Linha { get; set; }
        public string Motivo { get; set; } = string.Empty;
    }
}
