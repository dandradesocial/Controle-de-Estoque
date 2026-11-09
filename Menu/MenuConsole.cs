using ControleDeEstoque.Data;
using ControleDeEstoque.Models;
using ControleDeEstoque.Services;
using ControleDeEstoque.Utils;
using Microsoft.EntityFrameworkCore;
using Spectre.Console;

namespace ControleDeEstoque.Menu;

public class MenuConsole
{
    private readonly string _connectionString;

    public MenuConsole(string connectionString) => _connectionString = connectionString;

    private (AppDbContext db, ProdutoService service) CriarContexto()
    {
        var db = new AppDbContext(_connectionString);
        return (db, new ProdutoService(db));
    }

    // ---------------------- CREATE ----------------------
    public void CadastrarProduto()
    {
        ConsoleHelper.MostrarTela("CADASTRAR PRODUTO");

        string sku = ConsoleHelper.LerTextoObrigatorio("SKU (código único): ");
        string nome = ConsoleHelper.LerTextoObrigatorio("Nome: ");
        string desc = ConsoleHelper.LerTexto("Descrição (opcional): ");
        decimal pc = ConsoleHelper.LerDecimalNaoNegativo("Preço de custo: ");
        decimal pv = ConsoleHelper.LerDecimalNaoNegativo("Preço de venda: ");
        int qtd = ConsoleHelper.LerIntNaoNegativo("Quantidade em estoque: ");
        int minimo = ConsoleHelper.LerIntNaoNegativo("Estoque mínimo: ");

        var produto = new Produto
        {
            Sku = sku,
            Nome = nome,
            Descricao = string.IsNullOrWhiteSpace(desc) ? null : desc,
            PrecoCusto = pc,
            PrecoVenda = pv,
            QuantidadeEstoque = qtd,
            EstoqueMinimo = minimo,
            DataCadastro = DateTime.Now
        };

        if (!ValidarProduto(produto)) { ConsoleHelper.Pausar(); return; }

        // Item 7: aviso de prejuízo (não bloqueia, só avisa)
        if (produto.PrecoVenda < produto.PrecoCusto)
        {
            AnsiConsole.MarkupLine("[yellow]⚠ Preço de venda menor que o preço de custo. Confirma? (S/N)[/]");
            if (Console.ReadLine()?.Trim().ToUpper() != "S")
            {
                AnsiConsole.MarkupLine("[yellow]Cadastro cancelado.[/]");
                ConsoleHelper.Pausar();
                return;
            }
        }

        var (db, service) = CriarContexto();
        using (db)
        {
            try
            {
                service.Cadastrar(produto);
                AnsiConsole.MarkupLine(
                    $"[green]✓ Produto '{Markup.Escape(produto.Nome)}' cadastrado! (Id: {produto.Id})[/]");
            }
            catch (DbUpdateException ex) when (ex.InnerException?.Message.Contains("UNIQUE") == true)
            {
                AnsiConsole.MarkupLine("[red]✗ Já existe um produto com esse SKU. Escolha outro.[/]");
            }
        }
        ConsoleHelper.Pausar();
    }

    // ---------------------- READ (todos) ----------------------
    public void ListarProdutos()
    {
        ConsoleHelper.MostrarTela("LISTA DE PRODUTOS");

        var (db, service) = CriarContexto();
        using (db)
        {
            var produtos = service.ListarTodos();
            ConsoleHelper.MostrarTabelaPaginada(produtos, "Produtos ativos");
        }
        ConsoleHelper.Pausar();
    }

    // ---------------------- READ (busca) ----------------------
    public void BuscarProdutoPorNome()
    {
        ConsoleHelper.MostrarTela("BUSCAR PRODUTO POR NOME");

        string termo = ConsoleHelper.LerTextoObrigatorio("Digite parte do nome: ");

        var (db, service) = CriarContexto();
        using (db)
        {
            var produtos = service.BuscarPorNome(termo);
            if (produtos.Count == 0)
                AnsiConsole.MarkupLine($"[yellow]Nenhum produto ativo encontrado com '{Markup.Escape(termo)}'.[/]");
            else
                ConsoleHelper.MostrarTabelaPaginada(produtos, $"Resultados para '{termo}'");
        }
        ConsoleHelper.Pausar();
    }

    // ---------------------- UPDATE ----------------------
    public void AtualizarProduto()
    {
        ConsoleHelper.MostrarTela("ATUALIZAR PRODUTO");

        int id = ConsoleHelper.LerIntPositivo("Id do produto: ");

        var (db, service) = CriarContexto();
        using (db)
        {
            var produto = service.BuscarPorId(id);
            if (produto == null)
            {
                AnsiConsole.MarkupLine($"[red]✗ Produto com Id {id} não encontrado.[/]");
                ConsoleHelper.Pausar();
                return;
            }

            if (!produto.Ativo)
            {
                AnsiConsole.MarkupLine(
                    $"[yellow]Produto '{Markup.Escape(produto.Nome)}' está desativado. Reative-o (opção 12) antes de editar.[/]");
                ConsoleHelper.Pausar();
                return;
            }

            AnsiConsole.MarkupLine("[grey]Produto atual:[/]");
            ConsoleHelper.MostrarProduto(produto);
            AnsiConsole.MarkupLine("[grey]Deixe em branco para manter o valor atual.[/]\n");
            AnsiConsole.MarkupLine("[grey]Para alterar a quantidade em estoque, use as opções 8/9.[/]\n");

            string nome = ConsoleHelper.LerTextoOpcional(
                $"Novo nome [{Markup.Escape(produto.Nome)}]: ") ?? "";
            if (!string.IsNullOrWhiteSpace(nome)) produto.Nome = nome.Trim();

            string desc = ConsoleHelper.LerTextoOpcional(
                $"Nova descrição [{Markup.Escape(produto.Descricao ?? "-")}]: ") ?? "";
            if (!string.IsNullOrWhiteSpace(desc)) produto.Descricao = desc.Trim();

            bool pcChanged = false, pvChanged = false;

            string? pc = ConsoleHelper.LerTextoOpcional($"Novo preço de custo [{produto.PrecoCusto:C}]: ");
            if (!string.IsNullOrWhiteSpace(pc) && ConsoleHelper.TentarLerDecimal(pc, out decimal valPc))
            {
                if (valPc >= 0) { produto.PrecoCusto = valPc; pcChanged = true; }
                else AnsiConsole.MarkupLine("[red]Preço inválido, mantido o anterior.[/]");
            }

            string? pv = ConsoleHelper.LerTextoOpcional($"Novo preço de venda [{produto.PrecoVenda:C}]: ");
            if (!string.IsNullOrWhiteSpace(pv) && ConsoleHelper.TentarLerDecimal(pv, out decimal valPv))
            {
                if (valPv >= 0) { produto.PrecoVenda = valPv; pvChanged = true; }
                else AnsiConsole.MarkupLine("[red]Preço inválido, mantido o anterior.[/]");
            }

            string? em = ConsoleHelper.LerTextoOpcional($"Novo estoque mínimo [{produto.EstoqueMinimo}]: ");
            if (!string.IsNullOrWhiteSpace(em))
            {
                if (int.TryParse(em, out int valEm) && valEm >= 0) produto.EstoqueMinimo = valEm;
                else AnsiConsole.MarkupLine("[red]Estoque mínimo inválido, mantido o anterior.[/]");
            }

            if (!ValidarProduto(produto)) { ConsoleHelper.Pausar(); return; }

            // Item 7: só avisa se o usuário de fato mexeu nos preços
            if ((pcChanged || pvChanged) && produto.PrecoVenda < produto.PrecoCusto)
            {
                AnsiConsole.MarkupLine("[yellow]⚠ Preço de venda menor que o custo. Confirma? (S/N)[/]");
                if (Console.ReadLine()?.Trim().ToUpper() != "S")
                {
                    AnsiConsole.MarkupLine("[yellow]Atualização cancelada.[/]");
                    ConsoleHelper.Pausar();
                    return;
                }
            }

            service.Atualizar(produto);
            AnsiConsole.MarkupLine($"[green]✓ Produto Id {produto.Id} atualizado com sucesso![/]");
        }
        ConsoleHelper.Pausar();
    }

    // ---------------------- DELETE (soft) ----------------------
    public void DesativarProduto()
    {
        ConsoleHelper.MostrarTela("DESATIVAR PRODUTO (exclusão lógica)");
        AnsiConsole.MarkupLine("[grey]O produto sai das listagens, mas seu histórico é preservado.[/]\n");

        int id = ConsoleHelper.LerIntPositivo("Id do produto a desativar: ");

        var (db, service) = CriarContexto();
        using (db)
        {
            var produto = service.BuscarPorId(id);
            if (produto == null)
            {
                AnsiConsole.MarkupLine($"[red]✗ Produto com Id {id} não encontrado.[/]");
                ConsoleHelper.Pausar();
                return;
            }
            if (!produto.Ativo)
            {
                AnsiConsole.MarkupLine($"[yellow]Produto '{Markup.Escape(produto.Nome)}' já está desativado.[/]");
                ConsoleHelper.Pausar();
                return;
            }

            ConsoleHelper.MostrarProduto(produto);

            string? resp = ConsoleHelper.LerTextoOpcional("\nConfirma a desativação? (S/N): ");
            if (resp == null || resp.Trim().ToUpper() != "S")
            {
                AnsiConsole.MarkupLine("[yellow]Desativação cancelada.[/]");
                ConsoleHelper.Pausar();
                return;
            }

            service.Desativar(id);
            AnsiConsole.MarkupLine($"[green]✓ Produto '{Markup.Escape(produto.Nome)}' desativado.[/]");
            AnsiConsole.MarkupLine("[grey]Para reativar, use a opção 12.[/]");
        }
        ConsoleHelper.Pausar();
    }

    // ---------------------- RELATÓRIO: estoque baixo ----------------------
    public void ListarEstoqueBaixo()
    {
        ConsoleHelper.MostrarTela("ESTOQUE ABAIXO DO MÍNIMO");

        var (db, service) = CriarContexto();
        using (db)
        {
            var produtos = service.ListarEstoqueBaixo();
            if (produtos.Count == 0)
                AnsiConsole.MarkupLine("[green]✓ Nenhum produto com estoque baixo. Tudo em ordem![/]");
            else
            {
                ConsoleHelper.MostrarTabelaProdutos(produtos, "⚠ Produtos em alerta");
                AnsiConsole.MarkupLine($"[red bold]Total em alerta: {produtos.Count}[/]");
            }
        }
        ConsoleHelper.Pausar();
    }

    // ---------------------- RELATÓRIO: valor total ----------------------
    public void MostrarValorTotalEstoque()
    {
        ConsoleHelper.MostrarTela("VALOR TOTAL DO ESTOQUE");

        var (db, service) = CriarContexto();
        using (db)
        {
            var (qtdProdutos, totalItens, totalCusto, totalVenda) = service.ObterResumoEstoque();

            if (qtdProdutos == 0)
            {
                AnsiConsole.MarkupLine("[yellow]Nenhum produto ativo cadastrado.[/]");
            }
            else
            {
                var tabela = new Table()
                    .Border(TableBorder.Rounded)
                    .Title("[yellow]Resumo do Estoque[/]")
                    .AddColumn("Métrica")
                    .AddColumn("Valor");

                tabela.AddRow("Produtos ativos", qtdProdutos.ToString());
                tabela.AddRow("Total de itens em estoque", totalItens.ToString());
                tabela.AddRow("Valor total (custo)", totalCusto.ToString("C"));
                tabela.AddRow("Valor total (venda)", totalVenda.ToString("C"));
                tabela.AddRow("Lucro potencial", (totalVenda - totalCusto).ToString("C"));

                AnsiConsole.Write(tabela);
            }
        }
        ConsoleHelper.Pausar();
    }

    // ---------------------- entrada de estoque ----------------------
    public void RegistrarEntrada()
    {
        ConsoleHelper.MostrarTela("REGISTRAR ENTRADA DE ESTOQUE");

        int id = ConsoleHelper.LerIntPositivo("Id do produto: ");
        int quantidade = ConsoleHelper.LerIntPositivo("Quantidade a adicionar: ");
        string obs = ConsoleHelper.LerTexto("Observação (opcional): ");

        var (db, service) = CriarContexto();
        using (db)
        {
            bool ok = service.RegistrarEntrada(
                id, quantidade,
                string.IsNullOrWhiteSpace(obs) ? null : obs);

            if (ok)
                AnsiConsole.MarkupLine($"[green]✓ Entrada registrada: +{quantidade} un. no produto Id {id}.[/]");
            else
                AnsiConsole.MarkupLine($"[red]✗ Produto Id {id} não encontrado ou desativado.[/]");
        }
        ConsoleHelper.Pausar();
    }

    // ---------------------- saída de estoque ----------------------
    public void RegistrarSaida()
    {
        ConsoleHelper.MostrarTela("REGISTRAR SAÍDA DE ESTOQUE");

        int id = ConsoleHelper.LerIntPositivo("Id do produto: ");
        int quantidade = ConsoleHelper.LerIntPositivo("Quantidade a retirar: ");
        string obs = ConsoleHelper.LerTexto("Observação (opcional): ");

        var (db, service) = CriarContexto();
        using (db)
        {
            var (ok, erro) = service.RegistrarSaida(
                id, quantidade,
                string.IsNullOrWhiteSpace(obs) ? null : obs);

            if (ok)
                AnsiConsole.MarkupLine($"[green]✓ Saída registrada: -{quantidade} un. no produto Id {id}.[/]");
            else
                AnsiConsole.MarkupLine($"[red]✗ {Markup.Escape(erro ?? "Erro desconhecido.")}[/]");
        }
        ConsoleHelper.Pausar();
    }

    // ---------------------- histórico ----------------------
    public void ListarMovimentacoes()
    {
        ConsoleHelper.MostrarTela("HISTÓRICO DE MOVIMENTAÇÕES");

        int id = ConsoleHelper.LerIntPositivo("Id do produto: ");

        var (db, service) = CriarContexto();
        using (db)
        {
            var produto = service.BuscarPorId(id);
            if (produto == null)
            {
                AnsiConsole.MarkupLine($"[red]✗ Produto com Id {id} não encontrado.[/]");
                ConsoleHelper.Pausar();
                return;
            }

            AnsiConsole.MarkupLine(
                $"[grey]Produto: [bold]{Markup.Escape(produto.Nome)}[/] (SKU: {Markup.Escape(produto.Sku)})[/]");
            AnsiConsole.MarkupLine($"[grey]Estoque atual: {produto.QuantidadeEstoque} un.[/]\n");

            var movs = service.ListarMovimentacoes(id);

            if (movs.Count == 0)
            {
                AnsiConsole.MarkupLine("[yellow]Nenhuma movimentação registrada.[/]");
            }
            else
            {
                var tabela = new Table()
                    .Border(TableBorder.Rounded)
                    .Title("[yellow]Movimentações[/]")
                    .Expand();

                tabela.AddColumn("Data/Hora");
                tabela.AddColumn("Tipo");
                tabela.AddColumn("Qtd");
                tabela.AddColumn("Observação");

                foreach (var m in movs)
                {
                    string tipo = m.Tipo == TipoMovimentacao.Entrada
                        ? "[green]Entrada[/]" : "[red]Saída[/]";
                    string qtdStr = m.Tipo == TipoMovimentacao.Entrada
                        ? $"+{m.Quantidade}" : $"-{m.Quantidade}";

                    tabela.AddRow(
                        m.DataHora.ToString("dd/MM/yyyy HH:mm:ss"),
                        tipo,
                        qtdStr,
                        Markup.Escape(m.Observacao ?? "-"));
                }

                AnsiConsole.Write(tabela);
                AnsiConsole.MarkupLine($"[grey]Total: {movs.Count} movimentação(ões).[/]");
            }
        }
        ConsoleHelper.Pausar();
    }

    // ---------------------- listar desativados ----------------------
    public void ListarProdutosDesativados()
    {
        ConsoleHelper.MostrarTela("PRODUTOS DESATIVADOS");

        var (db, service) = CriarContexto();
        using (db)
        {
            var desativados = service.ListarDesativados();
            if (desativados.Count == 0)
                AnsiConsole.MarkupLine("[green]✓ Nenhum produto desativado.[/]");
            else
                ConsoleHelper.MostrarTabelaPaginada(desativados, "Produtos desativados");
        }
        ConsoleHelper.Pausar();
    }

    // ---------------------- reativar ----------------------
    public void ReativarProduto()
    {
        ConsoleHelper.MostrarTela("REATIVAR PRODUTO");

        var (db, service) = CriarContexto();
        using (db)
        {
            var desativados = service.ListarDesativados();
            if (desativados.Count == 0)
            {
                AnsiConsole.MarkupLine("[green]✓ Nenhum produto desativado.[/]");
                ConsoleHelper.Pausar();
                return;
            }

            var escolhas = desativados
                .Select(p => $"{p.Id} - {p.Nome} ({p.Sku})")
                .ToList();
            escolhas.Add("✗ Cancelar");

            var escolha = AnsiConsole.Prompt(
                new SelectionPrompt<string>()
                    .Title("[bold]Escolha o produto a reativar:[/]")
                    .PageSize(15)
                    .AddChoices(escolhas));

            if (escolha.StartsWith("✗")) return;

            int id = int.Parse(escolha.Split(' ')[0]);
            service.Reativar(id);

            var produto = service.BuscarPorId(id);
            AnsiConsole.MarkupLine($"[green]✓ Produto '{Markup.Escape(produto!.Nome)}' reativado.[/]");
        }
        ConsoleHelper.Pausar();
    }

    // ---------------------- exportar CSV ----------------------
    public void ExportarCsv()
    {
        ConsoleHelper.MostrarTela("EXPORTAR PRODUTOS PARA CSV");

        var (db, service) = CriarContexto();
        using (db)
        {
            var ativos = service.ListarTodos();
            if (ativos.Count == 0)
            {
                AnsiConsole.MarkupLine("[yellow]Nenhum produto ativo para exportar.[/]");
                ConsoleHelper.Pausar();
                return;
            }

            string arquivo = service.ExportarCsvAtivos();
            AnsiConsole.MarkupLine($"[green]✓ {ativos.Count} produto(s) exportado(s) para:[/]");
            AnsiConsole.MarkupLine($"[grey]{Markup.Escape(arquivo)}[/]");
            AnsiConsole.MarkupLine("[grey]Abra com Excel ou Google Sheets.[/]");
        }
        ConsoleHelper.Pausar();
    }

    // ---------------------- validação ----------------------
    private bool ValidarProduto(Produto produto)
    {
        var ctx = new System.ComponentModel.DataAnnotations.ValidationContext(produto);
        var erros = new List<System.ComponentModel.DataAnnotations.ValidationResult>();

        bool ok = System.ComponentModel.DataAnnotations.Validator.TryValidateObject(
            produto, ctx, erros, validateAllProperties: true);

        if (!ok)
            foreach (var erro in erros)
                AnsiConsole.MarkupLine($"[red]✗ {erro.ErrorMessage}[/]");

        return ok;
    }
}