# PostgreSQL UUID keys

Read this only when the provider is PostgreSQL and UUIDs are primary keys. The SQL Server answer is
different — see `sqlserver.md`.

## Why PostgreSQL differs from SQL Server

Two differences drive everything on this page:

1. **PostgreSQL has no clustered index.** Tables are heaps; the primary key is a separate B-tree. A
   random UUID therefore does not cause row-level page splits the way it does on a clustered SQL
   Server table. The cost is in the index: random inserts touch scattered index pages, which hurts
   cache locality and WAL volume, but it is a smaller and differently-shaped problem.
2. **`uuid` compares in byte order.** Unlike SQL Server, PostgreSQL orders UUIDs by their bytes. A
   UUID that is sequential in byte layout *is* sequential to the index.

The consequence: **UUIDv7 works here.** It is the natural answer on PostgreSQL and the wrong answer
on SQL Server. Never carry a sequential-GUID choice across the two.

## The options

### `Guid.CreateVersion7()` — .NET 9+, application-generated

The default recommendation for new PostgreSQL schemas with UUID keys.

```csharp
public sealed class Order
{
    public Guid Id { get; init; } = Guid.CreateVersion7();
}
```

- Time-ordered in byte layout, so index locality is good.
- The value exists before persistence, so outbox rows, correlation and client-side graphs all work.
- Standard (RFC 9562), so it is portable and readable by other systems.
- Embeds a millisecond timestamp — which is also its disclosure caveat.

Tell EF Core the application supplies it:

```csharp
builder.Property(o => o.Id).ValueGeneratedNever();
```

Without that, EF Core may treat the key as store-generated and skip your value.

### Database-generated UUIDv7 — PostgreSQL 18+

PostgreSQL 18 added a native `uuidv7()` function. Earlier versions do not have it; on 13–17 you have
`gen_random_uuid()` (v4, from the built-in pgcrypto-derived function) and nothing native for v7.

**Check the server version before recommending `uuidv7()`.** It is the single most common way this
guidance goes wrong — the function simply does not exist on the version most deployments are running.

```sql
-- PostgreSQL 18 and later only
ALTER TABLE orders ALTER COLUMN id SET DEFAULT uuidv7();
```

Prefer application-side generation anyway unless rows are inserted by paths that bypass the
application, because the value-before-persistence property is worth more than the marginal
difference.

### `gen_random_uuid()` — database-generated v4

Available without an extension from PostgreSQL 13. Random, so index locality is poor, but
unpredictable and simple.

### `Guid.NewGuid()` — application-generated v4

Random and unpredictable. Fine at low to moderate write volume, which covers most applications.

## Npgsql and EF Core notes

- `Guid` maps to `uuid` natively. Do not store UUIDs as `text` or `varchar` — it costs space, loses
  index efficiency and defeats comparison.
- Npgsql's `uuid` mapping is byte-order faithful, so a v7 value sorts in the index as you expect.
- If the application generates the key, set `ValueGeneratedNever()` so EF Core does not fetch a
  store-generated value that does not exist.
- A legacy database may have UUIDs in `text` columns. Migrating to `uuid` is usually worth it, but it
  is a data migration, not a model change.

## Choosing

| Situation | Use |
| -- | -- |
| New schema, .NET 9+, UUID keys | `Guid.CreateVersion7()`, `ValueGeneratedNever()` |
| .NET 8 or earlier, high write volume | A reviewed UUIDv7 implementation, or accept v4 after measuring |
| Rows inserted outside the application, PostgreSQL 18+ | `uuidv7()` as a column default |
| Rows inserted outside the application, PostgreSQL 13–17 | `gen_random_uuid()` |
| Key must be unpredictable | `Guid.NewGuid()` or `gen_random_uuid()` — v7 leaks creation time |
| Low or moderate volume | v4 — the locality difference will not be your bottleneck |

## The timestamp caveat

A UUIDv7 exposes when the row was created, to the millisecond. Where identifiers are public, that can
leak signup times, order rates or activity patterns — and it makes identifiers partially guessable by
time window. Where that matters, use a random UUID for the public identifier and keep the v7 value
internal, or use a surrogate.
