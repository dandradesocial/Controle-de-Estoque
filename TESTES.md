\# Casos de Teste — Controle de Estoque



Execute os casos abaixo na ordem para validar o sistema após cada alteração.

Use `dotnet run` na pasta do projeto. Para "zerar", apague `bin/Debug/net8.0/estoque.db`.



| # | Cenário | Passos | Resultado Esperado |

|---|---------|--------|--------------------|

| 1 | Cadastro válido | Opção 1 → SKU `CAN-001`, Nome `Caneta Azul`, Custo `1,50`, Venda `3,00`, Qtd `50`, Mín `10` | Mensagem verde "cadastrado com Id 1" |

| 2 | SKU duplicado | Opção 1 → repetir SKU `CAN-001` | Erro "Já existe um produto com esse SKU" |

| 3 | Nome vazio | Opção 1 → deixar Nome em branco | Loop "Este campo é obrigatório" até preencher |

| 4 | Preço negativo | Opção 1 → Custo `-5` | Erro "não pode ser negativo", repete a pergunta |

| 5 | Quantidade negativa | Opção 1 → Qtd `-1` | Erro "maior ou igual a zero", repete |

| 6 | Decimal com vírgula | Opção 1 → Custo `10,50` | Aceita, salva como 10.50 |

| 7 | Decimal com ponto | Opção 1 → Custo `10.50` | Aceita, salva como 10.50 |

| 8 | Texto em campo numérico | Opção 1 → Custo `abc` | Erro "número decimal", repete |

| 9 | Listagem | Opção 2 | Tabela com todos os produtos, status verde |

| 10 | Busca case-insensitive | Opção 3 → `caneta` (minúsculo) | Encontra `Caneta Azul` |

| 11 | Busca sem resultado | Opção 3 → `zzz` | "Nenhum produto encontrado" |

| 12 | Estoque baixo | Opção 1 → 2º produto com Qtd `2` e Mín `10`, depois Opção 6 | Tabela vermelha com o produto em alerta |

| 13 | Valor total | Opção 7 | Tabela com custo, venda, lucro potencial |

| 14 | Atualização parcial | Opção 4 → Id existente, deixar campos em branco, alterar só Qtd para `20` | "atualizado com sucesso", tabela reflete |

| 15 | Atualização de Id inexistente | Opção 4 → Id `999` | "Produto não encontrado" |

| 16 | Exclusão cancelada | Opção 5 → Id existente, responder `N` | "Exclusão cancelada", produto continua na lista |

| 17 | Exclusão confirmada | Opção 5 → Id existente, responder `S` | Produto removido da tabela |

| 18 | Menu inválido | (já não ocorre, o `SelectionPrompt` só permite opções válidas) | — |

| 19 | Persistência | Fechar o programa (opção 0), rodar de novo, opção 2 | Produtos cadastrados continuam lá |

| 20 | Banco recriado | Fechar, apagar `estoque.db`, rodar de novo, opção 2 | "Nenhum produto cadastrado" |

