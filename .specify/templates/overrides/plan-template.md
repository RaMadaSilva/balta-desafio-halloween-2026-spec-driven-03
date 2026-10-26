# Plano de Implementação: [FEATURE]

**Ramo**: `[###-feature-name]` | **Data**: [DATE] | **Especificação**: [link]

**Entrada**: Especificação da funcionalidade em `/specs/[###-feature-name]/spec.md`

**Nota**: Este template é preenchido pelo comando `/skill:speckit-plan`; a sua definição descreve o fluxo de trabalho de execução.

## Resumo

[Extrair da especificação da funcionalidade: requisito principal + abordagem técnica resultante da pesquisa]

## Contexto Técnico

<!--
  AÇÃO NECESSÁRIA: Substitua o conteúdo desta secção pelos detalhes técnicos
  do projeto. A estrutura aqui apresentada tem carácter orientador para guiar
  o processo de iteração.
-->

**Linguagem/Versão**: [por exemplo, Python 3.11, Swift 5.9, Rust 1.75 ou NEEDS CLARIFICATION]

**Dependências Principais**: [por exemplo, FastAPI, UIKit, LLVM ou NEEDS CLARIFICATION]

**Armazenamento**: [se aplicável, por exemplo, PostgreSQL, CoreData, ficheiros ou N/A]

**Testes**: [por exemplo, pytest, XCTest, cargo test ou NEEDS CLARIFICATION]

**Plataforma de Destino**: [por exemplo, servidor Linux, iOS 15+, WASM ou NEEDS CLARIFICATION]

**Tipo de Projeto**: [por exemplo, library/cli/web-service/mobile-app/compiler/desktop-app ou NEEDS CLARIFICATION]

**Objetivos de Desempenho**: [específicos do domínio, por exemplo, 1000 pedidos/s, 10 mil linhas/s, 60 fps ou NEEDS CLARIFICATION]

**Restrições**: [específicas do domínio, por exemplo, <200 ms p95, <100 MB de memória, funcionamento offline ou NEEDS CLARIFICATION]

**Escala/Âmbito**: [específicos do domínio, por exemplo, 10 mil utilizadores, 1 milhão de linhas de código, 50 ecrãs ou NEEDS CLARIFICATION]

## Verificação da Constituição

*BARREIRA: Tem de ser aprovada antes da pesquisa da Fase 0. Verificar novamente após a conceção da Fase 1.*

[Barreiras determinadas com base no ficheiro da constituição]

## Estrutura do Projeto

### Documentação (desta funcionalidade)

```text
specs/[###-feature]/
├── plan.md              # Este ficheiro (resultado do comando /skill:speckit-plan)
├── research.md          # Resultado da Fase 0 (comando /skill:speckit-plan)
├── data-model.md        # Resultado da Fase 1 (comando /skill:speckit-plan)
├── quickstart.md        # Resultado da Fase 1 (comando /skill:speckit-plan)
├── contracts/           # Resultado da Fase 1 (comando /skill:speckit-plan)
└── tasks.md             # Resultado da Fase 2 (comando /skill:speckit-tasks — NÃO criado por /skill:speckit-plan)
```

### Código-Fonte (raiz do repositório)
<!--
  AÇÃO NECESSÁRIA: Substitua a árvore de marcadores abaixo pela estrutura concreta
  desta funcionalidade. Elimine as opções não utilizadas e desenvolva a estrutura escolhida
  com caminhos reais (por exemplo, apps/admin, packages/something). O plano entregue não deve
  incluir etiquetas de Opção.
-->

```text
# [REMOVE IF UNUSED] Opção 1: Projeto único (PREDEFINIÇÃO)
src/
├── models/
├── services/
├── cli/
└── lib/

tests/
├── contract/
├── integration/
└── unit/

# [REMOVE IF UNUSED] Opção 2: Aplicação Web (quando forem detetados "frontend" + "backend")
backend/
├── src/
│   ├── models/
│   ├── services/
│   └── api/
└── tests/

frontend/
├── src/
│   ├── components/
│   ├── pages/
│   └── services/
└── tests/

# [REMOVE IF UNUSED] Opção 3: Aplicação móvel + API (quando forem detetados "iOS/Android")
api/
└── [igual ao backend acima]

ios/ ou android/
└── [estrutura específica da plataforma: módulos da funcionalidade, fluxos da IU, testes da plataforma]
```

**Decisão sobre a Estrutura**: [Documente a estrutura selecionada e faça referência aos
diretórios reais indicados acima]

## Registo de Complexidade

> **Preencher APENAS se a Verificação da Constituição tiver violações que precisem de ser justificadas**

| Violação | Porque é Necessária | Alternativa Mais Simples Rejeitada Porque |
|----------|---------------------|--------------------------------------------|
| [por exemplo, 4.º projeto] | [necessidade atual] | [porque 3 projetos são insuficientes] |
| [por exemplo, padrão Repository] | [problema específico] | [porque o acesso direto à base de dados é insuficiente] |
