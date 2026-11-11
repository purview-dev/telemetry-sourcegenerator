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

EF Core does **not** map `ValueGeneratedOnAdd` to `NEWSEQUENTIALID()` by default. For a `Guid` key on
SQL Server the provider generates the value client-side with `SequentialGuidValueGenerator` (below).
Configure a store default explicitly when the database must own the value:

```csharp
builder.Property(o => o.Id).HasDefaultValueSql("NEWSEQUENTIALID()");
```

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

### Application-generated, store-ordered strategy

When one context type serves both SQL Server and PostgreSQL, or the identifier must exist before
`SaveChanges` on both, a single built-in generator is not enough: the two stores order GUIDs
differently. The answer is a store-specific strategy carried as context-options data — a
`static abstract` witness whose SQL Server implementation returns `FastGuid.NewSqlServerGuid()`, plus a
value generator and a model-finalising convention that applies it to every GUID key.

```csharp
public sealed class SqlServerSequentialGuidProvider : ISequentialGuidProvider
{
	public static string Name => "SqlServer";

	public static Guid NewGuid() => FastGuid.NewSqlServerGuid();

	public static Guid NewGuid(DateTime timestampUtc) =>
		FastGuid.NewSqlServerGuid(UtcTimestamp.EnsureUtc(timestampUtc));

	public static DateTime GetTimestamp(Guid value) => FastGuid.SqlServer.GetTimestamp(value);

	public static Guid MinValueFor(DateTime timestampUtc) =>
		FastGuid.SqlServer.MinGuidForTimestamp(UtcTimestamp.EnsureUtc(timestampUtc));

	public static Guid MaxValueFor(DateTime timestampUtc) =>
		FastGuid.SqlServer.MaxGuidForTimestamp(UtcTimestamp.EnsureUtc(timestampUtc));
}
```

- The increasing part must sit where SQL Server sorts it. The trailing six bytes dominate
  `uniqueidentifier`; a byte-ordered UUIDv7 does not.
- The generator must hand the value through the property's value converter, or a `readonly record
  struct OrderId` key receives a raw `Guid`.
- Apply it at convention precedence, so explicit `ValueGeneratedNever()`, a store default and a
  computed column all win.

The full sample is the Engineering Playbook's *GUID Key Strategy* page. The abstraction is earned by
the second provider; on one SQL Server provider `NEWSEQUENTIALID()` or EF Core's generator is enough.

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
| High insert volume, identifier needed before persisting | EF Core `SequentialGuidValueGenerator`, or a store-ordered `FastGuid` strategy when both providers share a model |
| Key must be unpredictable (public surface, security boundary) | `Guid.NewGuid()`, with a non-clustered PK |
| Low or moderate volume | `Guid.NewGuid()` — do not optimise what does not hurt |
| You want a sortable timestamp in the key and accept the locality cost | `Guid.CreateVersion7()` |

## Measure before optimising

Index fragmentation from random GUIDs is real but frequently irrelevant. Before changing a key
strategy, confirm it matters: check page splits, fragmentation and buffer-pool pressure on the actual
table at the actual volume. Changing a primary key strategy on a live table is expensive; do it
against evidence.
