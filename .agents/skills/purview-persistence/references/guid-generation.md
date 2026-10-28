# Choosing a GUID generation strategy

The cross-provider decision procedure. Provider detail lives in `sqlserver.md` and `postgresql.md`.

## Step 0 — is this question in scope?

Ask nothing and recommend nothing unless **both** are true:

- The project persists data, and
- GUIDs are (or are about to be) primary keys.

A stateless CLI, a pure library, a service with an integer key, or a project using GUIDs only as
external correlation identifiers does not need this decision. Raising it anyway is the failure this
page is written to prevent.

## Step 1 — must the identifier exist before persistence?

This question decides more than any other.

**Yes** — the application needs the value before `SaveChanges` for an outbox row, a correlation
identifier, an object graph built client-side, or an identifier returned before commit. The database
cannot be the generator. Go to step 2 with client-side options only.

**No** — the database may generate it, and database generation is available to you.

## Step 2 — which provider, and which version?

Do not skip the version. It changes the available options materially:

| Provider | Version matters because |
| -- | -- |
| SQL Server | `NEWSEQUENTIALID()` is long-standing; the comparison semantics that make byte-ordered UUIDs useless have never changed |
| PostgreSQL | Native `uuidv7()` exists only from 18; `gen_random_uuid()` is built in from 13 |
| .NET | `Guid.CreateVersion7()` exists from .NET 9 |

**The two databases do not share an answer.** SQL Server compares `uniqueidentifier` with the last
six bytes most significant, so a byte-ordered UUIDv7 is effectively random to its clustered index.
PostgreSQL compares `uuid` in byte order, so UUIDv7 is genuinely sequential there. A strategy that is
optimal on one is unremarkable or wrong on the other.

## Step 3 — does locality actually matter?

Sequential keys exist to reduce index fragmentation and page splits under write pressure. If the
table takes tens of inserts a minute, this is not your problem and a random GUID is the simpler,
more private choice.

Reach for a sequential strategy when you can point at write volume, measured fragmentation, page
splits, or buffer-pool pressure. "It's best practice" is not a measurement.

Note also that on SQL Server the problem is often better solved by making the clustered index
something other than the GUID key — see `sqlserver.md`.

## Step 4 — is unpredictability required?

Sequential and time-ordered identifiers are guessable, and UUIDv7 discloses creation time to the
millisecond. Where an identifier appears in a URL, is handed to a third party, or acts as a
capability, use a random UUID — or keep the ordered value internal and expose a random surrogate.

Never use `NEWSEQUENTIALID()` output as a secret.

## Decision summary

| Provider | Needs value before insert | Locality matters | Recommendation |
| -- | -- | -- | -- |
| SQL Server | No | Yes | `NEWSEQUENTIALID()` (EF Core's default for `Guid` keys) |
| SQL Server | Yes | Yes | EF Core `SequentialGuidValueGenerator` |
| SQL Server | Either | No | `Guid.NewGuid()` |
| PostgreSQL | Yes | Yes | `Guid.CreateVersion7()` with `ValueGeneratedNever()` |
| PostgreSQL | No | Yes | `uuidv7()` on 18+, otherwise generate v7 in the application |
| PostgreSQL | Either | No | `Guid.NewGuid()` or `gen_random_uuid()` |
| Any | Either | Either, but must be unpredictable | Random v4 |

## Prefer built-in capability

In order of preference:

1. A framework capability — `Guid.CreateVersion7()`, EF Core's value generators.
2. A database capability — `NEWSEQUENTIALID()`, `uuidv7()`, `gen_random_uuid()`.
3. A reviewed, tested implementation of a published standard, when neither of the above is available
   on your target versions.
4. A bespoke generator — only against a measured requirement the first three cannot meet.

A hand-written sequential GUID generator is easy to get subtly wrong — endianness, clock regression,
counter rollover within a millisecond, and thread safety are all live hazards, and the failure mode
is duplicate or badly-ordered keys found in production.

## There is no Purview GUID package

No Purview library provides GUID generation today. Do not reference one and do not invent one.

If a concrete need emerges that the built-in options cannot meet — established by measurement, not by
anticipation — that is the point at which creating one is worth discussing. Until then this guidance
is the deliverable, and this page is where a recommendation for such a package would be documented.
