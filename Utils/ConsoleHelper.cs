using System.Globalization;
using ControleDeEstoque.Models;
using Spectre.Console;

namespace ControleDeEstoque.Utils;

public static class ConsoleHelper
{
    // ---------------------- LEITURA SEGURA ----------------------

    public static string LerTextoObrigatorio(string mensagem)
    {
        while (true)
        {
            Console.Write(mensagem);
            string? entrada = Console.ReadLine();
            if (!string.IsNullOrWhiteSpace(entrada)) return entrada.Trim();
            AnsiConsole.MarkupLine("[red]Este campo é obrigatório.[/]");
        }
    }

    public static string LerTexto(string mensagem)
    {
        Console.Write(mensagem);
        return Console.ReadLine()?.Trim() ?? string.Empty;
    }

    public static string? LerTextoOpcional(string mensagem)
    {
        Console.Write(mensagem);
        return Console.ReadLine();
    }

    public static bool TentarLerDecimal(string entrada, out decimal valor)
    {
        string normalizado = entrada.Trim().Replace(",", ".");
        return decimal.TryParse(normalizado, NumberStyles.Number,
                                CultureInfo.InvariantCulture, out valor);
    }

    public static decimal LerDecimalNaoNegativo(string mensagem)
    {
        while (true)
        {
            Console.Write(mensagem);
            string? entrada = Console.ReadLine();
            if (entrada != null && TentarLerDecimal(entrada, out decimal valor))
            {
                if (valor < 0) { AnsiConsole.MarkupLine("[red]O valor não pode ser negativo.[/]"); continue; }
                return valor;
            }
            AnsiConsole.MarkupLine("[red]Valor inválido. Digite um número decimal (ex: 10,50).[/]");
        }
    }

    public static int LerIntPositivo(string mensagem)
    {
        while (true)
        {
            Console.Write(mensagem);
            string? entrada = Console.ReadLine();
            if (int.TryParse(entrada, out int valor) && valor > 0) return valor;
            AnsiConsole.MarkupLine("[red]Digite um número inteiro maior que zero.[/]");
        }
    }

    public static int LerIntNaoNegativo(string mensagem)
    {
        while (true)
        {
            Console.Write(mensagem);
            string? entrada = Console.ReadLine();
            if (int.TryParse(entrada, out int valor) && valor >= 0) return valor;
            AnsiConsole.MarkupLine("[red]Digite um número inteiro maior ou igual a zero.[/]");
        }
    }

    public static void Pausar(string mensagem = "Pressione qualquer tecla para voltar...")
    {
        AnsiConsole.MarkupLine($"[grey]{mensagem}[/]");
        Console.ReadKey();
    }

    // ---------------------- HELPER DE TELA (item 6) ----------------------

    public static void MostrarTela(string titulo)
    {
        AnsiConsole.Clear();
        AnsiConsole.Write(new Rule($"[yellow]{titulo}[/]").RuleStyle("grey"));
    }

    // ---------------------- RENDERIZAÇÃO ----------------------

    public static void MostrarTabelaProdutos(IEnumerable<Produto> produtos, string titulo = "Produtos")
    {
        var lista = produtos.ToList();
        if (lista.Count == 0)
        {
            AnsiConsole.MarkupLine("[yellow]Nenhum produto encontrado.[/]");
            return;
        }

        var tabela = new Table()
            .Border(TableBorder.Rounded)
            .Title($"[yellow]{titulo}[/]")
            .Expand();

        tabela.AddColumn("Id");
        tabela.AddColumn("SKU");
        tabela.AddColumn("Nome");
        tabela.AddColumn("Custo");
        tabela.AddColumn("Venda");
        tabela.AddColumn("Qtd");
        tabela.AddColumn("Mín.");
        tabela.AddColumn("Status");

        foreach (var p in lista)
        {
            string status;
            if (!p.Ativo)
                status = "[grey]inativo[/]";
            else if (p.QuantidadeEstoque < p.EstoqueMinimo)
                status = "[red]⚠ BAIXO[/]";
            else
                status = "[green]OK[/]";

            tabela.AddRow(
                p.Id.ToString(),
                Markup.Escape(p.Sku),
                Markup.Escape(p.Nome),
                p.PrecoCusto.ToString("C"),
                p.PrecoVenda.ToString("C"),
                p.QuantidadeEstoque.ToString(),
                p.EstoqueMinimo.ToString(),
                status);
        }

        AnsiConsole.Write(tabela);
    }

    public static void MostrarTabelaPaginada(
        IEnumerable<Produto> produtos, string titulo, int itensPorPagina = 10)
    {
        var lista = produtos.ToList();
        if (lista.Count == 0)
        {
            AnsiConsole.MarkupLine("[yellow]Nenhum produto encontrado.[/]");
            return;
        }

        int totalPaginas = (int)Math.Ceiling(lista.Count / (double)itensPorPagina);
        int paginaAtual = 1;

        while (true)
        {
            AnsiConsole.Clear();
            var itens = lista.Skip((paginaAtual - 1) * itensPorPagina).Take(itensPorPagina);
            MostrarTabelaProdutos(itens, $"{titulo} (página {paginaAtual}/{totalPaginas})");
            AnsiConsole.MarkupLine($"[grey]Total: {lista.Count} produto(s).[/]");

            if (totalPaginas <= 1) return;

            var acoes = new List<string>();
            if (paginaAtual > 1) acoes.Add("◄ Anterior");
            if (paginaAtual < totalPaginas) acoes.Add("Próxima ►");
            acoes.Add("✗ Fechar listagem");

            var escolha = AnsiConsole.Prompt(
                new SelectionPrompt<string>()
                    .Title($"[grey]Página {paginaAtual} de {totalPaginas}. Escolha:[/]")
                    .AddChoices(acoes));

            if (escolha == "◄ Anterior") paginaAtual--;
            else if (escolha == "Próxima ►") paginaAtual++;
            else return;
        }
    }

    public static void MostrarProduto(Produto p)
    {
        string linhaAtivo = p.Ativo ? "[green]Sim[/]" : "[grey]Não[/]";

        var painel = new Panel(
            new Markup(
                $"[bold]Id:[/] {p.Id}\n" +
                $"[bold]SKU:[/] {Markup.Escape(p.Sku)}\n" +
                $"[bold]Nome:[/] {Markup.Escape(p.Nome)}\n" +
                $"[bold]Descrição:[/] {Markup.Escape(p.Descricao ?? "-")}\n" +
                $"[bold]Preço de custo:[/] {p.PrecoCusto:C}\n" +
                $"[bold]Preço de venda:[/] {p.PrecoVenda:C}\n" +
                $"[bold]Quantidade em estoque:[/] {p.QuantidadeEstoque}\n" +
                $"[bold]Estoque mínimo:[/] {p.EstoqueMinimo}\n" +
                $"[bold]Data de cadastro:[/] {p.DataCadastro:dd/MM/yyyy HH:mm}\n" +
                $"[bold]Ativo:[/] {linhaAtivo}"))
            .Border(BoxBorder.Rounded)
            .BorderColor(p.Ativo
                ? (p.QuantidadeEstoque < p.EstoqueMinimo ? Color.Red : Color.Grey)
                : Color.Grey);

        AnsiConsole.Write(painel);

        if (p.Ativo && p.QuantidadeEstoque < p.EstoqueMinimo)
            AnsiConsole.MarkupLine("[red bold]>>> ESTOQUE ABAIXO DO MÍNIMO! <<<[/]");
    }
}