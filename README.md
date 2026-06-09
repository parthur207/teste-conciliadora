# Parking – Teste Técnico Full Stack

Sistema de gestão de estacionamento com cadastro de clientes, veículos, importação CSV e faturamento proporcional.

---

## Stack

| Camada | Tecnologia |
|---|---|
| Backend | .NET 8 Web API + EF Core 8 + PostgreSQL |
| Frontend | React 18 (Vite) + React Query + React Router |
| Banco | PostgreSQL (sem containers; direto via `appsettings.json`) |

---

## Como executar o projeto

### Pré-requisitos

- .NET 8 SDK
- Node.js 18+
- PostgreSQL 13+ rodando localmente

### 1. Banco de dados

Crie o banco e execute o seed inicial:

```bash
psql -h localhost -U postgres -d parking_test -f scripts/seed.sql
```

> No Windows sem WSL, execute o script pelo DBeaver ou pgAdmin.

A string de conexão padrão é:
```
Host=localhost;Port=5432;Database=parking_test;Username=postgres;Password=postgres
```
Ajuste em `src/backend/appsettings.json` se necessário.

> **Nota:** a tabela `veiculo_historico` é criada automaticamente na primeira execução da API (via SQL idempotente em `Program.cs`). Nenhuma migração manual é necessária.

### 2. Backend

```bash
cd src/backend
dotnet restore
dotnet run
```

API disponível em `http://localhost:5000`.  
Swagger em `http://localhost:5000/swagger`.

### 3. Frontend

```bash
cd src/frontend
npm install
npm run dev
```

Aplicação em `http://localhost:5173`.  
Para apontar para outra URL de API: `VITE_API_URL=http://meuhost:5000 npm run dev`.

---

## Implementações realizadas

### Tarefa 1 – Edição de clientes

**Backend (`ClientesController.cs`):**
- Validação de `Nome` obrigatório no `POST` e `PUT`.
- Verificação de unicidade da combinação `Nome + Telefone` no `PUT`, excluindo o próprio registro (evita falso positivo ao salvar sem mudanças).
- Mensagens de erro em português amigáveis (`400 Bad Request`, `404 Not Found`, `409 Conflict`).

**Frontend (`ClientesPage.jsx`):**
- Botão "Editar" em cada linha da tabela abre um formulário inline pré-preenchido com os dados atuais.
- Campos editáveis: Nome, Telefone, Endereço, Mensalista (checkbox), Valor Mensalidade.
- Exibição de erros retornados pela API diretamente acima do formulário.
- Linha editada destacada visualmente.
- Confirmação antes de excluir.
- Tabela expandida com colunas Endereço e Mensalidade.

---

### Tarefa 2 – Edição de veículos

**Backend (`VeiculosController.cs`):**
- `GET /api/veiculos` agora inclui `clienteNome` no retorno via projeção (sem N+1).
- Validação de cliente existente no `POST` e `PUT`.
- Rastreamento de troca de proprietário: quando `clienteId` muda no `PUT`, o registro histórico atual é fechado (`DataFim = hoje`) e um novo é aberto (`DataInicio = hoje`).

**Frontend (`VeiculosPage.jsx`):**
- Substituído o `prompt()` nativo por formulário React completo com estado gerenciado.
- Seleção de cliente via `<select>` com todos os campos disponíveis.
- Nome do cliente exibido na tabela (em vez do UUID).
- Filtro de listagem por cliente.
- Confirmação antes de excluir.
- Erros da API exibidos acima do formulário.

---

### Tarefa 3 – Importação CSV

**Backend (`ImportController.cs`):**

**Antes:**
```
Linha 3: Placa inválida (raw='ABC,Honda,2020,...')
```

**Depois:**
```json
{ "linha": 3, "motivo": "Placa inválida: 'XPTO999'." }
{ "linha": 7, "motivo": "Número de colunas inválido (esperado 9, encontrado 5)." }
{ "linha": 12, "motivo": "Placa 'ABC1234' já está cadastrada." }
{ "linha": 15, "motivo": "Valor de mensalidade inválido: 'abc'." }
```

Melhorias:
- Validação explícita do número de colunas (falha precoce, antes de tentar acessar `cols[8]`).
- Cada tipo de erro tem sua própria mensagem descritiva.
- Dados brutos do CSV removidos das mensagens de erro.
- `decimal.Parse` usa `InvariantCulture` para aceitar `150.00` independente do locale do servidor.
- Retorno inclui `totalErros` além do array `erros`.
- Criação de histórico de veículo registrada na importação.

**Frontend (`CsvUploadPage.jsx`):**
- Painel de estatísticas: Processados / Inseridos / Erros com cores semânticas.
- Cada erro exibido como card com badge "Linha N" e descrição do motivo.
- Feedback de loading durante o upload.
- Exibição do formato esperado do CSV.

---

### Tarefa 4 – Faturamento proporcional

**Novo modelo (`VeiculoHistorico.cs`):**

```
veiculo_historico
├── id (PK)
├── veiculo_id (FK → veiculo)
├── cliente_id (FK → cliente)
├── data_inicio (inclusiva: primeiro dia do proprietário)
└── data_fim (exclusiva: dia em que o veículo saiu; null = proprietário atual)
```

**Semântica de datas (exclusiva no fim):**

| Proprietário | DataInicio | DataFim | Dias em setembro |
|---|---|---|---|
| Cliente A | 2025-09-01 | 2025-09-11 | 10 (01/09–10/09) |
| Cliente B | 2025-09-11 | null | 20 (11/09–30/09) |
| **Total** | | | **30** ✓ |

**Fórmula:**
```
taxa_diária = valor_mensalidade / dias_no_mês
dias = DataFim (ou 1º dia do próximo mês) - max(DataInicio, início do mês)
valor = taxa_diária × dias
```

**`FaturamentoService.cs`:**
- Lê `VeiculoHistorico` para encontrar todos os períodos de posse que se sobrepõem ao mês de competência.
- Cálculo feito com `.Date` para eliminar imprecisão intra-dia.
- Valor arredondado para 2 casas decimais.
- `Observacao` na fatura descreve o cálculo.
- Idempotência mantida.

**`Program.cs`:**
- Tabela `veiculo_historico` criada com `CREATE TABLE IF NOT EXISTS` na inicialização.
- Veículos existentes recebem registro inicial automático com `DataInicio = data_inclusao` (idempotente via `WHERE NOT EXISTS`).

**`FaturasController.cs`:**
- `GET /api/faturas` agora retorna `clienteNome` via `JOIN` (sem N+1).
- `Observacao` incluída na resposta.

**Frontend (`FaturamentoPage.jsx`):**
- Nome do cliente exibido na tabela.
- Observação do cálculo exibida.
- Feedback de loading e mensagem quando não há faturas.
- Nota de "BUG proposital" removida.

---

## Decisões técnicas

### Por que `VeiculoHistorico` em vez de uma coluna `data_inicio` no `Veiculo`?

Um veículo pode trocar de dono múltiplas vezes. Uma única coluna registraria apenas o estado atual. A tabela de histórico permite auditar todas as transferências e calcular faturamento correto para qualquer período retroativo.

### Por que `DataFim` exclusiva?

Com semântica exclusiva, quando o veículo é transferido no dia D:
- Proprietário anterior: `DataFim = D` → não inclui o dia D
- Novo proprietário: `DataInicio = D` → inclui o dia D

Isso elimina ambiguidade no dia da transferência e garante que `D_anterior + D_posterior = total_dias_no_mês` sem sobreposição.

### Por que SQL direto em `Program.cs` em vez de migration EF Core?

O projeto não tinha infra de migrations configurada. Usar `EnsureCreated()` sobrescreveria o banco existente. A abordagem `CREATE TABLE IF NOT EXISTS` + `INSERT ... WHERE NOT EXISTS` é idempotente, incremental e não quebra dados pré-existentes – adequada para evoluir um banco em uso sem CI/CD.

### Por que N+1 no EF Core foi evitado?

- `VeiculosController.List`: usa projeção com `Select` (não carrega a entidade completa) em vez de `Include` + serialização.
- `FaturasController.List`: usa `JOIN` no LINQ em vez de subquery por linha.
- `FaturamentoService`: busca históricos por cliente em uma única query por iteração.

### Por que validações no controller e não em `FluentValidation`?

O projeto referencia `FluentValidation` no `.csproj` mas não o usa. Para manter consistência com o padrão existente (validações inline no controller), não foi introduzida nova infraestrutura. As validações são simples o suficiente para não justificarem uma camada extra.

---

## Melhorias futuras

| Área | Melhoria |
|---|---|
| **Segurança** | Autenticação JWT + RBAC |
| **Migrations** | Configurar `dotnet ef migrations` para controle formal do schema |
| **CSV** | Suporte a delimitador configurável (`;`) e encoding diferente de UTF-8 |
| **CSV** | Importação em lote com transação única (rollback parcial por linha) |
| **Faturamento** | Endpoint para consultar histórico de posse de um veículo |
| **Faturamento** | Cancelamento/estorno de faturas |
| **Faturamento** | Fatura pro rata de novos mensalistas (baseada em `DataInclusao`) |
| **Frontend** | Paginação na listagem de veículos |
| **Frontend** | Toast notifications em vez de `alert()` para erros de exclusão |
| **Testes** | Testes unitários para `FaturamentoService` (cobertura do cálculo proporcional) |
| **Testes** | Testes de integração para os endpoints de CSV e faturamento |
| **Infra** | Docker Compose com PostgreSQL para onboarding zero-config |
| **EF Core** | Índice em `veiculo_historico(cliente_id, data_inicio, data_fim)` para otimizar queries de faturamento |

---

## Estrutura de arquivos alterados

```
src/
├── backend/
│   ├── Models/
│   │   └── VeiculoHistorico.cs          [NOVO] histórico de propriedade de veículo
│   ├── Data/
│   │   └── AppDbContext.cs              [MOD] + DbSet<VeiculoHistorico> + mapping
│   ├── Program.cs                       [MOD] inicialização idempotente da tabela
│   ├── Controllers/
│   │   ├── ClientesController.cs        [MOD] validação + unicidade no PUT
│   │   ├── VeiculosController.cs        [MOD] histórico + clienteNome + validações
│   │   ├── FaturasController.cs         [MOD] clienteNome no retorno via JOIN
│   │   └── ImportController.cs         [MOD] erros estruturados + validação de colunas
│   └── Services/
│       └── FaturamentoService.cs        [MOD] faturamento proporcional por dias
└── frontend/
    └── src/pages/
        ├── ClientesPage.jsx             [MOD] formulário de edição + erros + mais colunas
        ├── VeiculosPage.jsx             [MOD] edição com cliente + clienteNome na tabela
        ├── FaturamentoPage.jsx          [MOD] clienteNome + observação + UX
        └── CsvUploadPage.jsx            [MOD] display visual de erros linha a linha
```
