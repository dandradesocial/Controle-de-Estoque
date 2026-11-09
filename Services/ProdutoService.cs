using System.ComponentModel.DataAnnotations;
using System.Globalization;
using System.Text;
using ControleDeEstoque.Data;
using ControleDeEstoque.Models;
using Microsoft.EntityFrameworkCore;
using Serilog;

namespace ControleDeEstoque.Services;

/// <summary>
/// Concentra as operações de negócio e de acesso a dados dos produtos.
/// Não é Repository — não tem interface, não tem DI. É apenas organização.
/// </summary>
public class ProdutoService
{
    private readonly AppDbContext _db;

    // pt-BR é a cultura padrão para o CSV: decimal com vírgula, separador ";",
    // compatível com Excel PT-BR. Definida explicitamente para não depender
    // da configuração da máquina onde o programa roda.
    private static readonly CultureInfo CulturaCsv = CultureInfo.GetCultureInfo("pt-BR");

    public ProdutoService(AppDbContext db) => _db = db;

    // ============================ CONSULTAS ============================

    /// <summary>Retorna todos os produtos ativos ordenados por nome.</summary>
    public List<Produto> ListarTodos() =>
        _db.Produtos.AsNoTracking()      // item 1: sem change tracking (só leitura)
            .Where(p => p.Ativo)
            .OrderBy(p => p.Nome)
            .ToList();

    /// <summary>
    /// Busca um produto pelo Id (inclui desativados). Usa tracking porque
    /// o retorno é usado em Atualizar/Reativar/Desativar — o EF precisa
    /// rastrear a entidade para gravar as alterações depois.
    /// </summary>
    public Produto? BuscarPorId(int id) => _db.Produtos.Find(id);

    /// <summary>Busca produtos ativos cujo nome contenha o termo (case-insensitive).</summary>
    public List<Produto> BuscarPorNome(string termo)
    {
        string padrao = $"%{termo}%";
        return _db.Produtos.AsNoTracking()      // item 1
            .Where(p => p.Ativo && EF.Functions.Like(p.Nome, padrao))
            .OrderBy(p => p.Nome)
            .ToList();
    }

    /// <summary>Produtos ativos com quantidade abaixo do estoque mínimo.</summary>
    public List<Produto> ListarEstoqueBaixo() =>
        _db.Produtos.AsNoTracking()      // item 1
            .Where(p => p.Ativo && p.QuantidadeEstoque < p.EstoqueMinimo)
            .OrderBy(p => p.Nome)
            .ToList();

    /// <summary>Produtos desativados (soft delete).</summary>
    public List<Produto> ListarDesativados() =>
        _db.Produtos.AsNoTracking()      // item 1
            .Where(p => !p.Ativo)
            .OrderBy(p => p.Nome)
            .ToList();

    // ============================ CRUD ============================

    /// <summary>Insere um novo produto e salva no banco.</summary>
    public void Cadastrar(Produto produto)
    {
        Log.Information("Solicitado cadastro: SKU {Sku}, Nome {Nome}", produto.Sku, produto.Nome);

        // Item 9: validação defensiva. O MenuConsole já valida, mas se algum
        // outro caller usar o service direto, esta barreira impede dados inválidos.
        var ctx = new ValidationContext(produto);
        Validator.ValidateObject(produto, ctx, validateAllProperties: true);

        _db.Produtos.Add(produto);
        _db.SaveChanges();

        Log.Information("Produto cadastrado: {Nome} (SKU {Sku}, Id {Id})",
            produto.Nome, produto.Sku, produto.Id);
    }

    /// <summary>Persiste alterações em um produto existente.</summary>
    public bool Atualizar(Produto produto)
    {
        Log.Information("Solicitado update: Id {Id}, Nome {Nome}", produto.Id, produto.Nome);

        // Item 3: sem Find() extra. Se veio rastreado (caso normal via BuscarPorId),
        // o EF já detecta as mudanças em SaveChanges. Se por algum motivo veio
        // detached, anexamos como Modified para garantir o UPDATE.
        var entry = _db.Entry(produto);
        if (entry.State == EntityState.Detached)
            entry.State = EntityState.Modified;

        _db.SaveChanges();
        Log.Information("Produto atualizado: Id {Id}, Nome {Nome}", produto.Id, produto.Nome);
        return true;
    }

    /// <summary>Desativa um produto (soft delete). Histórico é preservado.</summary>
    public bool Desativar(int id)
    {
        Log.Information("Solicitada desativação: Id {Id}", id);

        var produto = _db.Produtos.Find(id);
        if (produto == null) return false;

        produto.Ativo = false;
        _db.SaveChanges();
        Log.Information("Produto desativado: Id {Id}, Nome {Nome}", id, produto.Nome);
        return true;
    }

    /// <summary>Reativa um produto previamente desativado.</summary>
    public bool Reativar(int id)
    {
        Log.Information("Solicitada reativação: Id {Id}", id);

        var produto = _db.Produtos.Find(id);
        if (produto == null) return false;

        produto.Ativo = true;
        _db.SaveChanges();
        Log.Information("Produto reativado: Id {Id}, Nome {Nome}", id, produto.Nome);
        return true;
    }

    // ============================ MOVIMENTAÇÕES ============================

    /// <summary>
    /// Registra uma entrada de estoque e atualiza o saldo do produto
    /// na mesma transação atômica.
    /// </summary>
    public bool RegistrarEntrada(int produtoId, int quantidade, string? observacao = null)
    {
        if (quantidade <= 0)
            throw new ArgumentException("Quantidade deve ser maior que zero.", nameof(quantidade));

        Log.Information("Solicitada entrada: +{Qtd} un. no produto Id {Id}", quantidade, produtoId);

        // Item 4: verificação antes de abrir transação. Evita gastar
        // transação com uma validação que pode falhar rápido.
        var produto = _db.Produtos.Find(produtoId);
        if (produto == null || !produto.Ativo) return false;

        using var tx = _db.Database.BeginTransaction();

        produto.QuantidadeEstoque += quantidade;

        _db.Movimentacoes.Add(new MovimentacaoEstoque
        {
            ProdutoId = produtoId,
            Tipo = TipoMovimentacao.Entrada,
            Quantidade = quantidade,
            Observacao = observacao
        });

        _db.SaveChanges();
        tx.Commit();

        Log.Information(
            "Entrada registrada: +{Qtd} un. de {Nome} (Id {Id}). Novo estoque: {Saldo}",
            quantidade, produto.Nome, produto.Id, produto.QuantidadeEstoque);
        return true;
    }

    /// <summary>
    /// Registra uma saída de estoque e atualiza o saldo. Falha se não
    /// houver quantidade suficiente.
    /// </summary>
    public (bool sucesso, string? erro) RegistrarSaida(int produtoId, int quantidade, string? observacao = null)
    {
        if (quantidade <= 0)
            throw new ArgumentException("Quantidade deve ser maior que zero.", nameof(quantidade));

        Log.Information("Solicitada saída: -{Qtd} un. do produto Id {Id}", quantidade, produtoId);

        var produto = _db.Produtos.Find(produtoId);
        if (produto == null)
            return (false, "Produto não encontrado.");
        if (!produto.Ativo)
            return (false, "Produto está desativado. Reative-o antes de movimentar.");
        if (produto.QuantidadeEstoque < quantidade)
            return (false, $"Estoque insuficiente. Disponível: {produto.QuantidadeEstoque} un.");

        using var tx = _db.Database.BeginTransaction();

        produto.QuantidadeEstoque -= quantidade;

        _db.Movimentacoes.Add(new MovimentacaoEstoque
        {
            ProdutoId = produtoId,
            Tipo = TipoMovimentacao.Saida,
            Quantidade = quantidade,
            Observacao = observacao
        });

        _db.SaveChanges();
        tx.Commit();

        Log.Information(
            "Saída registrada: -{Qtd} un. de {Nome} (Id {Id}). Novo estoque: {Saldo}",
            quantidade, produto.Nome, produto.Id, produto.QuantidadeEstoque);
        return (true, null);
    }

    /// <summary>Histórico de movimentações do produto (do mais recente ao mais antigo).</summary>
    public List<MovimentacaoEstoque> ListarMovimentacoes(int produtoId) =>
        _db.Movimentacoes.AsNoTracking()      // item 1
            .Where(m => m.ProdutoId == produtoId)
            .OrderByDescending(m => m.DataHora)
            .ToList();

    // ============================ RELATÓRIOS ============================

    public decimal CalcularValorTotalEstoque()
    {
        var produtos = _db.Produtos.AsNoTracking().Where(p => p.Ativo).ToList();
        return produtos.Sum(p => p.PrecoCusto * p.QuantidadeEstoque);
    }

    public (int qtdProdutos, int totalItens, decimal totalCusto, decimal totalVenda)
        ObterResumoEstoque()
    {
        var produtos = _db.Produtos.AsNoTracking().Where(p => p.Ativo).ToList();
        int totalItens = produtos.Sum(p => p.QuantidadeEstoque);
        decimal totalCusto = produtos.Sum(p => p.PrecoCusto * p.QuantidadeEstoque);
        decimal totalVenda = produtos.Sum(p => p.PrecoVenda * p.QuantidadeEstoque);
        return (produtos.Count, totalItens, totalCusto, totalVenda);
    }

    // ============================ EXPORTAÇÃO ============================

    /// <summary>
    /// Exporta os produtos ativos para um CSV em <c>exports/</c>, com
    /// cultura pt-BR explícita (decimal com vírgula, separador ";").
    /// </summary>
    public string ExportarCsvAtivos()
    {
        Log.Information("Solicitada exportação CSV");
        var produtos = ListarTodos();

        string exportsDir = Path.Combine(AppContext.BaseDirectory, "exports");
        Directory.CreateDirectory(exportsDir);

        string arquivo = Path.Combine(exportsDir, $"produtos-{DateTime.Now:yyyyMMdd-HHmmss}.csv");

        // UTF8Encoding(true) emite BOM — necessário para o Excel reconhecer acentos
        using var sw = new StreamWriter(arquivo, false, new UTF8Encoding(true));

        sw.WriteLine("Id;SKU;Nome;Descricao;PrecoCusto;PrecoVenda;Qtd;Minimo;DataCadastro;ValorTotalCusto");

        foreach (var p in produtos)
        {
            // Item 8: cultura explícita, não CurrentCulture
            sw.WriteLine(string.Join(";", new[]
            {
                p.Id.ToString(),
                EscaparCsv(p.Sku),
                EscaparCsv(p.Nome),
                EscaparCsv(p.Descricao ?? ""),
                p.PrecoCusto.ToString("F2", CulturaCsv),
                p.PrecoVenda.ToString("F2", CulturaCsv),
                p.QuantidadeEstoque.ToString(),
                p.EstoqueMinimo.ToString(),
                p.DataCadastro.ToString("dd/MM/yyyy HH:mm", CulturaCsv),
                (p.PrecoCusto * p.QuantidadeEstoque).ToString("F2", CulturaCsv)
            }));
        }

        Log.Information("CSV exportado: {Arquivo} ({Total} produtos)", arquivo, produtos.Count);
        return arquivo;
    }

    private static string EscaparCsv(string valor)
    {
        if (valor.Contains(';') || valor.Contains('"') || valor.Contains('\n') || valor.Contains('\r'))
            return "\"" + valor.Replace("\"", "\"\"") + "\"";
        return valor;
    }
}