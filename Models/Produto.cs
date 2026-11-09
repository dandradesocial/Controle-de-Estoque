using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ControleDeEstoque.Models;

public class Produto
{
    public int Id { get; set; }

    /// Código único de negócio (SKU). Ex.: CAN-001.
    [Required(ErrorMessage = "O SKU é obrigatório.")]
    [MaxLength(50, ErrorMessage = "O SKU deve ter no máximo 50 caracteres.")]
    public string Sku { get; set; } = string.Empty;

    [Required(ErrorMessage = "O nome é obrigatório.")]
    [MaxLength(200, ErrorMessage = "O nome deve ter no máximo 200 caracteres.")]
    public string Nome { get; set; } = string.Empty;

    [MaxLength(500, ErrorMessage = "A descrição deve ter no máximo 500 caracteres.")]
    public string? Descricao { get; set; }

    [Range(0, double.MaxValue, ErrorMessage = "Preço de custo não pode ser negativo.")]
    [Column(TypeName = "decimal(18,2)")]
    public decimal PrecoCusto { get; set; }

    [Range(0, double.MaxValue, ErrorMessage = "Preço de venda não pode ser negativo.")]
    [Column(TypeName = "decimal(18,2)")]
    public decimal PrecoVenda { get; set; }

    [Range(0, int.MaxValue, ErrorMessage = "Quantidade em estoque não pode ser negativa.")]
    public int QuantidadeEstoque { get; set; }

    [Range(0, int.MaxValue, ErrorMessage = "Estoque mínimo não pode ser negativo.")]
    public int EstoqueMinimo { get; set; }

    public DateTime DataCadastro { get; set; } = DateTime.Now;

    public bool Ativo { get; set; } = true;

    public List<MovimentacaoEstoque> Movimentacoes { get; set; } = new();
}