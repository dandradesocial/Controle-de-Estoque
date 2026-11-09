# ControleDeEstoque

Sistema console de **Controle de Estoque** em **.NET 8**, **EF Core 8**, **SQLite**, **Spectre.Console** e **Serilog**, com histórico completo de movimentações, soft delete, paginação, exportação CSV e backup automático.

Projeto pensado para **iniciantes em C#** que querem ver um sistema funcional e organizado **sem** cair em overengineering (sem Repository, sem UnitOfWork, sem DI container, sem CQRS, sem async/await desnecessário). Cada conceito profissional está aplicado onde faz sentido, com explicação no código.

---

## Sumário

- [Tecnologias](#tecnologias)
- [Estrutura do projeto](#estrutura-do-projeto)
- [Instalação](#instalação)
  - [Via dotnet CLI](#via-dotnet-cli)
  - [Via Visual Studio Community](#via-visual-studio-community)
- [Como executar](#como-executar)
- [Funcionalidades](#funcionalidades)
- [Regras de negócio](#regras-de-negócio)
- [Movimentações de estoque](#movimentações-de-estoque)
- [Soft delete (exclusão lógica)](#soft-delete-exclusão-lógica)
- [Paginação](#paginação)
- [Exportação CSV](#exportação-csv)
- [Backup automático](#backup-automático)
- [Logs](#logs)
- [Arquitetura — e por que é assim](#arquitetura--e-por-que-é-assim)
- [Refinamentos técnicos aplicados](#refinamentos-técnicos-aplicados)
- [Banco de dados e schema](#banco-de-dados-e-schema)
- [Testes](#testes)
- [Solução de problemas](#solução-de-problemas)
- [Próximos passos (opcional)](#próximos-passos-opcional)

---

## Tecnologias

| Camada | Pacote | Versão | Para quê |
|--------|--------|--------|----------|
| Plataforma | .NET | 8.0 | Runtime e SDK |
| ORM | Microsoft.EntityFrameworkCore.Sqlite | 8.0.10 | Acesso a dados + banco SQLite |
| Configuração | Microsoft.Extensions.Configuration.Json | 8.0.10 | Leitura do `appsettings.json` |
| UI Console | Spectre.Console | 0.49.1 | Tabelas, menus, cores |
| Logging | Serilog + Sinks.File + Sinks.Console | 3.1.1 / 5.0.0 / 5.0.1 | Logs rotativos diários |

---

## Estrutura do projeto

```
ControleDeEstoque/
├── ControleDeEstoque.csproj
├── appsettings.json
├── Program.cs
├── Models/
│   ├── Produto.cs
│   └── MovimentacaoEstoque.cs
├── Data/
│   └── AppDbContext.cs
├── Services/
│   └── ProdutoService.cs
├── Menu/
│   └── MenuConsole.cs
├── Utils/
│   └── ConsoleHelper.cs
├── TESTES.md
└── README.md
```

Após a primeira execução, são criadas automaticamente:

```
bin/Debug/net8.0/
├── estoque.db                    ← banco SQLite
├── logs/
│   └── estoque-YYYY-MM-DD.log    ← logs rotativos diários
├── backups/
│   └── estoque-YYYYMMDD-HHmmss.db ← backups automáticos (mantém os 10 mais recentes)
└── exports/
    └── produtos-YYYYMMDD-HHmmss.csv ← CSVs exportados
```

---

## Instalação

### Via dotnet CLI

```bash
# 1. Criar o projeto
dotnet new console -n ControleDeEstoque
cd ControleDeEstoque

# 2. Adicionar os pacotes
dotnet add package Microsoft.EntityFrameworkCore.Sqlite --version 8.0.10
dotnet add package Microsoft.Extensions.Configuration.Json --version 8.0.10
dotnet add package Serilog --version 3.1.1
dotnet add package Serilog.Sinks.Console --version 5.0.1
dotnet add package Serilog.Sinks.File --version 5.0.0
dotnet add package Spectre.Console --version 0.49.1

# 3. Criar as pastas
mkdir Models Data Services Menu Utils
```

Depois copie os arquivos (`Program.cs`, `Models/`, `Data/`, `Services/`, `Menu/`, `Utils/`, `appsettings.json`) para o projeto.

### Via Visual Studio Community

1. **File → New → Project → Console App (.NET 8)**, nome `ControleDeEstoque`.
2. No **Solution Explorer**, clique com o botão direito no **projeto** → **Manage NuGet Packages** → aba **Browse** → instale os 6 pacotes da tabela acima (versões exatas).
3. Crie as pastas `Models`, `Data`, `Services`, `Menu`, `Utils` com **Add → New Folder**.
4. Adicione/copie os arquivos em cada pasta.
5. Clique com o botão direito no projeto → **Add → Existing Item** para adicionar `appsettings.json` na raiz do projeto.
6. **F5** para executar.

> ⚠️ **Atenção ao `Package Manager Console`**: se preferir o PM Console em vez do NuGet UI, o comando correto é `Install-Package Spectre.Console -Version 0.49.1`, **não** `dotnet add package`. E confira se o dropdown **"Default project"** no topo do PM Console aponta para o projeto `ControleDeEstoque`, não para a Solution.

---

## Como executar

```bash
dotnet run
```

Ou, no Visual Studio: **F5**.

Na **primeira execução**, o `estoque.db` é criado automaticamente em `bin/Debug/net8.0/estoque.db` — nenhuma configuração adicional necessária.

### Zerar o banco

Feche o programa e apague `bin/Debug/net8.0/estoque.db`. O próximo `dotnet run` recria tudo do zero. **Nenhum outro arquivo precisa ser apagado** (logs, backups e exports são independentes).

---

## Funcionalidades

| # | Opção | Descrição |
|---|-------|-----------|
| 1 | Cadastrar produto | Nome, SKU único, preços, estoque e mínimo |
| 2 | Listar todos os produtos | Tabela **paginada** (10 por página) |
| 3 | Buscar produto por nome | Case-insensitive, paginada |
| 4 | Atualizar produto | Nome, descrição, preços, estoque mínimo |
| 5 | Desativar produto | **Soft delete** — histórico preservado |
| 6 | Estoque abaixo do mínimo | Só produtos ativos, com destaque em vermelho |
| 7 | Valor total do estoque | Custo, venda e lucro potencial |
| 8 | Registrar entrada | +Qtd no estoque, com observação |
| 9 | Registrar saída | −Qtd, bloqueado se saldo insuficiente |
| 10 | Ver histórico | Timeline de entradas/saídas de um produto |
| 11 | Listar desativados | Produtos em soft delete |
| 12 | Reativar produto | Volta um produto desativado |
| 13 | Exportar CSV | Abre no Excel (semicolon + BOM UTF-8) |
| 0 | Sair | Encerra o programa |

---

## Regras de negócio

- **SKU obrigatório e único** — validado por índice `UNIQUE` no banco e por `DbUpdateException` tratada com mensagem amigável.
- **Nome obrigatório** (máx. 200 caracteres).
- **Preços não-negativos** (`PrecoCusto`, `PrecoVenda`).
- **Quantidades não-negativas** (`QuantidadeEstoque`, `EstoqueMinimo`).
- **DataCadastro** preenchida automaticamente com `DateTime.Now`.
- **Saída nunca deixa saldo negativo** — bloqueada com mensagem `"Estoque insuficiente. Disponível: X un."`.
- **Produto desativado não aceita entrada/saída** — precisa reativar antes.
- **Confirmação obrigatória** antes de desativar.
- **Aviso de prejuízo**: se `PrecoVenda < PrecoCusto`, pede confirmação (não bloqueia — promoções existem).
- **Decimais aceitam vírgula ou ponto** (`10,50` e `10.50` são válidos).
- **Entradas inválidas nunca quebram o programa** — todos os campos numéricos têm loop de validação.

---

## Movimentações de estoque

Toda alteração de saldo passa por uma **movimentação registrada**. Você **não** altera `QuantidadeEstoque` "na unha" — você registra uma entrada ou saída. Isso garante:

### Auditoria completa
Cada movimentação guarda **data/hora, tipo, quantidade e observação** (opcional). Exemplo de histórico:

```
Data/Hora             Tipo     Qtd   Observação
2026-11-09 14:22:10   Saída    -15   Venda PDV
2026-11-09 10:05:33   Entrada  +20   Compra Fornecedor X
```

### Atomicidade
Estoque e movimentação são salvos na **mesma transação**. Ou os dois são gravados, ou nenhum — nunca fica meio-salvo.

### Saldo sempre consistente
A soma de todas as entradas menos todas as saídas **sempre** é igual ao `QuantidadeEstoque` atual. Se houver divergência, é bug.

### Como usar

- **Opção 8** — entrada de estoque (compra, devolução, ajuste positivo)
- **Opção 9** — saída de estoque (venda, perda, ajuste negativo)
- **Opção 10** — histórico de movimentações de um produto

> 🚨 **Por que não editar quantidade direto?** Porque qualquer sistema real precisa responder "por que esse produto tem 3 unidades e ontem tinha 10?". Se você só sobrescreve o número, a resposta se perde. Movimentações guardam a **causa** — não só o efeito.

---

## Soft delete (exclusão lógica)

Produtos **nunca** são apagados do banco. Ao "desativar" (opção 5), a coluna `Ativo` vira `false`. Isso:

- Preserva 100% do histórico fiscal, contábil e de movimentações.
- Permite **reativar** a qualquer momento (opção 12).
- Elimina o problema de "não posso excluir produto com histórico" — a FK `Restrict` só existiria se algum dia alguém adicionar `Remove()` no código; hoje **não existe mais `Remove()`** em lugar nenhum.

### Como funciona na prática

| Ação | Antes | Depois |
|------|-------|--------|
| Desativar | Produto ativo | `Ativo = false`, some das listagens ativas |
| Histórico | Continua acessível | **Continua acessível** (para auditoria) |
| Reativar | — | `Ativo = true`, volta a aparecer |

> 💡 Em sistemas reais (SAP, Totvs, Bling), "excluir produto" **sempre** significa soft delete. O hard delete só existe em scripts administrativos controlados e auditados.

---

## Paginação

Todas as listagens com potencial de crescer (opções 2, 3, 6, 11) exibem **10 itens por página** com navegação por setas (◄ Anterior / Próxima ►). Funciona com **qualquer volume** de dados:

- 1 a 10 produtos → tela única, sem botões de navegação.
- 11 ou mais → páginas com `◄ Anterior` e `Próxima ►`.
- Última página → só `◄ Anterior` e `✗ Fechar listagem`.

---

## Exportação CSV

Opção 13 gera `exports/produtos-YYYYMMDD-HHmmss.csv`. Formato otimizado para **Excel PT-BR**:

- Separador `;` (padrão PT-BR)
- Codificação **UTF-8 com BOM** (mantém acentos)
- Decimal com **vírgula** (cultura `pt-BR` explícita, independente da máquina)
- Coluna calculada `ValorTotalCusto` (PrecoCusto × Quantidade)
- Apenas produtos **ativos**

### Colunas exportadas

```
Id;SKU;Nome;Descricao;PrecoCusto;PrecoVenda;Qtd;Minimo;DataCadastro;ValorTotalCusto
```

### Escapamento

Nomes ou descrições que contenham `;`, `"`, ou quebra de linha são automaticamente escapados com aspas duplas (padrão RFC 4180).

---

## Backup automático

Na **inicialização**, o programa copia `estoque.db` para `backups/estoque-YYYYMMDD-HHmmss.db`. **Mantém os 10 mais recentes** — os mais antigos são removidos automaticamente.

### Características

- **Nunca impede o programa de rodar**: se o backup falhar (disco cheio, permissão), o sistema apenas **loga um warning** e continua.
- **Primeira execução não gera backup** (não há `estoque.db` para copiar).
- **`Ctrl+C` também dispara backup final** antes de encerrar.
- Local: `bin/Debug/net8.0/backups/`

### Restaurar um backup

1. Feche o programa.
2. Copie o arquivo desejado de `backups/` para `bin/Debug/net8.0/estoque.db` (sobrescrevendo).
3. Rode `dotnet run`.

---

## Logs

- **Local**: `bin/Debug/net8.0/logs/estoque-YYYY-MM-DD.log`
- **Rotação**: diária (um arquivo por dia, sem limite de tamanho)
- **Nível arquivo**: `Information` (registra tudo)
- **Nível console**: `Warning` (só erros aparecem no terminal — evita atropelar a UI do Spectre)
- **Registra**: cadastros, atualizações, desativações, reativações, entradas, saídas, exports, backups, erros e **intenções** (antes de cada operação mutante)

### Exemplo de log

```
2026-11-09 14:22:10 [INF] Aplicação iniciada
2026-11-09 14:22:10 [INF] Backup criado: estoque-20261109-142210.db
2026-11-09 14:23:45 [INF] Solicitada entrada: +20 un. no produto Id 3
2026-11-09 14:23:45 [INF] Entrada registrada: +20 un. de Caneta Azul (Id 3). Novo estoque: 70
2026-11-09 14:25:02 [INF] Solicitada saída: -15 un. do produto Id 3
2026-11-09 14:25:02 [INF] Saída registrada: -15 un. de Caneta Azul (Id 3). Novo estoque: 55
2026-11-09 14:26:30 [INF] Solicitada desativação: Id 3
2026-11-09 14:26:30 [INF] Produto desativado: Id 3, Nome Caneta Azul
2026-11-09 14:28:11 [ERR] Erro ao executar opção 9
System.InvalidOperationException: ...
```

### Inspecionar

```powershell
# PowerShell / CMD
type bin\Debug\net8.0\logs\estoque-2026-11-09.log

# Bash
cat bin/Debug/net8.0/logs/estoque-2026-11-09.log
```

---

## Arquitetura — e por que é assim

O projeto **não usa** Repository, UnitOfWork, DI container, CQRS, MediatR ou async/await. Esses padrões têm valor em sistemas grandes e multiusuário, mas em um projeto console de uma entidade eles são **ruído** — o único efeito seria esconder o que realmente está acontecendo.

O que existe:

| Arquivo | Responsabilidade |
|---------|------------------|
| `Program.cs` | Loop principal + configuração (Serilog, backup, verificação de schema) |
| `Menu/MenuConsole.cs` | Cada opção do menu (valida entrada, chama o service, renderiza) |
| `Services/ProdutoService.cs` | Regras de negócio + acesso a dados via EF Core |
| `Data/AppDbContext.cs` | Configuração do DbContext |
| `Models/` | Entidades com DataAnnotations |
| `Utils/ConsoleHelper.cs` | Leitura segura + renderização com Spectre |

**Regra de ouro**: o `MenuConsole` **nunca** acessa o `AppDbContext` diretamente — sempre via `ProdutoService`. Isso mantém uma separação leve (não arquitetural) que facilita migrar para Web API no futuro (o service inteiro se reaproveita).

> 📚 **Para aprender mais**: veja o arquivo `TESTES.md` — ele tem 36 casos manuais que cobrem todos os cenários, incluindo os de erro.

---

## Refinamentos técnicos aplicados

Melhorias de performance e robustez aplicadas após revisão. **Nenhuma altera o comportamento visível do usuário** — todas são internas:

### Performance

- **`AsNoTracking()` nas consultas de leitura** (`ListarTodos`, `BuscarPorNome`, `ListarEstoqueBaixo`, `ListarDesativados`, `ListarMovimentacoes`, resumos). Elimina o overhead de change tracking em consultas que não editam. Com 1.000 produtos, economiza ~1.000 snapshots internos por listagem.

- **`Atualizar` sem `Find()` extra** — a entidade já vem rastreada do `BuscarPorId`, então o `SaveChanges` detecta alterações sem precisar de um roundtrip adicional ao banco.

- **Transação só depois da validação** em `RegistrarEntrada`/`RegistrarSaida` — evita abrir transação para operações que falham rápido (produto inexistente, estoque insuficiente).

### Robustez

- **Validação defensiva no `ProdutoService`** — `Cadastrar` roda `Validator.ValidateObject` internamente. Se algum caller futuro usar o service direto (ignorando o `MenuConsole`), dados inválidos ainda são recusados.

- **`Ctrl+C` tratado** — faz backup final, `Log.CloseAndFlush()` e sai com código `0`.

- **Menu como array declarativo** — `(int Num, string Texto, Action<MenuConsole>)[]` elimina o parsing frágil de `int.Parse(escolha.Split(' ')[0])`. Se o texto mudar, nada quebra.

### Consistência

- **`AnsiConsole.Clear()` em vez de `Console.Clear()`** — mantém o estado interno do Spectre, evitando flicker em alguns terminais.

- **`MostrarTela(titulo)`** centralizado — DRY no padrão "clear + rule" repetido em todos os métodos do menu.

- **CSV com cultura `pt-BR` explícita** — não depende da configuração da máquina. Um PC EN-US exporta os mesmos arquivos que PT-BR.

### Auditoria

- **Logs de intenção** — antes de cada operação mutante, loga `Solicitado ...`. Junto com o log de resultado (`... registrado`), você vê **quem tentou** mesmo quando **falhou**.

- **Aviso de prejuízo** — ao cadastrar/atualizar com `PrecoVenda < PrecoCusto`, pede confirmação. Não bloqueia (promoções existem), mas protege contra erro de digitação.

---

## Banco de dados e schema

### Arquivo

- Nome: `estoque.db`
- Local: `bin/Debug/net8.0/` (diretório de execução)
- Engine: **SQLite** (arquivo único, sem servidor)
- Criação: `db.Database.EnsureCreated()` na primeira execução — **sem migrations**

### Tabelas

**`Produtos`**

| Coluna | Tipo | Notas |
|--------|------|-------|
| Id | INTEGER PK | autoincremento |
| Sku | TEXT(50) | **UNIQUE**, obrigatório |
| Nome | TEXT(200) | obrigatório |
| Descricao | TEXT(500) | nullable |
| PrecoCusto | decimal(18,2) | ≥ 0 |
| PrecoVenda | decimal(18,2) | ≥ 0 |
| QuantidadeEstoque | INTEGER | ≥ 0 |
| EstoqueMinimo | INTEGER | ≥ 0 |
| DataCadastro | TEXT (DateTime) | preenchida na criação |
| Ativo | INTEGER (bool) | default `1` (true), indexado |

**`Movimentacoes`**

| Coluna | Tipo | Notas |
|--------|------|-------|
| Id | INTEGER PK | autoincremento |
| ProdutoId | INTEGER FK | → Produtos.Id (Restrict) |
| Tipo | INTEGER | 1 = Entrada, 2 = Saída |
| Quantidade | INTEGER | > 0 |
| Observacao | TEXT(500) | nullable |
| DataHora | TEXT (DateTime) | preenchida na criação |

### ⚠️ Mudança de schema

Este projeto evoluiu por fases. Se você tem um `estoque.db` **criado em versão anterior** (sem `Movimentacoes` ou sem `Ativo`), o programa **detecta** e mostra:

```
BANCO DE DADOS DESATUALIZADO
O arquivo estoque.db foi criado com uma versão anterior.
Detalhe: SQLite Error 1: 'no such table: Movimentacoes'.

Para resolver, apague o arquivo e rode novamente.
```

**Solução**: apague `bin/Debug/net8.0/estoque.db`. O banco é recriado na próxima execução. A partir da Fase 3, você tem **backup automático** — não perde dados em futuras mudanças.

> 💡 **Se algum dia** o schema precisar evoluir **sem perda de dados**, o próximo passo é adotar **EF Core Migrations** (`dotnet ef migrations add ...`). Enquanto o schema estabilizar, `EnsureCreated()` é mais simples e adequado.

---

## Testes

Veja **`TESTES.md`** — 36 casos manuais com passos e resultado esperado, organizados em:

- CRUD básico (6 casos)
- Soft delete (9 casos)
- Paginação (5 casos)
- CSV (5 casos)
- Backup (5 casos)
- Schema (2 casos)
- Logs (4 casos)

O objetivo é ter uma **prova de qualidade reproduzível** — se você mexer no código daqui a 3 meses, roda os 36 casos e confirma que nada quebrou. É o substituto pragmático para testes automatizados em um projeto desse porte.

> 📌 Se um dia fizer testes automatizados, o alvo é o `ProdutoService` — ele é puro, sem dependência de console, e cada método tem contrato claro (retorno, efeito esperado, exceções).

---

## Solução de problemas

### `"Não foi possível encontrar nenhum projeto em..."` ao instalar pacote

Você está no **Package Manager Console** sem apontar para o projeto correto. Duas soluções:

1. **Confira o dropdown "Default project"** no topo do PM Console — deve mostrar `ControleDeEstoque`, não "Solution".
2. **Use o comando do PM Console**: `Install-Package Spectre.Console -Version 0.49.1` (não `dotnet add package`).
3. **Ou navegue até a pasta do `.csproj`** no terminal e rode `dotnet add package Spectre.Console --version 0.49.1`.

### `"no such table: Movimentacoes"` ou `"no such column: Ativo"` ao rodar

Você tem um `estoque.db` antigo. Apague `bin/Debug/net8.0/estoque.db` e rode novamente.

### Acentos quebrados no Excel

Se o CSV abrir com `Ã§Ã£o` em vez de `ção`, confirme que o arquivo foi salvo como UTF-8. O código usa `UTF8Encoding(true)` (com BOM), que é o padrão que o Excel PT-BR reconhece. Se estiver editando o arquivo manualmente, salve sem BOM e o Excel pode quebrar.

### `dotnet run` não encontra o `appsettings.json`

O `.csproj` deve ter o `ItemGroup` com:

```xml
<None Update="appsettings.json">
  <CopyToOutputDirectory>PreserveNewest</CopyToOutputDirectory>
</None>
```

Se o arquivo não chegar em `bin/Debug/net8.0/`, o programa usa fallback `"Data Source=estoque.db"` — funciona, mas fica sem configuração externa.

### Backup não está sendo criado

- Verifique permissão de escrita em `bin/Debug/net8.0/backups/`.
- **Primeira execução não gera backup** — não há `estoque.db` para copiar. Rode duas vezes.
- Se o log tiver `[WRN] Falha ao criar backup`, veja o detalhe (disco cheio, antivírus bloqueando).

### Menu aparece com caracteres estranhos (`?` ou quadrados)

O terminal precisa suportar Unicode para os símbolos `◄ ► ⚠ ✓ ✗`. No **Windows Terminal** e **VS Code**, funciona. No **cmd.exe** antigo, pode quebrar. Solução: usar o Windows Terminal, ou substituir os símbolos por ASCII nos arquivos `ConsoleHelper.cs` e `Program.cs`.

---

## Próximos passos (opcional)

Se você chegou até aqui, o projeto **está sólido e pronto para uso**. O que **realmente** falta agora não é código — é **usar o sistema por 2-3 semanas** e ver o que a prática pede.

### Se você quiser continuar aprendendo (fases futuras)

| Melhoria | Quando faz sentido |
|----------|-------------------|
| **Motivo e data de desativação** | Se quiser auditoria completa do soft delete (muda schema) |
| **Filtros combinados** | Se o cliente pedir "listar entre R$ X e Y com estoque baixo" |
| **Dashboard Spectre** (`BarChart`) | Se quiser uma tela inicial com gráficos de estoque |
| **EF Core Migrations** | Quando o schema estabilizar de vez — não recriar mais banco |
| **Split do `MenuConsole`** | Se a classe passar de ~600 linhas (hoje está em ~500) |
| **Testes unitários com xUnit** | Quando o `ProdutoService` estabilizar (ele é puro, fácil de testar) |
| **Migrar para Web API** | Se quiser expor o estoque para outros clientes — o `ProdutoService` se reaproveita inteiro |

### O que **NÃO** fazer (mesmo parecendo profissional)

- **Repository pattern** — `DbContext` já é a abstração. Interface só para testar prepara um problema que você não tem.
- **Unit of Work** — já é o `DbContext`.
- **DI container** — você instancia 1 service + 1 context por operação. Container resolve nada.
- **CQRS / MediatR** — 13 operações de menu. Não há dois lados "read" e "write" complexos.
- **`async/await` em tudo** — console com SQLite local: latência zero, código mais difícil de ler.

Se algum dia você **migrar para ASP.NET Core** (Web API, por exemplo), aí sim vale reconsiderar DI, async/await e separação em camadas — porque o contexto muda completamente. Enquanto for console, **o código atual está no ponto certo**.

---

**Bom proveito!** Se algo der errado, os **logs** são seu melhor amigo: `bin/Debug/net8.0/logs/estoque-YYYY-MM-DD.log`.