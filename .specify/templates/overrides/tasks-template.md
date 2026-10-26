---

description: "Template de lista de tarefas para implementação de funcionalidades"
---

# Tarefas: [FEATURE NAME]

**Entrada**: Documentos de conceção em `/specs/[###-feature-name]/`

**Pré-requisitos**: plan.md (obrigatório), spec.md (obrigatório para histórias de utilizador), research.md, data-model.md, contracts/

**Testes**: Os exemplos abaixo incluem tarefas de teste. Os testes são OPCIONAIS — inclua-os apenas se forem explicitamente solicitados na especificação da funcionalidade.

**Organização**: As tarefas são agrupadas por história de utilizador para permitir a implementação e o teste independentes de cada história.

## Formato: `[ID] [P?] [Story] Descrição`

- **[P]**: Pode ser executada em paralelo (ficheiros diferentes, sem dependências)
- **[Story]**: História de utilizador a que a tarefa pertence (por exemplo, US1, US2, US3)
- Inclua caminhos exatos dos ficheiros nas descrições

## Convenções de Caminhos

- **Projeto único**: `src/`, `tests/` na raiz do repositório
- **Aplicação Web**: `backend/src/`, `frontend/src/`
- **Aplicação móvel**: `api/src/`, `ios/src/` ou `android/src/`
- Os caminhos apresentados abaixo pressupõem um projeto único — ajuste-os com base na estrutura de plan.md

<!--
  ============================================================================
  IMPORTANTE: As tarefas abaixo são TAREFAS DE EXEMPLO apenas para ilustração.

  O comando /skill:speckit-tasks TEM DE substituí-las por tarefas reais com base em:
  - Histórias de utilizador de spec.md (com as suas prioridades P1, P2, P3...)
  - Requisitos da funcionalidade de plan.md
  - Entidades de data-model.md
  - Endpoints de contracts/

  As tarefas TÊM DE ser organizadas por história de utilizador para que cada história possa ser:
  - Implementada independentemente
  - Testada independentemente
  - Entregue como um incremento de MVP

  NÃO mantenha estas tarefas de exemplo no ficheiro tasks.md gerado.
  ============================================================================
-->

## Fase 1: Preparação (Infraestrutura Partilhada)

**Objetivo**: Inicialização do projeto e estrutura básica

- [ ] T001 Criar a estrutura do projeto de acordo com o plano de implementação
- [ ] T002 Inicializar o projeto em [linguagem] com as dependências de [framework]
- [ ] T003 [P] Configurar ferramentas de análise estática e formatação

---

## Fase 2: Fundações (Pré-requisitos Bloqueantes)

**Objetivo**: Infraestrutura essencial que TEM DE estar concluída antes de poder ser implementada QUALQUER história de utilizador

**⚠️ CRÍTICO**: Não pode começar qualquer trabalho em histórias de utilizador até esta fase estar concluída

Exemplos de tarefas de fundação (ajuste de acordo com o seu projeto):

- [ ] T004 Configurar o esquema da base de dados e o framework de migrações
- [ ] T005 [P] Implementar o framework de autenticação/autorização
- [ ] T006 [P] Configurar o encaminhamento da API e a estrutura de middleware
- [ ] T007 Criar modelos/entidades base de que todas as histórias dependem
- [ ] T008 Configurar a infraestrutura de tratamento de erros e registo
- [ ] T009 Configurar a gestão da configuração do ambiente

**Ponto de Verificação**: Fundações prontas — a implementação das histórias de utilizador pode agora começar em paralelo

---

## Fase 3: História de Utilizador 1 - [Título] (Prioridade: P1) 🎯 MVP

**Objetivo**: [Breve descrição do que esta história fornece]

**Teste Independente**: [Como verificar que esta história funciona isoladamente]

### Testes da História de Utilizador 1 (OPCIONAL — apenas se forem solicitados testes) ⚠️

> **NOTA: Escreva estes testes PRIMEIRO e garanta que FALHAM antes da implementação**

- [ ] T010 [P] [US1] Teste de contrato para [endpoint] em tests/contract/test_[name].py
- [ ] T011 [P] [US1] Teste de integração para [jornada do utilizador] em tests/integration/test_[name].py

### Implementação da História de Utilizador 1

- [ ] T012 [P] [US1] Criar o modelo [Entity1] em src/models/[entity1].py
- [ ] T013 [P] [US1] Criar o modelo [Entity2] em src/models/[entity2].py
- [ ] T014 [US1] Implementar [Service] em src/services/[service].py (depende de T012, T013)
- [ ] T015 [US1] Implementar [endpoint/funcionalidade] em src/[location]/[file].py
- [ ] T016 [US1] Adicionar validação e tratamento de erros
- [ ] T017 [US1] Adicionar registo das operações da história de utilizador 1

**Ponto de Verificação**: Neste momento, a História de Utilizador 1 deve estar totalmente funcional e poder ser testada independentemente

---

## Fase 4: História de Utilizador 2 - [Título] (Prioridade: P2)

**Objetivo**: [Breve descrição do que esta história fornece]

**Teste Independente**: [Como verificar que esta história funciona isoladamente]

### Testes da História de Utilizador 2 (OPCIONAL — apenas se forem solicitados testes) ⚠️

- [ ] T018 [P] [US2] Teste de contrato para [endpoint] em tests/contract/test_[name].py
- [ ] T019 [P] [US2] Teste de integração para [jornada do utilizador] em tests/integration/test_[name].py

### Implementação da História de Utilizador 2

- [ ] T020 [P] [US2] Criar o modelo [Entity] em src/models/[entity].py
- [ ] T021 [US2] Implementar [Service] em src/services/[service].py
- [ ] T022 [US2] Implementar [endpoint/funcionalidade] em src/[location]/[file].py
- [ ] T023 [US2] Integrar com os componentes da História de Utilizador 1 (se necessário)

**Ponto de Verificação**: As Histórias de Utilizador 1 E 2 devem funcionar ambas independentemente

---

## Fase 5: História de Utilizador 3 - [Título] (Prioridade: P3)

**Objetivo**: [Breve descrição do que esta história fornece]

**Teste Independente**: [Como verificar que esta história funciona isoladamente]

### Testes da História de Utilizador 3 (OPCIONAL — apenas se forem solicitados testes) ⚠️

- [ ] T024 [P] [US3] Teste de contrato para [endpoint] em tests/contract/test_[name].py
- [ ] T025 [P] [US3] Teste de integração para [jornada do utilizador] em tests/integration/test_[name].py

### Implementação da História de Utilizador 3

- [ ] T026 [P] [US3] Criar o modelo [Entity] em src/models/[entity].py
- [ ] T027 [US3] Implementar [Service] em src/services/[service].py
- [ ] T028 [US3] Implementar [endpoint/funcionalidade] em src/[location]/[file].py

**Ponto de Verificação**: Todas as histórias de utilizador devem agora funcionar independentemente

---

[Adicione mais fases de histórias de utilizador conforme necessário, seguindo o mesmo padrão]

---

## Fase N: Refinamento e Aspetos Transversais

**Objetivo**: Melhorias que afetam várias histórias de utilizador

- [ ] TXXX [P] Atualizações da documentação em docs/
- [ ] TXXX Limpeza e refatoração do código
- [ ] TXXX Otimização do desempenho em todas as histórias
- [ ] TXXX [P] Testes unitários adicionais (se solicitados) em tests/unit/
- [ ] TXXX Reforço da segurança
- [ ] TXXX Executar a validação de quickstart.md

---

## Dependências e Ordem de Execução

### Dependências entre Fases

- **Preparação (Fase 1)**: Sem dependências — pode começar imediatamente
- **Fundações (Fase 2)**: Depende da conclusão da Preparação — BLOQUEIA todas as histórias de utilizador
- **Histórias de Utilizador (Fase 3+)**: Todas dependem da conclusão da fase de Fundações
  - As histórias de utilizador podem depois avançar em paralelo (se houver disponibilidade da equipa)
  - Ou sequencialmente por ordem de prioridade (P1 → P2 → P3)
- **Refinamento (Fase Final)**: Depende da conclusão de todas as histórias de utilizador pretendidas

### Dependências das Histórias de Utilizador

- **História de Utilizador 1 (P1)**: Pode começar após as Fundações (Fase 2) — Sem dependências de outras histórias
- **História de Utilizador 2 (P2)**: Pode começar após as Fundações (Fase 2) — Pode integrar com US1, mas deve poder ser testada independentemente
- **História de Utilizador 3 (P3)**: Pode começar após as Fundações (Fase 2) — Pode integrar com US1/US2, mas deve poder ser testada independentemente

### Dentro de Cada História de Utilizador

- Os testes (se incluídos) TÊM DE ser escritos e FALHAR antes da implementação
- Modelos antes dos serviços
- Serviços antes dos endpoints
- Implementação principal antes da integração
- História concluída antes de avançar para a prioridade seguinte

### Oportunidades de Paralelização

- Todas as tarefas de Preparação assinaladas com [P] podem ser executadas em paralelo
- Todas as tarefas de Fundações assinaladas com [P] podem ser executadas em paralelo (dentro da Fase 2)
- Assim que a fase de Fundações estiver concluída, todas as histórias de utilizador podem começar em paralelo (se a capacidade da equipa o permitir)
- Todos os testes de uma história de utilizador assinalados com [P] podem ser executados em paralelo
- Os modelos dentro de uma história assinalados com [P] podem ser executados em paralelo
- Histórias de utilizador diferentes podem ser desenvolvidas em paralelo por membros diferentes da equipa

---

## Exemplo de Paralelização: História de Utilizador 1

```bash
# Executar todos os testes da História de Utilizador 1 em conjunto (se forem solicitados testes):
Task: "Teste de contrato para [endpoint] em tests/contract/test_[name].py"
Task: "Teste de integração para [jornada do utilizador] em tests/integration/test_[name].py"

# Executar a criação de todos os modelos da História de Utilizador 1 em conjunto:
Task: "Criar o modelo [Entity1] em src/models/[entity1].py"
Task: "Criar o modelo [Entity2] em src/models/[entity2].py"
```

---

## Estratégia de Implementação

### MVP Primeiro (Apenas História de Utilizador 1)

1. Concluir a Fase 1: Preparação
2. Concluir a Fase 2: Fundações (CRÍTICO — bloqueia todas as histórias)
3. Concluir a Fase 3: História de Utilizador 1
4. **PARAR e VALIDAR**: Testar a História de Utilizador 1 independentemente
5. Implementar em produção/demonstrar, se estiver pronta

### Entrega Incremental

1. Concluir Preparação + Fundações → Fundações prontas
2. Adicionar História de Utilizador 1 → Testar independentemente → Implementar/Demonstrar (MVP!)
3. Adicionar História de Utilizador 2 → Testar independentemente → Implementar/Demonstrar
4. Adicionar História de Utilizador 3 → Testar independentemente → Implementar/Demonstrar
5. Cada história acrescenta valor sem quebrar as histórias anteriores

### Estratégia de Equipa em Paralelo

Com vários programadores:

1. A equipa conclui a Preparação + Fundações em conjunto
2. Quando as Fundações estiverem concluídas:
   - Programador A: História de Utilizador 1
   - Programador B: História de Utilizador 2
   - Programador C: História de Utilizador 3
3. As histórias são concluídas e integram-se independentemente

---

## Notas

- Tarefas [P] = ficheiros diferentes, sem dependências
- A etiqueta [Story] associa a tarefa a uma história de utilizador específica para rastreabilidade
- Cada história de utilizador deve poder ser concluída e testada independentemente
- Verificar que os testes falham antes da implementação
- Fazer commit após cada tarefa ou grupo lógico
- Parar em qualquer ponto de verificação para validar a história independentemente
- Evitar: tarefas vagas, conflitos no mesmo ficheiro, dependências entre histórias que quebrem a independência
