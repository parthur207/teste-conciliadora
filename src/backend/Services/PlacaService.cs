using System.Text.RegularExpressions;

namespace Parking.Api.Services;

public class PlacaService
{
    public string Sanitizar(string? placa)
    {
        return Regex.Replace(
            placa ?? string.Empty,
            "[^A-Za-z0-9]",
            string.Empty)
            .ToUpperInvariant();
    }

    public bool EhValida(string placa)
    {
        if (string.IsNullOrWhiteSpace(placa))
            return false;

        placa = Sanitizar(placa);

        var placaAntiga = @"^[A-Z]{3}[0-9]{4}$";
        var placaMercosul = @"^[A-Z]{3}[0-9][A-Z][0-9]{2}$";

        return Regex.IsMatch(placa, placaAntiga)
            || Regex.IsMatch(placa, placaMercosul);
    }
}