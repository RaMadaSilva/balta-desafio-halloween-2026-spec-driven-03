# Pesquisa Técnica: Encurtador de Links

## 1. Persistência e Isolamento dos Slices

**Decisão**: Utilizar `Microsoft.Data.Sqlite` 10.0.14 com ADO.NET e SQL parametrizado. Cada slice
mantém os seus comandos SQL e projeções no próprio ficheiro. `Shared` contém apenas configuração,
fábrica de ligações e inicialização/migrações, sem entidades de domínio ou `DbContext`.

**Rationale**: Uma única tabela e quatro operações SQL simples não justificam um ORM. A abordagem
permite que `CreateLink`, `RedirectLink` e `GetLinkStats` trabalhem sobre a mesma tabela sem
referenciar tipos uns dos outros, cumprindo o isolamento exigido pela constituição.

**Alternativas consideradas**: EF Core foi rejeitado por acrescentar modelos, contexto e migrações sem
benefício neste esquema; Dapper foi rejeitado por ser uma dependência adicional desnecessária.

Fontes: [Microsoft.Data.Sqlite](https://www.nuget.org/packages/Microsoft.Data.Sqlite/10.0.14),
[erros e concorrência](https://learn.microsoft.com/en-us/dotnet/standard/data/sqlite/database-errors).

## 2. Esquema, Datas e Unicidade

**Decisão**: Criar uma tabela `links` com `code TEXT COLLATE BINARY PRIMARY KEY`, `destination`,
`expires_at_utc` opcional e `access_count` com valor inicial zero. Aplicar `STRICT`, `WITHOUT ROWID`
e restrições `CHECK` como defesa adicional. Guardar a expiração como texto ISO-8601 UTC de formato e
precisão fixos.

**Rationale**: A chave primária torna a base de dados a autoridade final sobre unicidade. `BINARY`
preserva a distinção entre maiúsculas e minúsculas. Formato UTC uniforme permite comparações
cronológicas consistentes no SQLite, que não tem tipo nativo de data.

**Alternativas consideradas**: `NOCASE` violaria a distinção de códigos; timestamps Unix seriam válidos,
mas acrescentam decisões de precisão e conversão sem melhorar este contrato; uma tabela de acessos foi
rejeitada porque apenas o total agregado é necessário.

Fontes: [CREATE TABLE](https://www.sqlite.org/lang_createtable.html),
[colações](https://www.sqlite.org/datatype3.html#collating-sequences),
[tipos de data](https://www.sqlite.org/datatype3.html#date-and-time-datatype).

## 3. Geração e Colisões de Códigos

**Decisão**: Gerar cada carácter com `RandomNumberGenerator.GetInt32` sobre o alfabeto ASCII de 62
símbolos. Inserir imediatamente cada candidato; apenas erros de chave primária/única provocam nova
tentativa. Limitar a 32 tentativas e devolver `503` se todas colidirem. Código personalizado duplicado
resulta em `409`, sem regeneração.

**Verificação**: O gerador permanece no slice `CreateLink`, com substituição local nos testes e sem
estado global ou endpoint de teste. Forçar `Taken01` já existente seguido de `Fresh01` livre deve
produzir uma única inserção e preservar o original; 32 colisões consecutivas devem devolver `503`
sem inserção. O ensaio de unicidade de 1000 códigos corre ao nível do handler, separado do limite HTTP.

**Rationale**: O gerador criptográfico evita viés de módulo e códigos previsíveis. A restrição da base
de dados resolve corridas entre criadores; um `SELECT` prévio não garantiria unicidade. O limite evita
ciclos infinitos em falhas ou exaustão inesperada.

**Alternativas consideradas**: `Random`, GUID truncado e bytes com operador módulo foram rejeitados por
previsibilidade ou viés; verificar antes de inserir foi rejeitado por apresentar uma condição de corrida.

Fontes: [RandomNumberGenerator.GetInt32](https://learn.microsoft.com/en-us/dotnet/api/system.security.cryptography.randomnumbergenerator.getint32?view=net-10.0),
[códigos de erro SQLite](https://www.sqlite.org/rescode.html#constraint_primarykey).

## 4. Redirecionamento e Contagem Atómica

**Decisão**: Executar `UPDATE links SET access_count = access_count + 1` apenas quando o código existe
e está ativo, com `RETURNING destination`. Uma linha devolvida autoriza o `302`; nenhuma linha leva a
uma consulta somente para distinguir `404` de `410`. Não usar transação explícita nesta operação.

**Rationale**: Uma única instrução ACID verifica validade, incrementa e devolve o destino sem janela
entre leitura e escrita. A comparação `expires_at_utc > now_utc` trata o instante exato como expirado.
Não existe atualização separada do contador que possa perder acessos concorrentes.

**Alternativas consideradas**: Ler e depois incrementar permitiria contagens incorretas ou estados
inconsistentes; uma transação ao redor da resposta HTTP aumentaria contenção sem tornar o commit e a
receção pelo cliente uma operação distribuída atómica.

Fontes: [SQLite RETURNING](https://www.sqlite.org/lang_returning.html).

## 5. Configuração e Inicialização do SQLite

**Decisão**: Abrir uma ligação por operação com pooling, `Default Timeout=5`, `Foreign Keys=True` e
`Cache=Default`. Ativar WAL no arranque e manter `synchronous` durável predefinido. Versionar migrações
com `PRAGMA user_version`, aplicadas em transação imediata antes de aceitar pedidos.

**Rationale**: Ligações independentes são seguras entre pedidos e o pooling reduz custo. WAL melhora
leitura durante escritas; o timeout gere contenção sem retries ilimitados. Migrações ordenadas tornam
o arranque repetível e detetam versões futuras não suportadas.

**Alternativas consideradas**: Uma ligação singleton foi rejeitada por não ser thread-safe;
`Cache=Shared` com WAL foi rejeitado por ser desaconselhado; apenas `CREATE TABLE IF NOT EXISTS` foi
rejeitado por não detetar estruturas antigas divergentes.

Fontes: [connection strings](https://learn.microsoft.com/en-us/dotnet/standard/data/sqlite/connection-strings),
[WAL](https://www.sqlite.org/wal.html), [user_version](https://www.sqlite.org/pragma.html#pragma_user_version),
[async no provider](https://learn.microsoft.com/en-us/dotnet/standard/data/sqlite/async).

## 6. Contrato de Redirecionamento sem Cache

**Decisão**: Responder `302 Found` com `Location` e os cabeçalhos `Cache-Control: no-store, no-cache`,
`Pragma: no-cache` e `Expires: 0`.

**Rationale**: O redirecionamento temporário e a proibição de armazenamento fazem clientes conformes
voltar à aplicação em cada clique, permitindo revalidar expiração e contar o acesso.

**Alternativas consideradas**: `301` e `308` foram rejeitados por serem permanentes e potencialmente
reutilizados sem contactar o encurtador; `307` é válido, mas preservar o método não oferece benefício
ao fluxo definido com `GET`.

Fontes: [Results.Redirect](https://learn.microsoft.com/en-us/dotnet/api/microsoft.aspnetcore.http.results.redirect?view=aspnetcore-10.0),
[cache de respostas](https://learn.microsoft.com/en-us/aspnet/core/performance/caching/response?view=aspnetcore-10.0).

## 7. Limitação de Criação

**Decisão**: Integrar uma política nomeada apenas em `POST /links` no middleware de rate limiting do
shared framework, particionada por `RemoteIpAddress`. A implementação local em
`Shared/RateLimitingExtensions.cs` mantém até dez instantes monotónicos por IP e toma a decisão
atomicamente por partição. Antes de admitir um pedido, remove instantes com idade maior ou igual a
60 segundos; dez instantes restantes implicam `429`, fila zero e `Retry-After` até libertar a permissão
mais antiga, arredondado para cima em segundos e com mínimo 1. Usar `TimeProvider` substituível nos
testes. Não aplicar limitador global. A configuração de proxy confiável antecede o middleware quando
existir proxy.

**Rationale**: A política protege somente a criação e não afeta redirecionamento ou estatísticas.
O registo dos instantes preserva o requisito exato dos últimos 60 segundos; a expiração em blocos de
uma janela segmentada poderia libertar permissões antecipadamente.

**Alternativas consideradas**: Janelas fixas e segmentadas foram rejeitadas por não garantirem a
fronteira exata de 60 segundos; limitação de todas as rotas prejudicaria links partilhados; persistência
distribuída fica fora do âmbito de uma instância única.

**Verificação**: Com relógio controlado e uma origem não alinhada a segmentos, admitir dez pedidos no
instante t, recusar o 11.º em t e em t+59,999 segundos e admitir novo pedido em t+60 segundos. Verificar
`Retry-After` e independência entre IPs, sem esperas reais nem desativação do middleware.

Fontes: [rate limiting](https://learn.microsoft.com/en-us/aspnet/core/performance/rate-limit?view=aspnetcore-10.0),
[proxies e cabeçalhos forwarded](https://learn.microsoft.com/en-us/aspnet/core/host-and-deploy/proxy-load-balancer?view=aspnetcore-10.0).

## 8. OpenAPI e Scalar

**Decisão**: Usar `Microsoft.AspNetCore.OpenApi` 10.0.14 e `Scalar.AspNetCore` 2.18.3, com `AddOpenApi`,
`MapOpenApi` e `MapScalarApiReference`. Expor `/openapi/v1.json` e `/docs/scalar` em desenvolvimento e fixar
as versões no ficheiro de projeto e no lock file.

**Rationale**: O gerador nativo produz OpenAPI 3.1 e o Scalar fornece a interface interativa exigida,
sem introduzir Swagger UI. A documentação usa uma rota com vários segmentos para preservar códigos
personalizados válidos como `scalar`. Metadados dos endpoints mantêm contratos e documentação sincronizados.

**Alternativas consideradas**: Swashbuckle/Swagger UI foi rejeitado pela constituição e por adicionar
uma cadeia de documentação alternativa sem necessidade.

Fontes: [OpenAPI em ASP.NET Core](https://learn.microsoft.com/en-us/aspnet/core/fundamentals/openapi/aspnetcore-openapi?view=aspnetcore-10.0),
[usar documentos OpenAPI](https://learn.microsoft.com/en-us/aspnet/core/fundamentals/openapi/using-openapi-documents?view=aspnetcore-10.0),
[integração Scalar](https://github.com/scalar/scalar/blob/main/documentation/integrations/aspnetcore/integration.md).

## 9. Estratégia de Testes

**Decisão**: Utilizar xUnit v3 e `Microsoft.AspNetCore.Mvc.Testing` 10.0.14. Testes unitários cobrem
validação e geração; testes HTTP com `WebApplicationFactory<Program>` cobrem contratos, persistência,
concorrência, limitação e Scalar. O cliente de redirecionamento usa `AllowAutoRedirect=false`.

**Cenários de Falha**: Cada endpoint tem testes de `503` em Problem Details por falha reversível na
abertura da ligação, depois da inicialização e antes da escrita ou leitura. Após recuperação, verificar
que criação não inseriu dados, redirecionamento não incrementou e consulta não alterou dados; o
redirecionamento recusado não inclui `Location`. Estes testes não pressupõem que uma falha de I/O após
um commit incerto permita garantir ausência de escrita.

**Desempenho**: Seguir o protocolo de [quickstart.md](quickstart.md), em Release sem depurador, com
relógio real e registo de SDK, sistema operativo, CPU e memória. Excluir aquecimento, medir 100 criações
em dez IPs com dez pedidos por IP e 1000 redirecionamentos com concorrência máxima de 50. Calcular
o percentil 95 pela posição `ceil(0,95 * N)` da amostra ordenada e validar todas as respostas e o total
1000. Não executar outras classes de testes em paralelo com estes ensaios, excluir falhas nem
desativar o limitador. Apenas o transporte de teste atribui os IPs; cabeçalhos arbitrários não passam
a ser confiáveis em produção.

**Rationale**: Testes HTTP exercem o pipeline real e permitem verificar `Location`, ausência de cache,
contagem e respostas de erro sem depender de um destino externo. Cada factory usa uma base de dados
isolada.

As classes de testes correm sequencialmente para não sobrepor ensaios de escrita em disco e distorcer
a carga aplicada ao SQLite. Os testes de concorrência mantêm o seu paralelismo explícito de até
50 pedidos; esta configuração não serializa pedidos na aplicação.

**Alternativas consideradas**: Testar apenas handlers não validaria middleware, rotas ou OpenAPI;
testes que seguem redirecionamentos esconderiam o contrato `302` e poderiam contactar destinos externos.

Fontes: [testes de integração](https://learn.microsoft.com/en-us/aspnet/core/test/integration-tests?view=aspnetcore-10.0),
[testes de Minimal APIs](https://learn.microsoft.com/en-us/aspnet/core/fundamentals/minimal-apis/test-min-api?view=aspnetcore-10.0),
[xUnit v3](https://xunit.net/docs/getting-started/v3/microsoft-testing-platform).

## Limites Operacionais Assumidos

- Uma única instância da aplicação, com base de dados em disco local; não usar réplicas a partilhar
  WAL através de um sistema de ficheiros de rede.
- O limitador por IP é local ao processo; limitação distribuída exigiria um componente adicional.
- Os objetivos de 1000 acessos concorrentes e latência são validados por teste de carga; WAL não
  elimina a serialização de escritas do SQLite.
- Não repetir cegamente atualizações após erros de I/O ou commit incerto, pois isso poderia duplicar
  acessos; erros de indisponibilidade produzem `503` controlado.
