namespace Parking.Api.Interfaces
{
    public interface IPlacaService
    {
        string Sanitizar(string? placa);
        bool EhValida(string placa);
    }
}
