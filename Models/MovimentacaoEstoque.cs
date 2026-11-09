namespace ControleDeEstoque.Models;

public enum TipoMovimentacao
{
    Entrada = 1,
    Saida = 2
}

public class MovimentacaoEstoque
{
    public int Id { get; set; }

    public int ProdutoId { get; set; }

    public Produto? Produto { get; set; }

    public TipoMovimentacao Tipo { get; set; }
    public int Quantidade { get; set; }
    public string? Observacao { get; set; }
    public DateTime DataHora { get; set; } = DateTime.Now;
}