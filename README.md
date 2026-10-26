<img width="100%" alt="Halloween 2026" src="https://baltaio.blob.core.windows.net/static/images/v4/challenges/halloween-2026/banner.jpg" />

## 🎃 Halloween - Desafio 3

Oi, eu sou o [seu nome aqui] e este é o espaço onde compartilho minha jornada de aprendizado durante o desafio **Halloween 2026**, realizado pelo [balta.io](https://balta.io). 👻

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

Neste processo eu aprendi:
* ✅

## Bagde
<img src="https://baltaio.blob.core.windows.net/static/images/v4/challenges/halloween-2026/03.png" width="200" />

## Problema
--

## Sobre o Halloween 2026
O desafio **Halloween 2026** consiste em implementar implementar o modelo Spec Driven Development de ponta a ponta, criando apps completas com IA.

### Veja meu progresso no desafio
[Incluir link para o repositório central]
