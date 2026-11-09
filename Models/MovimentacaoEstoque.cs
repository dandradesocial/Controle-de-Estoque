namespace ControleDeEstoque.Models;

// Tipos de movimentação suportados. O valor numérico é o que fica
// salvo na coluna "Tipo" da tabela Movimentacoes.
public enum TipoMovimentacao
{
    Entrada = 1,
    Saida = 2
}

// Registra uma entrada ou saída de estoque de um produto.
// Cada produto tem uma lista dessas — é o histórico dele.
public class MovimentacaoEstoque
{
    public int Id { get; set; }

    // Chave estrangeira para Produto
    public int ProdutoId { get; set; }

    // Propriedade de navegação — permite acessar o Produto a partir
    // da movimentação (ex.: mov.Produto.Nome). O "?" indica que pode
    // estar nula se não usarmos Include ao consultar.
    public Produto? Produto { get; set; }

    public TipoMovimentacao Tipo { get; set; }
    public int Quantidade { get; set; }
    public string? Observacao { get; set; }
    public DateTime DataHora { get; set; } = DateTime.Now;
}