<img width="100%" alt="Halloween 2026" src="https://baltaio.blob.core.windows.net/static/images/v4/challenges/halloween-2026/banner.jpg" />

## 🎃 Halloween - Desafio 3

Olá, sou o Raul Mateia da Silva e este é o espaço onde partilho o que aprendi durante o desafio **Halloween 2026**, realizado pelo [balta.io](https://balta.io). 👻

Aqui você vai encontrar projetos, exercícios e códigos que estou desenvolvendo durante o desafio.

### Sobre este desafio
Neste desafio o objetivo é criar um encurtador de URLs completo utilizando os conceitos de Spec-Driven Development via SpecKit

#### O que foi implementado
- API .NET 10 com criação de links automáticos ou personalizados e expiração UTC opcional.
- Redirecionamento com contagem atómica, estatísticas públicas e persistência SQLite em WAL.
- Limite de dez criações por IP numa janela móvel de 60 segundos.
- OpenAPI e Scalar em desenvolvimento, com testes de contrato, concorrência e desempenho.

### Executar e validar

Requer SDK .NET 10. Na raiz do repositório:

```powershell
dotnet restore src/UrlShortener.sln --locked-mode
dotnet build src/UrlShortener.sln --configuration Release --no-restore
dotnet test src/UrlShortener.sln --configuration Release --no-build
dotnet run --project src/UrlShortener.Api --no-launch-profile -- --environment Development --urls http://localhost:5080
```

A documentação interativa está em `http://localhost:5080/docs/scalar`.
A base inicial é `src/UrlShortener.Api/links.db`; a variável `ConnectionStrings__Links`
permite escolher outro ficheiro. Os cenários e critérios de validação estão no
[guia de validação](specs/001-encurtador-links/quickstart.md).

Neste processo aprendi a:

- Usar o SpecKit para definir requisitos e critérios de aceitação antes de escrever código, seguindo o fluxo de especificação, planeamento, tarefas e implementação.
- Construir uma API com .NET 10 e Minimal APIs, organizada por funcionalidade com Vertical Slice Architecture.
- Persistir links em SQLite e contar acessos com operações atómicas, para não perder incrementos quando chegam pedidos simultâneos.
- Tratar a expiração em UTC, validar URLs e códigos personalizados e limitar a criação de links por endereço IP.
- Escrever testes de integração, concorrência e desempenho, incluindo cenários de reinício da aplicação e falhas de acesso à base de dados.
- Rever o código gerado com apoio de IA e verificar se o comportamento corresponde à especificação, usando testes e a documentação interactiva do Scalar.

## Bagde
<img src="https://baltaio.blob.core.windows.net/static/images/v4/challenges/halloween-2026/03.png" width="200" />

## Problema

URLs longas são pouco práticas para partilhar e difíceis de escrever manualmente. Quem as partilha também precisa de saber quantos acessos receberam e, em alguns casos, definir até quando podem ser utilizadas.

O desafio foi criar um encurtador que recebe uma URL e devolve um link curto com um código único, automático ou personalizado. Ao abrir esse link, o visitante é redirecionado para o destino e o acesso é contabilizado. O criador pode consultar o total de acessos e definir uma expiração opcional.

A solução tem de preservar os links e os contadores após reiniciar a aplicação, contar correctamente os acessos simultâneos e impedir que links expirados redirecionem ou registem novos acessos. Todo o desenvolvimento segue os requisitos e critérios de aceitação definidos com Spec-Driven Development através do SpecKit.

## Sobre o Halloween 2026
O desafio **Halloween 2026** consiste em implementar implementar o modelo Spec Driven Development de ponta a ponta, criando apps completas com IA.

### Veja meu progresso no desafio
[Incluir link para o repositório central]
