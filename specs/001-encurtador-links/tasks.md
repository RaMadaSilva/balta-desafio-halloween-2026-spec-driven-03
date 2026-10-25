---

description: "Tarefas para implementação do encurtador de links"
---

# Tarefas: Encurtador de Links

**Entrada**: Documentos de conceção em `/specs/001-encurtador-links/`

**Pré-requisitos**: plan.md, spec.md, research.md, data-model.md, contracts/openapi.json, quickstart.md

**Testes**: Incluídos porque a constituição exige testes aplicáveis e o plano/quickstart define testes
unitários, HTTP, concorrência, persistência, documentação e desempenho. Escrever os testes antes da
implementação correspondente e confirmar primeiro que falham.

**Organização**: Tarefas agrupadas por história. Todo o código, incluindo testes, reside em `src`.
Cada feature fica integralmente num único ficheiro e não referencia outra feature.

## Formato: `[ID] [P?] [Story] Descrição`

- **[P]**: Pode ser executada em paralelo com outras tarefas cujas dependências estejam concluídas.
- **[Story]**: US1 = criação; US2 = redirecionamento; US3 = estatísticas.
- Todos os caminhos são relativos à raiz do repositório.

## Conclusão e Commit por Tarefa (Obrigatório)

- Cada tarefa de T001 a T026 TEM DE originar um commit próprio, imediatamente após a execução e
  validação aplicável. Não agrupar várias tarefas no mesmo commit nem adiar commits para o fim da fase.
- A mensagem do commit TEM DE incluir o ID da tarefa e descrever o resultado, por exemplo:
  `chore: T001 criar solução e projetos .NET 10`.
- Incluir no mesmo commit apenas as alterações da tarefa e a sua marcação `- [x]` neste
  `specs/001-encurtador-links/tasks.md`. Rever o diff preparado e adicionar ficheiros ou trechos
  explicitamente, sem incluir alterações alheias à tarefa.
- A tarefa só é considerada concluída depois de o commit ter sido criado com sucesso e confirmado
  no histórico. Se o commit falhar, repor a marcação `- [ ]` e resolver a falha antes de avançar.
- Nas tarefas de testes prévios à implementação, confirmar e registar a falha esperada antes do
  commit; os testes correspondentes TÊM DE passar nas tarefas de integração T013, T017 e T020.
- Nas tarefas apenas de validação ou auditoria, registar os comandos/verificações e os resultados
  junto da tarefa neste ficheiro, incluindo as limitações encontradas, e incluir esse registo no commit.
- As tarefas `[P]` podem ser executadas em paralelo, mas a atualização deste ficheiro e a preparação
  e criação de commits na mesma árvore de trabalho TÊM DE ocorrer sequencialmente, uma tarefa de cada vez.

## Fase 1: Preparação (Infraestrutura Partilhada)

**Objetivo**: Inicializar a solução .NET 10 e os projetos dentro de `src`

- [x] T001 Criar `src/UrlShortener.sln`, os projetos `src/UrlShortener.Api/UrlShortener.Api.csproj` e `src/UrlShortener.Api.Tests/UrlShortener.Api.Tests.csproj`, adicionar ambos à solução e referenciar a API a partir dos testes com `net10.0`.
- [x] T002 [P] Configurar em `src/UrlShortener.Api/UrlShortener.Api.csproj` os pacotes `Microsoft.Data.Sqlite` 10.0.14, `Microsoft.AspNetCore.OpenApi` 10.0.14 e `Scalar.AspNetCore` 2.18.3, com lock file de restore.
- [x] T003 [P] Configurar xUnit v3 e `Microsoft.AspNetCore.Mvc.Testing` 10.0.14 em `src/UrlShortener.Api.Tests/UrlShortener.Api.Tests.csproj`, com SDK Web e lock file de restore.

---

## Fase 2: Fundações (Pré-requisitos Bloqueantes)

**Objetivo**: Disponibilizar persistência, middleware, documentação e infraestrutura de testes

**⚠️ CRÍTICO**: Nenhuma história começa antes desta fase estar concluída

- [x] T004 [P] Implementar em `src/UrlShortener.Api/Shared/SqliteConnectionFactory.cs` a leitura de `ConnectionStrings:Links` e uma fábrica que abre uma ligação por operação com pooling, `Default Timeout=5`, `Foreign Keys=True`, `Cache=Default` e sem objetos ADO.NET singleton; configurar `ConnectionStrings:Links` com `Data Source=links.db` em `src/UrlShortener.Api/appsettings.json`, permitindo substituição por `ConnectionStrings__Links` e validando configuração ausente ou inválida no arranque.
- [ ] T005 Implementar em `src/UrlShortener.Api/Shared/DatabaseInitializer.cs` a ativação/verificação de WAL e migrações por `PRAGMA user_version`; a v1 cria `links` como `STRICT, WITHOUT ROWID`, com `code TEXT PRIMARY KEY COLLATE BINARY`, `length(code) BETWEEN 4 AND 20`, `code NOT GLOB '*[^0-9A-Za-z]*'`, `destination TEXT NOT NULL` com `length(destination) BETWEEN 1 AND 2048`, `expires_at_utc TEXT NULL` e `access_count INTEGER NOT NULL DEFAULT 0` com `access_count >= 0` (depende de T004).
- [ ] T006 [P] Implementar em `src/UrlShortener.Api/Shared/RateLimitingExtensions.cs` a política `create-links` particionada por `RemoteIpAddress`, com janela móvel exata de 60 segundos, registo monotónico de até 10 instantes por IP e decisão atómica; remover instantes com idade maior ou igual a 60 segundos antes de admitir pedidos, usar `TimeProvider` substituível nos testes, fila zero, resposta `429` em Problem Details e `Retry-After` igual ao tempo até libertar a permissão mais antiga, arredondado para cima em segundos e com mínimo 1; sem aproximação por segmentos nem limitador global.
- [ ] T007 Configurar em `src/UrlShortener.Api/Program.cs` a composição Minimal API, inicialização da base antes de aceitar pedidos, forwarded headers apenas para proxies confiáveis, `UseRateLimiter`, Problem Details, OpenAPI `/openapi/v1.json`, Scalar `/docs/scalar` em desenvolvimento e `public partial class Program`; confirmar arranque com o `appsettings.json` inicial e com `ConnectionStrings__Links` a apontar para uma base temporária (depende de T004, T005 e T006).
- [ ] T008 Criar em `src/UrlShortener.Api.Tests/ApiFactory.cs` uma `WebApplicationFactory<Program>` com base SQLite temporária isolada por teste, `AllowAutoRedirect=false` e apoio para inserir fixtures diretamente; permitir controlar o relógio, atribuir `RemoteIpAddress` no transporte de teste e simular falha reversível na abertura da ligação após inicialização, sem invocar outras features nem desativar o limitador; testes de desempenho usam o relógio real (depende de T003 e T007).

**Ponto de Verificação**: A solução compila, a migração é reproduzível e a factory de testes arranca com Scalar.

---

## Fase 3: História de Utilizador 1 - Criar um Link Curto (Prioridade: P1) 🎯 MVP

**Objetivo**: Criar links com código automático ou personalizado, expiração UTC opcional e limite por IP

**Teste Independente**: Exercer apenas `POST /links`; verificar códigos, respostas, persistência da criação,
validações, duplicados, distinção por maiúsculas e o 11.º pedido, sem depender dos outros endpoints.

### Testes da História de Utilizador 1

> **NOTA: Escrever estes testes primeiro e confirmar que falham antes de T010**

- [ ] T009 [P] [US1] Escrever em `src/UrlShortener.Api.Tests/CreateLinkTests.cs` testes de `201` automático/personalizado, código automático de 7 caracteres, personalizado entre 4 e 20 inclusive, alfabeto ASCII alfanumérico e sensibilidade a maiúsculas, URL absoluta `http`/`https` de 1–2048 caracteres, expiração futura UTC ou nula, `400` e `409`; verificar unicidade de 1000 códigos gerados ao nível do handler, sem sujeitar esse ensaio do handler ao limite HTTP; provocar uma colisão automática com geração controlada (`Taken01` existente seguido de `Fresh01` livre), verificando nova tentativa, uma única inserção e preservação do original, e 32 colisões consecutivas com `503` e nenhuma inserção; simular indisponibilidade de ligação antes da escrita e verificar `503` em Problem Details e dados inalterados após recuperação; testar com relógio controlado dez pedidos no instante t, recusa do 11.º em t e em t+59,999 segundos, admissão em t+60 segundos, `Retry-After` e isolamento entre IPs, usando uma origem temporal não alinhada e sem esperas reais.

### Implementação da História de Utilizador 1

- [ ] T010 [P] [US1] Implementar contratos e validação em `src/UrlShortener.Api/Features/CreateLink.cs`: destino obrigatório, absoluto, `http`/`https` e 1–2048 caracteres; código opcional ASCII alfanumérico de 4–20 caracteres sem normalização; expiração opcional futura em UTC; erros `400` compreensíveis sem escrita (depende de T009).
- [ ] T011 [US1] Implementar o handler e SQL parametrizado em `src/UrlShortener.Api/Features/CreateLink.cs`: gerar 7 caracteres com `RandomNumberGenerator.GetInt32`, permitindo substituição local do gerador nos testes de colisão sem estado global nem endpoint de teste; inserir `access_count=0`, usar `expires_at_utc` no formato `yyyy-MM-ddTHH:mm:ss.fffffffZ`, resolver colisões automáticas em até 32 tentativas, devolver `409` para código personalizado duplicado e `503` após esgotar tentativas ou perante indisponibilidade (depende de T010).
- [ ] T012 [US1] Implementar `POST /links` em `src/UrlShortener.Api/Features/CreateLink.cs` com delegação no handler local, resposta `201` contendo `code`, `shortUrl`, `destination` e `expiresAtUtc`, cabeçalho `Location` para estatísticas, `RequireRateLimiting("create-links")` e metadados OpenAPI de `201`, `400`, `409`, `429` e `503` (depende de T011).
- [ ] T013 [US1] Registar o mapeamento de `CreateLink` em `src/UrlShortener.Api/Program.cs` e executar os testes de `src/UrlShortener.Api.Tests/CreateLinkTests.cs` até todos passarem (depende de T012).

**Ponto de Verificação**: US1 cria e persiste links corretamente sem chamar `RedirectLink` ou `GetLinkStats`.

---

## Fase 4: História de Utilizador 2 - Aceder ao Destino (Prioridade: P2)

**Objetivo**: Redirecionar links ativos e contar cada acesso exatamente uma vez

**Teste Independente**: Inserir fixtures diretamente pela `ApiFactory`, exercer apenas `GET /{code}` e ler
os contadores nas fixtures; validar ativo, expirado, inexistente, cache e concorrência sem usar US1/US3.

### Testes da História de Utilizador 2

> **NOTA: Escrever estes testes primeiro e confirmar que falham antes de T015**

- [ ] T014 [P] [US2] Escrever em `src/UrlShortener.Api.Tests/RedirectLinkTests.cs` testes de `302` e `Location`, incremento único em links com e sem expiração, `410` no instante da expiração sem incremento usando relógio controlado, `404` sem alterações, cabeçalhos `Cache-Control: no-store, no-cache`, `Pragma: no-cache` e `Expires: 0`, distinção por maiúsculas e exatamente 1000 acessos contabilizados em pedidos concorrentes; verificar que o código personalizado `scalar` redireciona e contabiliza também em desenvolvimento; simular indisponibilidade de ligação antes da escrita e verificar `503` em Problem Details, ausência de `Location` e contador inalterado após recuperação.

### Implementação da História de Utilizador 2

- [ ] T015 [P] [US2] Implementar o handler em `src/UrlShortener.Api/Features/RedirectLink.cs` com um único `UPDATE links SET access_count = access_count + 1` parametrizado, condição `expires_at_utc IS NULL OR expires_at_utc > $now_utc`, `COLLATE BINARY`, formato UTC fixo, instante obtido por `TimeProvider` substituível nos testes e `RETURNING destination`; sem transação explícita e com leitura posterior apenas para distinguir `404` de `410` (depende de T014).
- [ ] T016 [US2] Implementar `GET /{code}` em `src/UrlShortener.Api/Features/RedirectLink.cs` com delegação no handler local, `302` apenas após incremento, `Location`, `Cache-Control: no-store, no-cache`, `Pragma: no-cache`, `Expires: 0`, Problem Details para `404`, `410` e `503`, sem autenticação e com metadados OpenAPI (depende de T015).
- [ ] T017 [US2] Registar o mapeamento de `RedirectLink` em `src/UrlShortener.Api/Program.cs` e executar os testes de `src/UrlShortener.Api.Tests/RedirectLinkTests.cs` até todos passarem (depende de T016).

**Ponto de Verificação**: US2 redireciona e conta acessos concorrentes sem importar tipos de US1/US3.

---

## Fase 5: História de Utilizador 3 - Consultar Acessos (Prioridade: P3)

**Objetivo**: Consultar o total acumulado de qualquer link existente, incluindo expirados

**Teste Independente**: Inserir fixtures com totais conhecidos e exercer apenas `GET /links/{code}/stats`;
verificar zero, valores acumulados, expirados, códigos desconhecidos e ausência de escrita.

### Testes da História de Utilizador 3

> **NOTA: Escrever estes testes primeiro e confirmar que falham antes de T019**

- [ ] T018 [P] [US3] Escrever em `src/UrlShortener.Api.Tests/GetLinkStatsTests.cs` testes de `200` com total zero e valores conhecidos, consulta após expiração com `expired: true`, preservação do total, `404` para código desconhecido, distinção por maiúsculas e resposta com `code`, `accessCount`, `expiresAtUtc` e `expired`; simular indisponibilidade de ligação antes da leitura e verificar `503` em Problem Details e dados inalterados após recuperação.

### Implementação da História de Utilizador 3

- [ ] T019 [P] [US3] Implementar o handler em `src/UrlShortener.Api/Features/GetLinkStats.cs` com `SELECT` parametrizado por `code COLLATE BINARY`, leitura de `access_count` e `expires_at_utc`, cálculo de `expired` no instante UTC atual, sem condição de atividade e sem qualquer escrita (depende de T018).
- [ ] T020 [US3] Implementar `GET /links/{code}/stats` em `src/UrlShortener.Api/Features/GetLinkStats.cs`, registar o mapeamento em `src/UrlShortener.Api/Program.cs`, devolver `200` ou Problem Details `404`/`503` com metadados OpenAPI e executar `src/UrlShortener.Api.Tests/GetLinkStatsTests.cs` até todos passarem (depende de T019).

**Ponto de Verificação**: US3 consulta totais sem depender de implementações de US1/US2 e sem alterar dados.

---

## Fase 6: Refinamento e Aspetos Transversais

**Objetivo**: Validar persistência, documentação, desempenho, arquitetura e a solução completa

- [ ] T021 [P] Escrever e executar em `src/UrlShortener.Api.Tests/PersistenceTests.cs` testes que recriam a aplicação sobre a mesma base e comprovam preservação de código, destino, expiração e total, além da continuação do redirecionamento e da contagem.
- [ ] T022 [P] Escrever e executar em `src/UrlShortener.Api.Tests/ApiDocumentationTests.cs` testes de `/openapi/v1.json` e `/docs/scalar`, verificando as três operações, esquemas, respostas e cabeçalhos definidos em `specs/001-encurtador-links/contracts/openapi.json`, incluindo `503` em todos os endpoints e a separação entre a documentação e o código `scalar`.
- [ ] T023 Escrever e executar em `src/UrlShortener.Api.Tests/PerformanceTests.cs` o protocolo de `specs/001-encurtador-links/quickstart.md`: Release sem depurador, relógio real, registo de SDK/sistema operativo/CPU/memória, aquecimento excluído, 100 criações válidas distribuídas por dez IPs de teste distintos com dez pedidos por IP, 1000 redirecionamentos com concorrência máxima de 50 e contador iniciado em zero; medir cada pedido completo, calcular o percentil 95 pela posição `ceil(0,95 * N)` da amostra ordenada, exigir criação até 2 segundos e redirecionamento até 1 segundo, todas as respostas esperadas e total exatamente 1000; não excluir falhas nem desativar o limitador e executar estes ensaios sem outras classes de testes em paralelo.
- [ ] T024 Executar todos os cenários manuais de `specs/001-encurtador-links/quickstart.md`, registar e corrigir divergências de comportamento sem alterar os critérios da especificação (depende de T021, T022 e T023).
- [ ] T025 [P] Auditar `src/UrlShortener.sln` e todos os ficheiros referenciados para confirmar código e testes apenas em `src`, ausência de controladores, uma feature por ficheiro, nenhuma referência entre slices, `Shared` apenas transversal, endpoints sem acesso direto a dados, identificadores em inglês e comentários necessários em português.
- [ ] T026 Executar `dotnet restore`, `dotnet build --configuration Release --no-restore` e `dotnet test --configuration Release --no-build` em `src/UrlShortener.sln`, confirmando restore reproduzível, compilação .NET 10 e zero falhas depois de T024 e T025.

---

## Dependências e Ordem de Execução

### Dependências entre Fases

- **Preparação (Fase 1)**: Sem dependências; T002 e T003 podem avançar em paralelo depois de T001.
- **Fundações (Fase 2)**: Depende da Preparação e bloqueia todas as histórias; T005 depende de T004,
  T007 de T004–T006 e T008 de T003/T007.
- **US1 (Fase 3)**: Depende das Fundações; T009 → T010 → T011 → T012 → T013.
- **US2 (Fase 4)**: Depende apenas das Fundações; T014 → T015 → T016 → T017, com fixtures diretas.
- **US3 (Fase 5)**: Depende apenas das Fundações; T018 → T019 → T020, com fixtures diretas.
- **Refinamento (Fase 6)**: Depende das histórias pretendidas; T024 depende de T021–T023 e T026 de T024/T025.

### Dependências das Histórias de Utilizador

- As histórias são funcionalmente independentes depois da Fase 2; não importam tipos entre si.
- O desenvolvimento pode ocorrer em paralelo nos ficheiros de `Features` e nos respetivos testes.
- T013, T017 e T020 alteram `Program.cs` e TÊM DE ser integrados sequencialmente.
- A ordem de entrega recomendada é US1 → US2 → US3; um MVP utilizável pelo visitante inclui US1 e US2.

### Oportunidades de Paralelização

- T002 + T003: ficheiros de projeto diferentes.
- T004 + T006: infraestrutura independente em ficheiros diferentes.
- T009 + T014 + T018: testes das três histórias em ficheiros diferentes.
- T010 + T015 + T019: handlers/validação das três features em ficheiros diferentes, após os seus testes.
- T021 + T022 + T025: verificações finais independentes depois das histórias; T023 pode ser preparado
  em paralelo, mas os ensaios de desempenho correm isoladamente para evitar interferência nas medições.

## Exemplos de Paralelização

```text
Task: "T009 Escrever testes em src/UrlShortener.Api.Tests/CreateLinkTests.cs"
Task: "T014 Escrever testes em src/UrlShortener.Api.Tests/RedirectLinkTests.cs"
Task: "T018 Escrever testes em src/UrlShortener.Api.Tests/GetLinkStatsTests.cs"

Task: "T021 Validar persistência em src/UrlShortener.Api.Tests/PersistenceTests.cs"
Task: "T022 Validar documentação em src/UrlShortener.Api.Tests/ApiDocumentationTests.cs"
```

Executar T023 separadamente das outras verificações para medir desempenho sem interferência.

## Estratégia de Implementação

### MVP Primeiro

1. Concluir Preparação e Fundações.
2. Entregar US1 e validar criação/persistência de links de forma independente.
3. Entregar US2 para obter o primeiro fluxo utilizável de criação, partilha e redirecionamento.
4. Validar os pontos de verificação antes de avançar.

### Entrega Incremental

1. Fundações prontas → infraestrutura comum e testes disponíveis.
2. US1 → criação válida, códigos únicos e limitação por IP.
3. US2 → redirecionamento sem cache e contagem atómica.
4. US3 → consulta pública dos totais, inclusive após expiração.
5. Refinamento → persistência, Scalar, desempenho, quickstart e solução completa validados.

## Registo de Execução

- **T001**: `dotnet build src/UrlShortener.sln --configuration Release`: dois projetos compilados, zero avisos e erros; ignores .NET/SQLite e diff sem espaços finais verificados.

- **T002**: `dotnet restore` e `dotnet build ... --configuration Release --no-restore` aprovados; lock confirma Microsoft.Data.Sqlite/OpenApi 10.0.14 e Scalar 2.18.3, sem desvios; finais de linha normalizados por .gitattributes.

- **T003**: `dotnet test src/UrlShortener.sln --configuration Release --no-restore` compila ambos os projetos e inicia VSTest; zero testes nesta fase. xUnit v3 4.0.2 com pacote mtp-off preserva o comando previsto; cache NuGet indicado por variável de processo no sandbox.

- **T004**: RED: dois testes falharam antes da implementação. GREEN: suite Release 2/2; configuração obrigatória, caminho relativo, ligações independentes, timeout e foreign keys verificados.
