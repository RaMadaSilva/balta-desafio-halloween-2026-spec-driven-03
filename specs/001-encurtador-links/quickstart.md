# Guia de Validação: Encurtador de Links

Este guia valida os comportamentos de [spec.md](spec.md) através do contrato em
[contracts/openapi.json](contracts/openapi.json) e das regras de [data-model.md](data-model.md).

## Pré-requisitos

- SDK .NET 10 (10.0.402 ou superior compatível).
- Git Bash para os comandos `curl`, `seq` e `xargs` indicados.
- Diretório local com permissão de escrita para a base SQLite.

Na raiz do repositório:

```bash
dotnet restore src/UrlShortener.sln
dotnet build src/UrlShortener.sln --configuration Release --no-restore
dotnet test src/UrlShortener.sln --configuration Release --no-build
```

Resultado esperado: compilação e todos os testes concluídos sem falhas.

## Executar a API

A configuração `ConnectionStrings:Links` em `src/UrlShortener.Api/appsettings.json` deve ter o valor
inicial `Data Source=links.db`, apontando para um ficheiro local gravável no diretório de conteúdo da
aplicação. A variável de ambiente `ConnectionStrings__Links` permite substituir esse valor.
Confirmar o arranque primeiro com o valor inicial e depois com essa variável a apontar para uma base
temporária; configuração ausente ou inválida deve impedir o arranque com mensagem compreensível.

```bash
ASPNETCORE_ENVIRONMENT=Development dotnet run --project src/UrlShortener.Api/UrlShortener.Api.csproj --no-launch-profile --urls http://localhost:5080
```

No primeiro arranque, a inicialização deve criar a versão 1 do esquema e ativar WAL sem erros. Abrir
`http://localhost:5080/docs/scalar` e confirmar que os três endpoints estão visíveis. O documento deve estar
disponível em `http://localhost:5080/openapi/v1.json`.

## 1. Criação Automática e Personalizada

```bash
curl -i -X POST http://localhost:5080/links \
  -H 'Content-Type: application/json' \
  -d '{"destination":"https://example.com/a/very/long/address"}'

curl -i -X POST http://localhost:5080/links \
  -H 'Content-Type: application/json' \
  -d '{"destination":"https://example.com/promotion","code":"Promo2026"}'
```

Resultados esperados:

- ambas as respostas são `201` e contêm `code`, `shortUrl`, `destination` e `expiresAtUtc`;
- o primeiro código tem exatamente 7 caracteres alfanuméricos; o segundo é `Promo2026`;
- repetir o segundo pedido devolve `409` e não altera o link original;
- destino inválido, código fora do padrão ou expiração passada devolve `400` sem criar dados.

## 2. Redirecionamento, Cache e Estatísticas

Usar um cliente que não siga redirecionamentos automaticamente:

```bash
curl -i http://localhost:5080/Promo2026
curl -i http://localhost:5080/links/Promo2026/stats
```

Resultados esperados:

- o primeiro pedido devolve `302`, `Location: https://example.com/promotion`,
  `Cache-Control: no-store, no-cache`, `Pragma: no-cache` e `Expires: 0`;
- repetir o clique volta à aplicação e incrementa o total;
- as estatísticas devolvem `200` e `accessCount` igual ao número de cliques válidos;
- um código inexistente devolve `404` e não altera nenhum contador.

Repetir o cenário com um código personalizado `scalar`, criado no ambiente de desenvolvimento.
`GET /scalar` deve devolver `302` para o destino desse link e incrementar o seu contador;
`GET /docs/scalar` continua a apresentar a documentação. Não reservar nem recusar o código `scalar`.

## 3. Expiração UTC

Criar um link com `expiresAtUtc` alguns segundos no futuro em UTC. Antes desse instante, aceder ao
código e confirmar `302`. Depois do instante, repetir o acesso e consultar as estatísticas.

Resultados esperados:

- após a expiração, o acesso devolve `410` e não apresenta `Location`;
- o total permanece igual ao registado antes da expiração;
- as estatísticas continuam acessíveis com `200` e `expired: true`;
- uma data passada ou sem referência UTC é recusada na criação com `400`.

## 4. Persistência após Reinício

Criar um código personalizado, aceder algumas vezes e anotar as estatísticas. Parar o processo e
executar novamente o comando de arranque. Consultar o mesmo código.

Resultado esperado: destino, código, expiração e total permanecem inalterados; um link ainda ativo volta
a redirecionar e a incrementar a partir do valor preservado.

## 5. Limite de Criação

Numa instância nova, enviar 11 pedidos de criação com destinos válidos em menos de 60 segundos a partir
do mesmo endereço IP. Manter redirecionamentos e consultas a ocorrer durante o ensaio. Usar uma
instância nova ou aguardar a libertação das permissões dos cenários anteriores antes de começar.

Resultados esperados:

- os primeiros 10 pedidos são aceites e o pedido excedente devolve `429` com `Retry-After`;
- nenhum link é criado pelo pedido recusado;
- redirecionamentos e estatísticas não recebem `429` devido a esta política;
- após o tempo indicado, uma nova criação volta a ser aceite.

A janela é móvel e exata: cada pedido admitido ocupa uma das dez permissões durante 60 segundos;
pedidos recusados pelo limitador não prolongam esse prazo. `Retry-After` indica o tempo até libertar a
permissão mais antiga, arredondado para cima em segundos, com mínimo 1.

Nos testes automatizados, usar relógio controlado com origem não alinhada: dez pedidos no instante t
são admitidos; o 11.º em t e em t+59,999 segundos recebe `429`; em t+60 segundos há nova permissão.
Outro IP mantém as suas permissões. Executar estes casos sem esperas reais e mantendo o middleware ativo.

## 6. Contagem sob Concorrência

Criar um link novo sem expiração e substituir `CODE` pelo código obtido:

```bash
export CODE=CODE
seq 1 1000 | xargs -P 50 -I{} curl -s -o /dev/null "http://localhost:5080/${CODE}"
curl -s "http://localhost:5080/links/${CODE}/stats"
```

Resultado esperado: `accessCount` é exatamente 1000 e nunca diminui em consultas posteriores. O ensaio
de desempenho automatizado deve adicionalmente confirmar os percentis de 95% definidos em `SC-001` e
`SC-002`; a conclusão deste comando sozinha não comprova esses percentis.

## 7. Validação Estrutural

Rever os ficheiros em `src` e confirmar:

- a solução, a API e o projeto `UrlShortener.Api.Tests` estão dentro de `src`;
- não existe código de aplicação ou de testes fora de `src`;
- não existem controladores, pastas globais de serviços ou repositórios;
- cada feature está integralmente no seu ficheiro e não referencia outra feature;
- apenas infraestrutura transversal está em `Shared`;
- os endpoints delegam o acesso a dados nos handlers locais;
- identificadores estão em inglês e eventuais comentários em português;
- todos os endpoints podem ser visualizados e executados através do Scalar.

## 8. Indisponibilidade e Colisões

Executar nos testes das três histórias uma falha reversível na abertura da ligação SQLite, depois da
inicialização da aplicação e antes da operação. Os três endpoints devem devolver `503` em
`application/problem+json`; após recuperar a ligação, confirmar que criação não inseriu dados,
redirecionamento não incrementou o contador e consulta preservou os dados. O redirecionamento
recusado não inclui `Location`. Não simular estes casos sobre dados reais nem interpretar uma falha
após commit incerto como prova de ausência de escrita.

No teste de colisões de `CreateLink`, inserir `Taken01` por fixture e controlar localmente o gerador
para devolver `Taken01` seguido de `Fresh01`. Esperar `201`, uma única nova linha com `Fresh01` e
preservação integral de `Taken01`. Repetir com 32 candidatos iguais a `Taken01`: esperar `503` e
nenhuma nova linha. Executar a verificação de unicidade de 1000 códigos ao nível do handler, separada
dos testes HTTP da política de criação.

## 9. Protocolo de Desempenho (T023)

Executar `src/UrlShortener.Api.Tests/PerformanceTests.cs` em Release, sem depurador e sem outras classes
de testes em paralelo. Usar SQLite em disco local temporário, relógio real e cliente que não segue
redirecionamentos. Registar versão do SDK, sistema operativo, CPU, memória e configuração da execução.

1. Aquecer o pipeline com dez criações válidas de um IP reservado para aquecimento e dez
   redirecionamentos de um link de aquecimento. Excluir estes pedidos das amostras e dos contadores
   medidos; o IP e o link de aquecimento não são reutilizados nas medições.
2. Medir 100 criações válidas, sequencialmente, distribuídas por dez IPs de teste distintos, com dez
   pedidos por IP. Atribuir `RemoteIpAddress` apenas no transporte de teste, antes do limitador;
   não desativar a política nem tornar cabeçalhos arbitrários confiáveis em produção. Todas as
   respostas têm de ser `201`; qualquer `429` ou outro estado faz falhar o ensaio.
3. Escolher um link sem expiração e com contador zero, distinto do usado no aquecimento. Medir
   1000 pedidos de redirecionamento com concorrência máxima de 50. Todas as respostas têm de ser
   `302` para o destino esperado; consultar o contador fora da medição e exigir exatamente 1000.
4. Medir cada pedido completo com temporizador monotónico, desde antes do envio até à leitura
   completa da resposta, incluindo o corpo. Ordenar as durações de cada operação por ordem crescente;
   o percentil 95 é o elemento na posição `ceil(0,95 * N)`, contando posições a partir de 1:
   posição 95 nas 100 criações e posição 950 nos 1000 redirecionamentos.
5. Exigir percentil 95 de criação até 2 segundos e de redirecionamento até 1 segundo. Registar tamanho
   das amostras, percentis e estados HTTP; nunca excluir falhas, repetir silenciosamente pedidos
   falhados ou remover valores lentos. As medições com a factory abrangem o pipeline HTTP e SQLite
   locais; não medem latência de uma rede externa.
