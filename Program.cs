using ControleDeEstoque.Data;
using ControleDeEstoque.Menu;
using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Configuration;
using Serilog;
using Serilog.Events;
using Spectre.Console;

namespace ControleDeEstoque;

class Program
{
    // Array declarativo do menu: número + texto + ação. Elimina o
    // parsing frágil de "int.Parse(escolha.Split(' ')[0])".
    private static readonly (int Num, string Texto, Action<MenuConsole> Acao)[] Menu =
    {
        (1,  "Cadastrar produto",                             m => m.CadastrarProduto()),
        (2,  "Listar todos os produtos",                      m => m.ListarProdutos()),
        (3,  "Buscar produto por nome",                       m => m.BuscarProdutoPorNome()),
        (4,  "Atualizar produto",                             m => m.AtualizarProduto()),
        (5,  "Desativar produto (exclusão lógica)",           m => m.DesativarProduto()),
        (6,  "Listar produtos com estoque abaixo do mínimo",  m => m.ListarEstoqueBaixo()),
        (7,  "Mostrar valor total do estoque",                m => m.MostrarValorTotalEstoque()),
        (8,  "Registrar entrada de estoque",                  m => m.RegistrarEntrada()),
        (9,  "Registrar saída de estoque",                    m => m.RegistrarSaida()),
        (10, "Ver histórico de movimentações",                m => m.ListarMovimentacoes()),
        (11, "Listar produtos desativados",                   m => m.ListarProdutosDesativados()),
        (12, "Reativar produto",                              m => m.ReativarProduto()),
        (13, "Exportar produtos para CSV",                    m => m.ExportarCsv()),
        (0,  "Sair",                                          _ => { })
    };

    static int Main(string[] args)
    {
        ConfigureLogger();
        string connectionString = CarregarConnectionString();

        // Item 12: Ctrl+C salva um backup final e encerra com log limpo.
        Console.CancelKeyPress += (_, e) =>
        {
            e.Cancel = true;
            Log.Information("Interrompido pelo usuário (Ctrl+C)");
            try { FazerBackup(connectionString); } catch { }
            Log.CloseAndFlush();
            AnsiConsole.WriteLine();
            AnsiConsole.MarkupLine("[yellow]Interrompido. Até logo![/]");
            Environment.Exit(0);
        };

        try
        {
            Log.Information("Aplicação iniciada");
            FazerBackup(connectionString);

            if (!EnsureDatabaseSchema(connectionString))
                return 0;

            Run(connectionString);
            Log.Information("Aplicação encerrada normalmente");
            return 0;
        }
        catch (Exception ex)
        {
            Log.Fatal(ex, "Erro fatal na aplicação");
            AnsiConsole.MarkupLine($"[red]Erro fatal: {Markup.Escape(ex.Message)}[/]");
            AnsiConsole.MarkupLine("[grey]Detalhes salvos em logs/.[/]");
            return 1;
        }
        finally
        {
            Log.CloseAndFlush();
        }
    }

    static string CarregarConnectionString()
    {
        var config = new ConfigurationBuilder()
            .SetBasePath(AppContext.BaseDirectory)
            .AddJsonFile("appsettings.json", optional: true)
            .Build();

        return config.GetConnectionString("EstoqueDb") ?? "Data Source=estoque.db";
    }

    static void ConfigureLogger()
    {
        string logDir = Path.Combine(AppContext.BaseDirectory, "logs");
        Directory.CreateDirectory(logDir);

        Log.Logger = new LoggerConfiguration()
            .MinimumLevel.Information()
            .WriteTo.File(
                path: Path.Combine(logDir, "estoque-.log"),
                rollingInterval: RollingInterval.Day,
                outputTemplate: "{Timestamp:yyyy-MM-dd HH:mm:ss} [{Level:u3}] {Message:lj}{NewLine}{Exception}")
            .WriteTo.Console(restrictedToMinimumLevel: LogEventLevel.Warning)
            .CreateLogger();
    }

    static void Run(string connectionString)
    {
        var menu = new MenuConsole(connectionString);

        while (true)
        {
            AnsiConsole.Clear();
            AnsiConsole.Write(new Rule("[yellow]CONTROLE DE ESTOQUE[/]").RuleStyle("grey"));

            // SelectionPrompt genérico no tuple do menu — retorna o item
            // completo, e o UseConverter só controla a exibição.
            var opcao = AnsiConsole.Prompt(
                new SelectionPrompt<(int Num, string Texto, Action<MenuConsole> Acao)>()
                    .Title("[bold]Escolha uma opção (↑ ↓ + Enter):[/]")
                    .PageSize(15)
                    .UseConverter(o => $"{o.Num} - {o.Texto}")
                    .AddChoices(Menu));

            if (opcao.Num == 0) break;

            try
            {
                opcao.Acao(menu);
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Erro ao executar opção {Opcao}", opcao.Num);
                AnsiConsole.MarkupLine($"[red]Erro inesperado: {Markup.Escape(ex.Message)}[/]");
                AnsiConsole.MarkupLine("[grey]Detalhes em logs/. Pressione qualquer tecla...[/]");
                Console.ReadKey();
            }
        }
    }

    static void FazerBackup(string connectionString)
    {
        try
        {
            var builder = new SqliteConnectionStringBuilder(connectionString);
            string dbPath = builder.DataSource;
            if (!File.Exists(dbPath)) return;

            string backupDir = Path.Combine(AppContext.BaseDirectory, "backups");
            Directory.CreateDirectory(backupDir);

            string destino = Path.Combine(backupDir, $"estoque-{DateTime.Now:yyyyMMdd-HHmmss}.db");
            File.Copy(dbPath, destino, overwrite: false);

            Log.Information("Backup criado: {Arquivo}", Path.GetFileName(destino));

            var antigos = Directory.GetFiles(backupDir, "estoque-*.db")
                                   .OrderByDescending(f => f)
                                   .Skip(10)
                                   .ToList();

            foreach (var f in antigos)
            {
                try { File.Delete(f); }
                catch (Exception ex) { Log.Warning(ex, "Falha ao remover backup antigo {Arquivo}", f); }
            }
        }
        catch (Exception ex)
        {
            Log.Warning(ex, "Falha ao criar backup — prosseguindo sem backup");
        }
    }

    static bool EnsureDatabaseSchema(string connectionString)
    {
        using var db = new AppDbContext(connectionString);
        db.Database.EnsureCreated();

        try
        {
            _ = db.Movimentacoes.Any();
            _ = db.Produtos.FirstOrDefault();
            return true;
        }
        catch (SqliteException ex) when (
            ex.Message.Contains("no such table", StringComparison.OrdinalIgnoreCase) ||
            ex.Message.Contains("no such column", StringComparison.OrdinalIgnoreCase))
        {
            AnsiConsole.Clear();
            AnsiConsole.Write(new Rule("[red]BANCO DE DADOS DESATUALIZADO[/]").RuleStyle("red"));
            AnsiConsole.MarkupLine("\n[red]O arquivo estoque.db foi criado com uma versão anterior.[/]");
            AnsiConsole.MarkupLine($"[yellow]Detalhe: {Markup.Escape(ex.Message)}[/]\n");
            AnsiConsole.MarkupLine("Para resolver, apague o arquivo e rode novamente:");
            AnsiConsole.MarkupLine($"[grey]  {Path.GetFullPath("estoque.db")}[/]\n");
            AnsiConsole.MarkupLine("[grey]Pressione qualquer tecla para sair...[/]");
            Console.ReadKey();
            return false;
        }
    }
}