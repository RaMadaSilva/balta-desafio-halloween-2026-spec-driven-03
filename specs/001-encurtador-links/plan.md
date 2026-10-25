# Plano de Implementação: Encurtador de Links

**Ramo**: `001-encurtador-links` | **Data**: 2026-10-25 | **Especificação**: [spec.md](spec.md)

**Entrada**: Especificação da funcionalidade em `/specs/001-encurtador-links/spec.md`

**Nota**: Este plano cobre a pesquisa e o desenho; a decomposição em tarefas ocorre em
`/skill:speckit-tasks`.

## Resumo

Implementar uma API pública para criar links curtos com códigos automáticos ou personalizados,
redirecionar visitantes com contagem atómica e consultar o total acumulado, incluindo expiração UTC e
persistência após reinício. A solução usa .NET 10 Minimal APIs em três ficheiros de features isolados,
SQL parametrizado com `Microsoft.Data.Sqlite`, uma única tabela SQLite e infraestrutura transversal em
`Shared`. Scalar documenta e permite testar os endpoints em desenvolvimento.

## Contexto Técnico

**Linguagem/Versão**: C# 14, .NET 10; SDK disponível 10.0.402.

**Dependências Principais**: `Microsoft.Data.Sqlite` 10.0.14, `Microsoft.AspNetCore.OpenApi` 10.0.14,
`Scalar.AspNetCore` 2.18.3, `System.Threading.RateLimiting` no shared framework.

**Armazenamento**: SQLite em disco local, tabela `links`, WAL, pooling, timeout de 5 segundos e
migrações versionadas por `PRAGMA user_version`.

**Configuração Inicial**: `src/UrlShortener.Api/appsettings.json` define `ConnectionStrings:Links`
como `Data Source=links.db`; `ConnectionStrings__Links` permite substituir o valor. Configuração
ausente ou inválida impede o arranque com uma mensagem compreensível.

**Testes**: xUnit v3, `Microsoft.AspNetCore.Mvc.Testing` 10.0.14 e `WebApplicationFactory<Program>`;
testes unitários, de contrato/integração e de carga. Colisões usam geração controlada no teste;
fronteiras temporais usam `TimeProvider` substituível; cada endpoint tem testes de `503` por
indisponibilidade de ligação antes da operação e verificação dos dados após recuperação.

**Plataforma de Destino**: Servidor Windows ou Linux suportado por .NET 10, uma instância e disco local.

**Tipo de Projeto**: Web service HTTP com Minimal APIs, sem controladores.

**Objetivos de Desempenho**: 95% das criações válidas em até 2 segundos e 95% dos redirecionamentos em
até 1 segundo; contagem exata num ensaio de 1000 acessos, incluindo simultaneidade.

**Protocolo de Desempenho**: Release sem depurador, relógio real e ambiente registado; aquecimento
excluído das amostras; 100 criações distribuídas por dez IPs com dez pedidos por IP e 1000
redirecionamentos com concorrência máxima de 50. Medir o pedido completo, ordenar as durações e usar
a posição `ceil(0,95 * N)` para o percentil 95. Exigir todas as respostas esperadas e total final 1000,
sem excluir falhas ou desativar o limitador. Os ensaios correm sem outros testes em paralelo, conforme
o protocolo detalhado em [quickstart.md](quickstart.md).

**Restrições**: Um ficheiro por feature; nenhuma referência entre slices; endpoints sem acesso direto a
dados; SQLite obrigatório; criação limitada a 10 pedidos numa janela móvel exata de 60 segundos por IP;
redirecionamento `302` sem cache; comentários em português e identificadores em inglês.

**Escala/Âmbito**: Três endpoints públicos, uma tabela, uma instância, 1000 criações automáticas e 1000
acessos no ensaio de aceitação; sem autenticação, domínios personalizados, análise geográfica ou edição.

## Verificação da Constituição

*BARREIRA: Tem de ser aprovada antes da pesquisa da Fase 0. Verificar novamente após a conceção da Fase 1.*

| Princípio | Avaliação |
|---|---|
| I. Plataforma e Minimal APIs | **PASS**: .NET 10 e três endpoints Minimal API; cada endpoint valida/delega no handler local, que encapsula o acesso a dados; não existem controladores. |
| II. Vertical Slice por Feature | **PASS**: `CreateLink.cs`, `RedirectLink.cs` e `GetLinkStats.cs` contêm cada um contratos, endpoint, handler e SQL necessário; não há camadas horizontais de serviços ou repositórios. |
| III. Isolamento e Código Comum | **PASS**: nenhum slice importa tipos dos outros; `Shared` contém apenas ligação, migrações e configuração transversal, sem `ShortLink` ou DTOs partilhados. |
| IV. Persistência SQLite | **PASS**: SQL parametrizado reside nos handlers; configuração e inicialização residem em `Shared`; os endpoints HTTP não recebem ligações nem comandos. |
| V. Linguagem e Clareza | **PASS**: os ficheiros e tipos planeados usam inglês; comentários necessários serão em português. |
| VI. Scalar | **PASS**: OpenAPI nativo e Scalar `/docs/scalar` em desenvolvimento; os três endpoints têm metadados e contratos documentados. |

**Resultado antes da Fase 0**: aprovado, sem violações.

**Resultado depois da Fase 1**: aprovado. O modelo, os contratos e o quickstart preservam as mesmas
decisões e não introduzem dependências entre slices, controladores ou acesso HTTP direto à persistência.

## Estrutura do Projeto

### Documentação (desta funcionalidade)

```text
specs/001-encurtador-links/
├── plan.md
├── research.md
├── data-model.md
├── quickstart.md
├── contracts/
│   └── openapi.json
├── checklists/
│   └── requirements.md
└── tasks.md             # Criado apenas por /skill:speckit-tasks
```

### Código-Fonte (raiz do repositório)

```text
src/
├── UrlShortener.sln
├── UrlShortener.Api/
│   ├── UrlShortener.Api.csproj
│   ├── Program.cs
│   ├── appsettings.json
│   ├── Features/
│   │   ├── CreateLink.cs
│   │   ├── RedirectLink.cs
│   │   └── GetLinkStats.cs
│   └── Shared/
│       ├── SqliteConnectionFactory.cs
│       ├── DatabaseInitializer.cs
│       └── RateLimitingExtensions.cs
└── UrlShortener.Api.Tests/
    ├── UrlShortener.Api.Tests.csproj
    ├── CreateLinkTests.cs
    ├── RedirectLinkTests.cs
    ├── GetLinkStatsTests.cs
    └── ApiDocumentationTests.cs
```

**Decisão sobre a Estrutura**: Toda a solução, incluindo a API e o projeto de testes, reside em `src`;
não existe código de aplicação ou de testes fora desse diretório. `Program.cs` apenas compõe serviços,
middleware e mapeamentos expostos pelos três ficheiros em `Features`. Cada slice tem tipos privados e
SQL próprios. `Shared` centraliza somente infraestrutura independente das features. O projeto
`src/UrlShortener.Api.Tests` permanece separado da API e usa bases de dados temporárias isoladas.

## Desenho de Execução

- `POST /links` valida destino, código e expiração UTC; gera códigos com
  `RandomNumberGenerator.GetInt32`; aplica unicidade no `INSERT`; devolve `201`, `400`, `409` ou `503`.
  Uma política com registo monotónico de até dez instantes por IP aplica uma janela móvel exata de
  60 segundos, com decisão atómica, `TimeProvider` substituível e fila zero. Instantes com idade maior
  ou igual a 60 segundos são removidos antes de admitir um pedido; excesso devolve `429` e
  `Retry-After` até libertar a permissão mais antiga, arredondado para cima em segundos, com mínimo 1.
- `GET /{code}` executa `UPDATE ... RETURNING destination` com condição de atividade. Devolve `302` e
  cabeçalhos `no-store`/`no-cache` quando incrementa; caso contrário distingue `404` de `410`.
- `GET /links/{code}/stats` lê código, total, expiração e estado sem exigir que o link esteja ativo.
- `Shared/DatabaseInitializer` ativa WAL e aplica migrações antes de aceitar pedidos. A fábrica cria
  uma ligação por operação; nenhum objeto ADO.NET é singleton.
- OpenAPI `/openapi/v1.json` e Scalar `/docs/scalar` são disponibilizados em desenvolvimento; a rota
  de documentação tem vários segmentos para não colidir com códigos como `scalar`. Erros usam
  `application/problem+json` conforme [contracts/openapi.json](contracts/openapi.json).
