# Especificação da Funcionalidade: [FEATURE NAME]

**Ramo da Funcionalidade**: `[###-feature-name]`

**Criada em**: [DATE]

**Estado**: Rascunho

**Entrada**: Descrição do utilizador: "$ARGUMENTS"

## Cenários e Testes do Utilizador *(obrigatório)*

<!--
  IMPORTANTE: As histórias de utilizador devem ser PRIORIZADAS como jornadas do utilizador ordenadas por importância.
  Cada história/jornada do utilizador tem de ser TESTÁVEL INDEPENDENTEMENTE — ou seja, se implementar apenas UMA delas,
  deve continuar a obter um MVP (Produto Mínimo Viável) que forneça valor.

  Atribua prioridades (P1, P2, P3, etc.) a cada história, sendo P1 a mais crítica.
  Considere cada história como uma fatia autónoma de funcionalidade que pode ser:
  - Desenvolvida independentemente
  - Testada independentemente
  - Implementada em produção independentemente
  - Demonstrada aos utilizadores independentemente
-->

### História de Utilizador 1 - [Título Breve] (Prioridade: P1)

[Descreva esta jornada do utilizador em linguagem simples]

**Porquê esta prioridade**: [Explique o valor e o motivo deste nível de prioridade]

**Teste Independente**: [Descreva como pode ser testada independentemente — por exemplo, "Pode ser totalmente testada através de [ação específica] e fornece [valor específico]"]

**Cenários de Aceitação**:

1. **Dado** [estado inicial], **Quando** [ação], **Então** [resultado esperado]
2. **Dado** [estado inicial], **Quando** [ação], **Então** [resultado esperado]

---

### História de Utilizador 2 - [Título Breve] (Prioridade: P2)

[Descreva esta jornada do utilizador em linguagem simples]

**Porquê esta prioridade**: [Explique o valor e o motivo deste nível de prioridade]

**Teste Independente**: [Descreva como pode ser testada independentemente]

**Cenários de Aceitação**:

1. **Dado** [estado inicial], **Quando** [ação], **Então** [resultado esperado]

---

### História de Utilizador 3 - [Título Breve] (Prioridade: P3)

[Descreva esta jornada do utilizador em linguagem simples]

**Porquê esta prioridade**: [Explique o valor e o motivo deste nível de prioridade]

**Teste Independente**: [Descreva como pode ser testada independentemente]

**Cenários de Aceitação**:

1. **Dado** [estado inicial], **Quando** [ação], **Então** [resultado esperado]

---

[Adicione mais histórias de utilizador conforme necessário, cada uma com uma prioridade atribuída]

### Casos Limite

<!--
  AÇÃO NECESSÁRIA: O conteúdo desta secção consiste em marcadores de posição.
  Substitua-os pelos casos limite adequados.
-->

- O que acontece quando [condição limite]?
- Como é que o sistema trata [cenário de erro]?

## Requisitos *(obrigatório)*

<!--
  AÇÃO NECESSÁRIA: O conteúdo desta secção consiste em marcadores de posição.
  Substitua-os pelos requisitos funcionais adequados.
-->

### Requisitos Funcionais

- **FR-001**: O sistema TEM DE [capacidade específica, por exemplo, "permitir que os utilizadores criem contas"]
- **FR-002**: O sistema TEM DE [capacidade específica, por exemplo, "validar endereços de correio eletrónico"]
- **FR-003**: Os utilizadores TÊM DE conseguir [interação essencial, por exemplo, "repor a palavra-passe"]
- **FR-004**: O sistema TEM DE [requisito de dados, por exemplo, "persistir as preferências do utilizador"]
- **FR-005**: O sistema TEM DE [comportamento, por exemplo, "registar todos os eventos de segurança"]

*Exemplo de marcação de requisitos pouco claros:*

- **FR-006**: O sistema TEM DE autenticar os utilizadores através de [NEEDS CLARIFICATION: método de autenticação não especificado — correio eletrónico/palavra-passe, SSO, OAuth?]
- **FR-007**: O sistema TEM DE conservar os dados dos utilizadores durante [NEEDS CLARIFICATION: período de conservação não especificado]

### Entidades Principais *(incluir se a funcionalidade envolver dados)*

- **[Entidade 1]**: [O que representa, atributos principais sem implementação]
- **[Entidade 2]**: [O que representa, relações com outras entidades]

## Critérios de Sucesso *(obrigatório)*

<!--
  AÇÃO NECESSÁRIA: Defina critérios de sucesso mensuráveis.
  Estes têm de ser independentes da tecnologia e mensuráveis.
-->

### Resultados Mensuráveis

- **SC-001**: [Métrica mensurável, por exemplo, "Os utilizadores conseguem concluir a criação de uma conta em menos de 2 minutos"]
- **SC-002**: [Métrica mensurável, por exemplo, "O sistema suporta 1000 utilizadores simultâneos sem degradação"]
- **SC-003**: [Métrica de satisfação do utilizador, por exemplo, "90% dos utilizadores concluem com sucesso a tarefa principal à primeira tentativa"]
- **SC-004**: [Métrica de negócio, por exemplo, "Reduzir em 50% os pedidos de suporte relacionados com [X]"]

## Pressupostos

<!--
  AÇÃO NECESSÁRIA: O conteúdo desta secção consiste em marcadores de posição.
  Substitua-os pelos pressupostos adequados, com base em opções predefinidas razoáveis
  escolhidas quando a descrição da funcionalidade não especificou determinados detalhes.
-->

- [Pressuposto sobre os utilizadores-alvo, por exemplo, "Os utilizadores têm uma ligação estável à Internet"]
- [Pressuposto sobre os limites do âmbito, por exemplo, "O suporte para dispositivos móveis está fora do âmbito da v1"]
- [Pressuposto sobre dados/ambiente, por exemplo, "O sistema de autenticação existente será reutilizado"]
- [Dependência de um sistema/serviço existente, por exemplo, "Requer acesso à API de perfil de utilizador existente"]
