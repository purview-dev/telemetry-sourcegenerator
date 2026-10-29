# SQL Server GUID keys

Read this only when the provider is SQL Server and GUIDs are primary keys. The PostgreSQL answer is
different — see `postgresql.md`.

## Why SQL Server is a special case

A clustered index determines physical row order. A `uniqueidentifier` primary key is clustered by
default, so random GUIDs insert into the middle of the index, causing page splits, fragmentation and
a larger buffer-pool working set. On a write-heavy table this is measurable.

The complication is that **SQL Server does not compare `uniqueidentifier` values the way the bytes
are laid out.** Its ordering weights the last six bytes (node field) most significantly, then earlier
groups. So a GUID that is sequential in big-endian byte order — including a standard UUIDv7 — is
**not** sequential under SQL Server's comparison. This is precisely why the SQL Server and PostgreSQL
answers diverge.

## The options

### `NEWSEQUENTIALID()` — database-generated

A column default that produces values ascending in SQL Server's own ordering. Best index locality
available.

```sql
ALTER TABLE [Orders] ADD CONSTRAINT [DF_Orders_Id]
    DEFAULT NEWSEQUENTIALID() FOR [Id];
```

EF Core, for a `Guid` key on SQL Server, configures this by default — `ValueGeneratedOnAdd` maps to
`NEWSEQUENTIALID()` through the default value-generation strategy.

Trade-offs:

- The value does not exist until the row is inserted. EF Core retrieves it, but you cannot build an
  object graph around the key beforehand.
- Values are **not** unpredictable: the sequence restarts on service restart but is otherwise
  guessable. Never use one as a security token or capability URL.
- It reveals rough insert ordering, which can be an information-disclosure concern on a public
  surface.

### EF Core's `SequentialGuidValueGenerator` — client-generated

EF Core ships a value generator that produces GUIDs ordered for SQL Server's comparison semantics,
generated in the application.

```csharp
builder.Property(o => o.Id)
    .HasValueGenerator<SequentialGuidValueGenerator>()
    .ValueGeneratedOnAdd();
```

Use this when the identifier must exist before `SaveChanges`. It keeps most of the index locality
while letting the application own the value.

### `Guid.NewGuid()` — random

Correct, portable and unpredictable; poor index locality.

Appropriate when write volume is low (the fragmentation never matters), when the key is
non-clustered, or when unpredictability is a requirement. Do not dismiss it by default — for most
applications it is fine, and "most applications" includes more than people expect.

### `Guid.CreateVersion7()` — .NET 9+

Ordered in byte layout, but **not** ordered under SQL Server's comparison. On SQL Server it behaves
roughly like a random GUID for clustered-index purposes. It is the right answer on PostgreSQL and the
wrong answer here, which is the trap this page exists to prevent.

Use it on SQL Server only when you want the embedded timestamp for application reasons and accept
random-like insert locality.

## Decoupling the key from the index

Often the best answer is not a different GUID but a different index. Keep the GUID as the primary key
and make the clustered index something naturally ascending:

```csharp
builder.HasKey(o => o.Id).IsClustered(false);
builder.HasIndex(o => o.CreatedAt).IsClustered();
```

This removes the page-split problem entirely and leaves the key unpredictable. Consider it before
reaching for a sequential generator.

## Choosing

| Situation | Use |
| -- | -- |
| High insert volume, key may be database-generated | `NEWSEQUENTIALID()` |
| High insert volume, identifier needed before persisting | EF Core `SequentialGuidValueGenerator` |
| Key must be unpredictable (public surface, security boundary) | `Guid.NewGuid()`, with a non-clustered PK |
| Low or moderate volume | `Guid.NewGuid()` — do not optimise what does not hurt |
| You want a sortable timestamp in the key and accept the locality cost | `Guid.CreateVersion7()` |

## Measure before optimising

Index fragmentation from random GUIDs is real but frequently irrelevant. Before changing a key
strategy, confirm it matters: check page splits, fragmentation and buffer-pool pressure on the actual
table at the actual volume. Changing a primary key strategy on a live table is expensive; do it
against evidence.
