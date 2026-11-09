using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ControleDeEstoque.Models;

/// <summary>
/// Entidade que representa um produto no estoque.
/// Produtos nunca são removidos fisicamente — são <b>desativados</b>
/// (soft delete) para preservar o histórico de movimentações e auditoria.
/// </summary>
public class Produto
{
    public int Id { get; set; }

    /// <summary>Código único de negócio (SKU). Ex.: CAN-001.</summary>
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

    /// <summary>
    /// Indica se o produto está ativo. Produtos desativados não aparecem
    /// nas listagens normais, mas continuam no banco (soft delete).
    /// </summary>
    public bool Ativo { get; set; } = true;

    public List<MovimentacaoEstoque> Movimentacoes { get; set; } = new();
}