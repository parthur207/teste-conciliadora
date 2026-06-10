using CsvHelper.Configuration.Attributes;

public class VeiculoCsvDto
{
    [Name("placa")]
    public string Placa { get; set; } = string.Empty;

    [Name("modelo")]
    public string Modelo { get; set; } = string.Empty;

    [Name("ano")]
    public string Ano { get; set; } = string.Empty;

    [Name("cliente_nome")]
    public string ClienteNome { get; set; } = string.Empty;

    [Name("cliente_telefone")]
    public string ClienteTelefone { get; set; } = string.Empty;

    [Name("cliente_endereco")]
    public string ClienteEndereco { get; set; } = string.Empty;

    [Name("mensalista")]
    public string Mensalista { get; set; } = string.Empty;

    [Name("valor_mensalidade")]
    public string ValorMensalidade { get; set; } = string.Empty;
}