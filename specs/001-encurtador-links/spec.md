# Especificação da Funcionalidade: Encurtador de Links

**Ramo da Funcionalidade**: `001-encurtador-links`

**Criada em**: 2026-10-25

**Estado**: Rascunho

**Entrada**: Descrição do utilizador: "Encurtador de Link: o criador informa uma URL longa e recebe um link curto para partilhar. Quem clica no link curto é redirecionado ao destino e cada acesso é contabilizado. O criador consegue consultar quantos acessos o seu link recebeu. O código pode ser automático ou personalizado, é único e pode ter expiração. Links expirados não redirecionam nem contam acessos. O contador nunca diminui e os links continuam válidos após o reinício da aplicação. Cadastro e login de utilizadores, domínio personalizado, relatórios geográficos e edição do destino estão fora do âmbito."

## Clarifications

### Session 2026-10-25

- Q: Como deve ser interpretada a data e hora de expiração indicadas pelo criador? → A: A data e hora são sempre interpretadas como UTC; o criador deve enviá-las nessa referência.
- Q: Que limite de pedidos deve ser aplicado para reduzir abusos sem impedir a utilização normal dos links? → A: Limitar a criação a 10 links por minuto por endereço IP; redirecionamento e consulta ficam sem limite nesta versão.
- Q: Todos os cliques devem passar novamente pela aplicação para serem contabilizados, mesmo que isso reduza a utilização de cache pelos navegadores? → A: Os redirecionamentos não devem ser reutilizados automaticamente; cada clique volta à aplicação para validar o link e contar o acesso.

## Cenários e Testes do Utilizador *(obrigatório)*

### História de Utilizador 1 - Criar um Link Curto (Prioridade: P1)

Como criador, quero fornecer um endereço longo e, opcionalmente, escolher um código e uma data de
expiração, para receber um link curto que possa partilhar.

**Porquê esta prioridade**: A criação do link curto é a capacidade fundamental sem a qual não existe
valor para partilhar nem acessos para contabilizar.

**Teste Independente**: Criar links com código automático e personalizado, com e sem expiração, e
verificar o formato, a validação dos dados e a preservação dos links após o reinício da aplicação.

**Cenários de Aceitação**:

1. **Dado** um endereço de destino válido sem código personalizado nem expiração, **Quando** o
   criador solicita o link curto, **Então** recebe um link com código alfanumérico único de 7
   caracteres e sem data de expiração.
2. **Dado** um endereço de destino válido e um código alfanumérico disponível entre 4 e 20
   caracteres, **Quando** o criador solicita o link curto, **Então** recebe um link com exatamente
   o código escolhido.
3. **Dado** um endereço de destino válido e uma data de expiração futura, **Quando** o criador
   solicita o link curto, **Então** o link é criado com essa data de expiração.
4. **Dado** um código já utilizado, **Quando** o criador tenta criar outro link com esse código,
   **Então** a criação é recusada e o link existente permanece inalterado.
5. **Dado** um link criado com sucesso, **Quando** a aplicação é reiniciada, **Então** o código, o
   destino, a expiração e o total de acessos continuam disponíveis e inalterados.
6. **Dado** que um endereço IP já criou 10 links no último minuto, **Quando** tenta criar outro,
   **Então** o pedido é recusado sem criar um link, mas os seus redirecionamentos e consultas
   continuam disponíveis.

---

### História de Utilizador 2 - Aceder ao Destino (Prioridade: P2)

Como visitante, quero abrir um link curto válido para ser encaminhado ao endereço de destino,
registando o meu acesso.

**Porquê esta prioridade**: O redirecionamento concretiza o valor do link partilhado e origina os
acessos que o criador pretende acompanhar.

**Teste Independente**: Disponibilizar links ativos, expirados e inexistentes, aceder a cada código e
verificar o destino apresentado e a contabilização resultante, sem depender da consulta de estatísticas.

**Cenários de Aceitação**:

1. **Dado** um link sem expiração, **Quando** um visitante acede ao seu código, **Então** é
   redirecionado para o destino original e o total de acessos aumenta uma unidade.
2. **Dado** um link cuja expiração ainda não foi atingida, **Quando** um visitante acede ao código,
   **Então** é redirecionado para o destino original e o total de acessos aumenta uma unidade.
3. **Dado** um link cuja data de expiração já foi atingida, **Quando** um visitante acede ao código,
   **Então** não é redirecionado e o total de acessos permanece inalterado.
4. **Dado** um código que não existe, **Quando** um visitante tenta aceder, **Então** recebe uma
   indicação de que o link não existe e nenhum contador é alterado.
5. **Dado** um link ativo que recebe vários acessos simultâneos, **Quando** todos os acessos são
   concluídos, **Então** cada acesso válido é contabilizado exatamente uma vez.
6. **Dado** um visitante que já abriu um link curto, **Quando** volta a clicar no mesmo link,
   **Então** o pedido passa novamente pela aplicação para validar o link e contabilizar o novo acesso,
   sem reutilizar automaticamente o redirecionamento anterior.

---

### História de Utilizador 3 - Consultar Acessos (Prioridade: P3)

Como criador, quero consultar o total de acessos de um link através do seu código, para acompanhar a
utilização do endereço que partilhei.

**Porquê esta prioridade**: A consulta fornece visibilidade sobre a utilização, mas depende do valor
essencial de criar e utilizar links curtos.

**Teste Independente**: Preparar links com totais conhecidos, consultar os respetivos códigos e
verificar que os valores apresentados correspondem aos acessos válidos acumulados.

**Cenários de Aceitação**:

1. **Dado** um link que nunca recebeu acessos válidos, **Quando** o criador consulta o código,
   **Então** o total apresentado é zero.
2. **Dado** um link que recebeu vários acessos válidos, **Quando** o criador consulta o código,
   **Então** o total apresentado corresponde exatamente a esses acessos.
3. **Dado** um link com acessos registados, **Quando** ocorrem tentativas após a expiração ou com
   códigos inexistentes, **Então** o total consultado não diminui nem é incrementado por essas
   tentativas.

---

### Casos Limite

- Endereço vazio, sem esquema `http` ou `https`, sem destino válido ou com mais de 2048 caracteres é
  recusado sem criar um link.
- Código personalizado com menos de 4 ou mais de 20 caracteres, ou com caracteres não alfanuméricos,
  é recusado sem criar um link.
- Um código automático que coincida com um código existente não pode duplicá-lo; a criação continua
  com outro código até obter um código único.
- Datas de expiração vazias significam que o link não expira; uma data presente tem de ser futura no
  momento da criação.
- Um acesso no instante exato da expiração é considerado expirado: não redireciona nem incrementa o
  contador.
- Códigos que diferem apenas por maiúsculas e minúsculas são considerados diferentes e podem coexistir.
- Aceder ou consultar um código inexistente não cria dados nem altera acessos de outros links.
- A consulta de um link existente após a sua expiração continua disponível e apresenta o total acumulado.
- O 11.º pedido de criação do mesmo endereço IP dentro de um minuto é recusado até decorrer a janela
  móvel exata de 60 segundos, sem criar um link; redirecionamentos e consultas não são afetados.
  Permissões só são libertadas quando o pedido admitido correspondente completa 60 segundos.
- Cliques repetidos no mesmo código não podem reutilizar automaticamente um redirecionamento anterior;
  cada clique volta à aplicação para validar o estado do link e contabilizar o acesso.

## Requisitos *(obrigatório)*

### Requisitos Funcionais

- **FR-001**: O sistema TEM DE permitir criar um link curto a partir de um endereço de destino.
- **FR-002**: Quando não for fornecido um código, o sistema TEM DE gerar automaticamente um código
  alfanumérico com exatamente 7 caracteres.
- **FR-003**: O sistema TEM DE permitir escolher um código alfanumérico entre 4 e 20 caracteres,
  inclusive.
- **FR-004**: O sistema TEM DE garantir que cada código é único em todo o sistema e que códigos se
  distinguem por maiúsculas e minúsculas.
- **FR-005**: O sistema TEM DE recusar um código personalizado já existente sem modificar o link
  original.
- **FR-006**: O sistema TEM DE voltar a gerar um código automático quando houver colisão, sem criar
  códigos duplicados.
- **FR-007**: O sistema TEM DE aceitar apenas endereços absolutos com esquema `http` ou `https`, um
  destino válido e um máximo de 2048 caracteres.
- **FR-008**: O sistema TEM DE recusar dados inválidos sem criar ou alterar links e indicar o motivo
  da recusa ao criador.
- **FR-009**: O sistema TEM DE permitir definir uma data e hora de expiração futura em UTC ou
  omiti-la para criar um link sem expiração.
- **FR-010**: O sistema TEM DE preservar o código, o destino, a expiração e o total de acessos após
  o reinício da aplicação.
- **FR-011**: Ao aceder a um link ativo, o sistema TEM DE encaminhar o visitante para o endereço de
  destino original e impedir a reutilização automática do redirecionamento, de modo que cada clique
  volte à aplicação para validar o link e contabilizar o acesso.
- **FR-012**: O sistema TEM DE incrementar o total do link exatamente uma vez por cada acesso válido,
  incluindo acessos simultâneos.
- **FR-013**: O sistema NÃO PODE redirecionar nem contabilizar acessos quando a data de expiração já
  foi atingida.
- **FR-014**: O sistema TEM DE informar que um link não existe quando receber um código desconhecido,
  sem alterar qualquer contador.
- **FR-015**: O sistema TEM DE permitir consultar o total acumulado de acessos de um link existente
  através do seu código, mesmo após a expiração.
- **FR-016**: O total de acessos TEM DE começar em zero e NUNCA PODE diminuir durante a existência
  do link.
- **FR-017**: O sistema NÃO PODE permitir alterar o endereço de destino de um link depois da criação.
- **FR-018**: A criação, o acesso e a consulta de links NÃO PODEM exigir cadastro ou autenticação de
  utilizadores.
- **FR-019**: O sistema TEM DE limitar os pedidos de criação a 10 permissões numa janela móvel exata
  de 60 segundos por endereço IP, recusando pedidos excedentes sem criar links. Cada pedido admitido
  ocupa uma permissão durante 60 segundos; pedidos recusados pelo limitador não prolongam esse prazo.
  Redirecionamentos e consultas NÃO TÊM limite nesta versão.

### Entidades Principais *(incluir se a funcionalidade envolver dados)*

- **Link Encurtado**: Representa um endereço partilhável e contém o código único, o endereço de
  destino, a data de expiração opcional e o total acumulado de acessos válidos.
- **Acesso Válido**: Representa a ocorrência de uma visita a um link ativo que resulta em
  redirecionamento e num incremento único do total do respetivo link.

## Critérios de Sucesso *(obrigatório)*

### Resultados Mensuráveis

- **SC-001**: Pelo menos 95% das tentativas com dados válidos permitem obter um link curto em até 2
  segundos, sem intervenção manual.
- **SC-002**: Pelo menos 95% dos acessos a links ativos encaminham o visitante ao destino em até 1
  segundo em condições normais de utilização.
- **SC-003**: Em 1000 criações com código automático, 100% dos códigos têm 7 caracteres alfanuméricos
  e nenhum código é duplicado.
- **SC-004**: Após 1000 acessos válidos a um mesmo link, incluindo pedidos simultâneos, o total
  consultado é exatamente 1000 e não diminui em consultas posteriores.
- **SC-005**: Em 100% dos testes de reinício, os links previamente criados mantêm destino, código,
  expiração e total de acessos, continuando a comportar-se de acordo com o seu estado de validade.
- **SC-006**: Em 100% dos testes com endereços, códigos ou expirações inválidos, a criação é recusada
  sem alterar links existentes e com um motivo compreensível para o criador.

**Protocolo de Aceitação de Desempenho**: SC-001 e SC-002 são avaliados pelo protocolo reproduzível
de [quickstart.md](quickstart.md): Release sem depurador, ambiente registado, aquecimento excluído,
100 criações válidas em dez IPs com dez pedidos por IP e 1000 redirecionamentos com concorrência máxima
de 50. Medir pedidos completos e usar a posição `ceil(0,95 * N)` da amostra ordenada para o percentil 95,
sem excluir falhas, desativar o limitador ou executar outros testes em paralelo com as medições.

## Pressupostos

- Os códigos utilizam apenas letras ASCII maiúsculas, letras ASCII minúsculas e algarismos; são
  sensíveis a maiúsculas e minúsculas.
- O endereço curto utiliza o endereço público da própria aplicação, uma vez que domínios
  personalizados estão fora do âmbito.
- A data de expiração inclui dia e hora em UTC; o link deixa de ser válido no instante indicado.
- Sem cadastro ou autenticação, qualquer pessoa que conheça o código pode consultar o total de
  acessos; não existe informação privada associada ao criador.
- Cada pedido válido ao código conta como um acesso, sem distinção entre visitantes ou tentativas
  repetidas da mesma pessoa.
- Links e respetivos totais são conservados indefinidamente; eliminação e desativação manual não
  fazem parte desta funcionalidade.
- Relatórios geográficos, perfis de visitantes e qualquer análise além do total de acessos estão fora
  do âmbito.
