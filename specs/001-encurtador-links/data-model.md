# Modelo de Dados: Encurtador de Links

## Visão Geral

Existe uma única entidade persistida, `ShortLink`, materializada na tabela `links`. O conceito
`Acesso Válido` não tem tabela própria: é representado pelo incremento atómico de `access_count`.
Os slices usam projeções privadas desta tabela e não partilham tipos de domínio.

## Entidade: ShortLink

| Campo | Persistência | Regras |
|---|---|---|
| `Code` | `code TEXT PRIMARY KEY COLLATE BINARY` | Obrigatório; 4–20 caracteres ASCII alfanuméricos; exatamente 7 quando gerado; único e sensível a maiúsculas/minúsculas. |
| `Destination` | `destination TEXT NOT NULL` | URL absoluta `http` ou `https`; 1–2048 caracteres; imutável após inserção. |
| `ExpiresAtUtc` | `expires_at_utc TEXT NULL` | Ausente para links sem expiração; quando presente, instante futuro na criação, em UTC. |
| `AccessCount` | `access_count INTEGER NOT NULL DEFAULT 0` | Começa em zero; nunca negativo; nunca diminui; incrementa uma vez por acesso válido. |

Não existem relações com outras tabelas nesta versão.

## Restrições de Persistência

A migração inicial cria a tabela como `STRICT, WITHOUT ROWID` e aplica:

- `length(code) BETWEEN 4 AND 20`;
- `code NOT GLOB '*[^0-9A-Za-z]*'`;
- `length(destination) BETWEEN 1 AND 2048`;
- `access_count >= 0`.

A chave primária com `COLLATE BINARY` impede duplicados e preserva a diferença entre `abc`, `Abc` e
`ABC`. Não existe um segundo índice, porque todas as consultas usam `code`. As validações HTTP continuam
obrigatoriamente nos slices; os `CHECK`s não substituem mensagens compreensíveis.

## Formato Temporal

`expires_at_utc` usa texto ISO-8601 UTC com formato invariante e precisão fixa de sete casas
fracionárias: `yyyy-MM-ddTHH:mm:ss.fffffffZ`. A função SQL local `utc_now()` lê o `TimeProvider`
no mesmo formato durante a execução da escrita, depois de obter o bloqueio de escritor. Assim,
a espera pelo SQLite não conserva um instante anterior à expiração. As comparações lexicográficas
equivalem à ordem cronológica. O contrato JSON aceita datas ISO-8601 com UTC explícito.

## Estados e Transições

### Ativo

Um link está ativo quando `expires_at_utc IS NULL` ou `expires_at_utc > now_utc`. Apenas neste estado
pode redirecionar e incrementar `access_count`.

### Expirado

Um link passa automaticamente a expirado quando `expires_at_utc <= now_utc`. Não há escrita nem tarefa
de mudança de estado. O link permanece consultável, mas deixa de redirecionar e de contar acessos.
Esta transição é irreversível porque o destino e a expiração não são editáveis.

### Inexistente

Um código sem linha na tabela produz `404`, não cria dados e não altera contadores.

## Invariantes de Escrita

- A criação valida todos os dados antes do `INSERT` parametrizado.
- O `INSERT ... SELECT ... WHERE` volta a exigir expiração futura usando `utc_now()`; se o prazo
  terminar durante uma espera de escrita, devolve `400` sem inserir.
- A unicidade é imposta pelo `INSERT`; não se usa uma verificação prévia como garantia contra corridas.
- Códigos automáticos colididos são regenerados até 32 tentativas; códigos personalizados duplicados
  produzem `409`.
- Não existem operações `UPDATE` de `destination`, `code` ou `expires_at_utc`, nem operações `DELETE`.
- O único comando que altera o contador executa, numa única instrução:

```sql
UPDATE links
SET access_count = access_count + 1
WHERE code = $code COLLATE BINARY
  AND (expires_at_utc IS NULL OR expires_at_utc > utc_now())
RETURNING destination;
```

- Uma linha devolvida significa acesso válido e autoriza o `302`; zero linhas leva apenas a uma leitura
  para distinguir inexistência de expiração.
- A consulta de estatísticas lê `access_count` sem condição de atividade e nunca o modifica.

## Concorrência e Durabilidade

Cada operação abre a sua ligação através da infraestrutura comum. O `UPDATE ... RETURNING` é atómico e
não precisa de transação explícita. WAL e timeout de cinco segundos reduzem bloqueios, mas as escritas
continuam serializadas pelo SQLite. A persistência em disco local preserva todos os campos após reinício.
