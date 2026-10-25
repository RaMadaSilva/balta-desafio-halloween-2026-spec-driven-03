<!--
Relatório de Impacto da Sincronização
Alteração de versão: 1.0.0 → 1.1.0
Princípios modificados: nenhum.
Princípios adicionados: VI. Documentação Interativa com Scalar.
Secções adicionadas: nenhuma.
Secções removidas: nenhuma.
TODOs de acompanhamento: nenhum.
-->

# Constituição da API Encurtadora de URLs

## Princípios Fundamentais

### I. Plataforma e Minimal APIs

A API TEM DE ser desenvolvida em .NET 10 e exposta exclusivamente através de Minimal APIs.
NÃO É PERMITIDO criar controladores MVC, controladores de API ou classes base de controladores.
Cada endpoint TEM DE limitar-se a receber o pedido HTTP, validar a entrada, delegar a operação ao
respetivo handler e traduzir o resultado para uma resposta HTTP. Os endpoints NÃO PODEM aceder
diretamente a `DbContext`, ligações, comandos SQL, repositórios ou outros mecanismos de dados.

**Justificação**: Manter a camada HTTP fina separa o transporte das regras de negócio e do acesso
a dados, permitindo testar cada responsabilidade isoladamente.

### II. Vertical Slice por Feature

A solução TEM DE utilizar Vertical Slice Architecture. Cada feature TEM DE ser organizada num
ficheiro próprio, contendo o endpoint, os contratos de entrada e saída, o handler e o respetivo
acesso a dados sempre que estes elementos forem necessários. O nome do ficheiro TEM DE identificar
a feature em inglês. Uma feature NÃO PODE ser distribuída por camadas técnicas horizontais, como
pastas globais de controladores, serviços ou handlers.

**Justificação**: Concentrar cada capacidade num único slice torna o seu comportamento localizável,
reduz alterações dispersas e permite evoluir features de forma independente.

### III. Isolamento entre Slices e Código Comum

Um slice NÃO PODE importar, instanciar, invocar ou referenciar tipos pertencentes a outro slice,
incluindo handlers, modelos, contratos e componentes de acesso a dados. A partilha de comportamento
TEM DE ocorrer através de contratos externos explícitos, nunca através de dependências internas
entre slices. Apenas código genuinamente transversal e independente de qualquer feature PODE ser
partilhado; esse código TEM DE residir num único local comum denominado `Shared`. NÃO É PERMITIDO
duplicar infraestrutura comum nem criar locais alternativos para o mesmo código partilhado.

**Justificação**: O isolamento impede acoplamento entre features, enquanto a centralização do
código transversal evita duplicação sem transformar slices em dependências uns dos outros.

### IV. Persistência SQLite Encapsulada

SQLite É a única base de dados relacional permitida para a aplicação. Todo o acesso a dados TEM DE
estar encapsulado dentro do slice proprietário da operação ou na infraestrutura comum de `Shared`,
sem expor mecanismos de persistência à camada HTTP. A configuração da ligação, a criação do esquema
e as migrações TÊM DE ser determinísticas e reproduzíveis. Um slice NÃO PODE manipular dados através
dos componentes internos de outro slice.

**Justificação**: O encapsulamento preserva os limites das features e permite alterar a persistência
sem contaminar os contratos HTTP ou as regras de negócio.

### V. Convenções de Linguagem e Clareza

Os nomes de classes, records, interfaces, structs, enums, métodos, propriedades e demais tipos de
código TÊM DE ser escritos em inglês. Os comentários em código, quando necessários, TÊM DE ser
escritos em português. Identificadores técnicos, palavras reservadas, nomes de frameworks e termos
consagrados da plataforma PERMANECEM na forma original. Nomes e comentários NÃO PODEM misturar
português e inglês na mesma construção, exceto quando se tratar de terminologia técnica consagrada.

**Justificação**: A convenção uniforme facilita a leitura, a revisão e a integração com o ecossistema
.NET sem renunciar à documentação em português.

### VI. Documentação Interativa com Scalar

A documentação da API TEM DE utilizar Scalar como interface interativa para visualizar e testar os
endpoints. Todos os endpoints públicos TÊM DE estar representados no documento OpenAPI consumido pelo
Scalar, com os respetivos métodos, rotas, parâmetros, corpos de pedido e respostas. A interface TEM DE
estar acessível no ambiente de desenvolvimento e permitir enviar pedidos aos endpoints documentados.

**Justificação**: Uma referência interativa e sincronizada com os endpoints facilita a exploração da
API, a validação manual dos contratos e a deteção de divergências na documentação.

## Restrições Técnicas

- O projeto TEM DE utilizar o SDK e o runtime .NET 10, sem compatibilizar silenciosamente com
  versões anteriores.
- A composição da aplicação e o mapeamento dos endpoints TÊM DE utilizar Minimal APIs.
- NÃO É PERMITIDA a introdução de MVC, Razor Pages ou controladores para contornar estas regras.
- A configuração do SQLite TEM DE ser externa ao código da feature e não pode conter segredos
  embutidos no repositório.
- Cada feature TEM DE permanecer num único ficheiro; uma divisão adicional exige uma alteração
  prévia desta constituição.
- O diretório `Shared` É o único local permitido para código transversal partilhado entre slices.
- Dependências entre slices são proibidas mesmo quando mediadas por tipos internos de persistência.
- Scalar É a interface obrigatória de documentação interativa e NÃO PODE ser substituída por Swagger
  UI ou outra interface sem uma alteração desta constituição.

## Fluxo de Desenvolvimento e Qualidade

- Antes da implementação, cada alteração TEM DE identificar o slice afetado e confirmar que não
  introduz dependências para outros slices.
- A revisão TEM DE verificar que os endpoints não contêm acesso direto a dados nem regras que
  pertençam ao handler.
- A revisão TEM DE confirmar que cada feature está num único ficheiro e que o código partilhado
  está exclusivamente em `Shared`.
- A revisão de um endpoint TEM DE confirmar a sua presença no Scalar e a possibilidade de executar
  um pedido através dessa interface.
- As verificações TÊM DE incluir a compilação em .NET 10 e os testes aplicáveis ao comportamento
  alterado, cobrindo pelo menos o caminho de sucesso e os erros relevantes do endpoint.
- Qualquer violação destes princípios BLOQUEIA a integração até ser corrigida ou formalmente
  registada como exceção através de uma alteração da constituição.

## Governação

Esta constituição prevalece sobre convenções locais e preferências individuais. As alterações TÊM DE
ser propostas com a justificação, o impacto nos slices existentes e o plano de migração quando forem
incompatíveis com o código atual. A aprovação TEM DE ocorrer antes da adoção da alteração.

O versionamento segue Semantic Versioning: MAJOR para remoções ou redefinições incompatíveis de
princípios; MINOR para novos princípios, secções ou expansões materiais; PATCH para clarificações
sem alteração semântica. Toda a alteração TEM DE atualizar a versão e a data da última alteração.

Cada revisão de código TEM DE verificar explicitamente o cumprimento dos seis princípios, das
restrições técnicas e das barreiras de qualidade. Exceções NÃO PODEM ser aprovadas informalmente;
requerem uma alteração governada desta constituição.

**Versão**: 1.1.0 | **Ratificada em**: 2026-10-25 | **Última Alteração**: 2026-10-25
