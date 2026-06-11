# Parking Manager – Teste Técnico Full Stack

Sistema de gestão de estacionamento desenvolvido como solução para o desafio técnico Full Stack, contemplando correções, evolução de funcionalidades existentes e implementação de novas regras de negócio.

---

# Objetivo

Evolução de uma aplicação já existente responsável pelo gerenciamento de:

- Clientes
- Veículos
- Mensalistas
- Faturamento
- Importação de dados via CSV

Além da implementação das funcionalidades solicitadas, foram realizadas melhorias visando:

- Maior robustez das validações
- Melhor experiência do usuário
- Maior rastreabilidade das informações
- Melhor manutenção futura do sistema
- Correção de limitações arquiteturais identificadas durante a análise do código


# Link do Back-End em arquitetura moderna (Clean Architecture): https://github.com/parthur207/teste-conciliadora---Clean-Arch

---

# Tecnologias Utilizadas

| Camada | Tecnologia |
|----------|------------|
| Backend | ASP.NET Core 8 |
| ORM | Entity Framework Core 8 |
| Banco de Dados | PostgreSQL |
| Frontend | React + Vite |
| Gerenciamento de Estado | React Query |
| Navegação | React Router |
| Linguagem | C# / JavaScript |

---

# Como Executar

## Banco de Dados

Criar o banco PostgreSQL:

```sql
CREATE DATABASE parking_test;
```

Executar o script de carga inicial:

```bash
psql -h localhost -U postgres -d parking_test -f scripts/seed.sql
```

# OU:

Executar a criação do banco de dados via migration pelo EF Core.

# Caminho: [text](src/backend/Migrations)

* Execução via terminal pelo comando "dotnet ef database update". 
* É importante que a connection string ja esteja devidamente configurada e apontada para seu ambiente.

Configuração padrão:

```text
Host=localhost;
Port=5432;
Database=parking_test;
Username=postgres;
Password=postgres;
```

---

## Backend

```bash
cd src/backend

dotnet restore

dotnet run
```

Swagger:

```text
http://localhost:5000/swagger
```

---

## Frontend

```bash
cd src/frontend

npm install

npm run dev
```

Aplicação:

```text
http://localhost:5173
```

---

# Funcionalidades Implementadas

## 1. Edição Completa de Clientes

### Problema

A aplicação permitia apenas o cadastro de clientes, impossibilitando a manutenção dos dados posteriormente.

### Solução

Foi implementado fluxo completo de edição contemplando:

- Nome
- Telefone
- Endereço
- Status de mensalista
- Valor da mensalidade

Além disso, foi adicionada validação de unicidade para:

```text
Nome + Telefone
```

evitando cadastros duplicados.

### Benefícios

- Integridade dos dados
- Redução de duplicidades
- Melhor experiência operacional

---

## 2. Edição Completa de Veículos

### Problema

Os veículos não podiam ser alterados após o cadastro.

### Solução

Implementação de edição para:

- Modelo
- Ano
- Cliente associado

Também foi realizada validação de existência do cliente antes da alteração.

### Benefícios

- Flexibilidade operacional
- Consistência referencial
- Menor risco de dados órfãos

---

## 3. Evolução da Importação CSV

### Problema

As mensagens de erro retornadas pela importação eram genéricas e dificultavam a identificação da causa da falha.

Exemplo:

```text
Linha 3: erro ao processar registro
```

### Solução

O processamento passou a gerar erros estruturados contendo:

- Linha do arquivo
- Motivo exato da falha

Exemplos:

```json
{
  "linha": 12,
  "motivo": "Placa já cadastrada."
}
```

```json
{
  "linha": 7,
  "motivo": "Quantidade de colunas inválida."
}
```

Também foram implementadas validações adicionais:

- Quantidade de colunas
- Placa
- Cliente
- Valor da mensalidade
- Duplicidade

### Benefícios

- Diagnóstico mais rápido
- Menor retrabalho
- Melhor experiência para o usuário

---

## 4. Implementação do Faturamento Proporcional

### Problema

O sistema faturava sempre o valor integral da mensalidade, independentemente da data de entrada ou saída do cliente.

Isso gerava cobranças incorretas quando ocorria troca de titularidade durante o mês.

### Solução

Foi criada uma estrutura de histórico de relacionamento entre:

```text
Cliente ←→ Veículo
```

através da tabela:

```text
VeiculoHistorico
```

Permitindo registrar:

- Data de início da associação
- Data de encerramento da associação

Com isso, o faturamento passou a considerar apenas os dias efetivamente utilizados.

### Exemplo

| Cliente | Período |
|----------|----------|
| Cliente A | 01/09 a 10/09 |
| Cliente B | 11/09 a 30/09 |

Resultado:

```text
Cliente A → cobrança proporcional a 10 dias

Cliente B → cobrança proporcional a 20 dias
```

### Benefícios

- Precisão financeira
- Rastreabilidade histórica
- Possibilidade de auditoria futura

---

# Extensões Implementadas Além do Escopo

Durante o desenvolvimento foram identificadas oportunidades de melhoria que não estavam explicitamente descritas no desafio.

## Histórico de Proprietários

A troca de cliente em um veículo agora gera histórico automático.

Isso permite:

- Consultar proprietários anteriores
- Reprocessar faturamentos antigos
- Auditar movimentações

---

## Melhorias de Performance nas Consultas

Foram eliminados cenários de consultas excessivas (N+1 Query) através de carregamentos controlados e projeções específicas no Entity Framework.

### Benefícios

- Menor quantidade de consultas ao banco
- Melhor desempenho
- Menor consumo de recursos

---

## Padronização de Mensagens de Erro

Todos os fluxos críticos passaram a retornar mensagens mais claras e orientadas ao usuário.

Exemplos:

- Cliente não encontrado
- Veículo não encontrado
- Cliente já cadastrado
- Placa inválida
- Arquivo CSV inválido

---

## Melhorias de Experiência do Usuário

Foram adicionados:

- Formulários de edição completos
- Feedback visual para erros
- Melhor tratamento de falhas na importação
- Mensagens descritivas de validação

---

# Principais Decisões Técnicas

## Utilização de Histórico para Associação Cliente x Veículo

Ao invés de apenas substituir o cliente associado diretamente no veículo, foi criada uma entidade de histórico.

### Motivos

- Preservação dos dados históricos
- Suporte ao faturamento proporcional
- Possibilidade de auditoria
- Facilidade para futuras evoluções

---

## Datas de Vigência

Foi adotado o modelo:

```text
DataInicio → Inclusiva

DataFim → Exclusiva
```

Exemplo:

```text
Cliente A
01/09 → 11/09

Cliente B
11/09 → Atual
```

Essa abordagem evita sobreposição de períodos e elimina ambiguidades no dia da troca de titularidade.

---

## Evolução sem Quebra de Compatibilidade

As alterações foram implementadas preservando o funcionamento das funcionalidades já existentes.

O objetivo foi permitir a evolução da aplicação sem exigir grandes mudanças estruturais ou impactar fluxos previamente existentes.

---

# Melhorias Futuras

- Testes unitários
- Testes de integração
- Docker Compose
- Migrations automatizadas
- Histórico visual de alterações
- Paginação de listagens
- Autenticação e autorização
- Logs estruturados
- Observabilidade
- Processamento assíncrono da importação CSV

---

# Considerações Finais

A solução buscou não apenas atender às tarefas propostas, mas também corrigir limitações estruturais que poderiam impactar a manutenção e evolução da aplicação no futuro.

As implementações foram realizadas priorizando:

- Legibilidade
- Manutenibilidade
- Escalabilidade
- Integridade dos dados
- Qualidade da experiência do usuário

Além das funcionalidades solicitadas, foram incorporadas melhorias arquiteturais que aumentam a robustez do sistema e fornecem uma base mais adequada para futuras evoluções.
