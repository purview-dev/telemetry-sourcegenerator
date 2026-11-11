# Deriving well-known identifiers with UUIDv5

Read this only when an identifier's **value must be known before storage**: a system tenant, a seeded
reference row, an installation's service principal, or an ingested object whose provider key is stable
and never recycled. A project with random keys and no seeding has no use for UUIDv5 — say nothing and
move on. An outbox row that merely needs an id before `SaveChanges` takes a generated strategy instead.

## The rule

1. Derive the value: `Uuid5.Create(namespace, name)` is RFC 4122 version 5 (SHA-1), byte-identical for
   the same namespace and name in every process, language and restart.
2. Scope the namespace to the isolation boundary — one installation or one deployment. Never a global
   constant shared by every deployment.
3. Fix the name as a canonical constant of the identity kind. Not a display name, not user data, not a
   secret.
4. Once persisted, the value is authoritative. Never recompute it and update the row.
5. Key it explicitly with `ValueGeneratedNever()`, so a sequential-GUID convention never generates over
   a value the application already knows.

## The catalogue

Write the known identities down in one place.

| Identity | Namespace | Name | Derived when | Immutable |
| -- | -- | -- | -- | -- |
| System tenant of an installation | installation id | `system-tenant` | first bootstrap | yes |
| System principal of an installation | installation id | `system-service-principal` | first bootstrap | yes |
| Seeded reference row | installation id | `<entity>:<code>` | seed step | yes |
| Ingested external object (optional) | installation id | `<provider>:<external-id>` | first ingest | yes |

Derive the ingested row only when the provider guarantees the external key is stable and never
recycled. If it can be recycled, keep the local key sequential and make the external key a unique
column.

## Naming rules

- Lowercase ASCII, hyphen-separated, no whitespace, no locale-dependent casing.
- Never from a display name, email, URL, title, or anything a user can edit.
- Never from a secret, and never as a capability token. A name is guessable.
- Renaming a name creates a new identity. Renaming a persisted one is a data migration.

## Bootstrap sequence

Read the persisted identity; if present it wins, and a configured value must match or the operation
fails. If absent, insert it. Derive dependants from the persisted id. Save once. A second run changes
nothing.

The guard matters as much as the insert:

```csharp
if (persisted is not null && persisted.Id != configured)
{
	throw new InvalidOperationException(
		$"Configured installation id '{configured.Value}' does not match the persisted id '{persisted.Id.Value}'.");
}
```

## Migrations cannot call the helper

`HasData` and migration SQL need a literal GUID. A small script prints ready-to-paste constants from
the same derivation, and a test asserts every printed constant equals `Uuid5.Create(...)`. Changing the
namespace or a name after a migration has shipped is a data migration, not an edit.

## Checks to write

| Check | Shape |
| -- | -- |
| RFC 4122 conformance | Fixed vectors for DNS/URL/OID/X500 and the empty name; exact GUIDs |
| Determinism | Same pair twice equal; different names differ |
| Version and variant bits | Byte 6 high nibble `0x5`; byte 8 high bits `10` |
| Cross-language equality | Python `uuid.uuid5` or Node `crypto` reproduces the vectors |
| Catalogue uniqueness | The catalogue's constants are distinct |
| Bootstrap idempotency | Seeding twice changes zero rows; a mismatched configured id throws |
| Explicit keying | The key is `ValueGenerated.Never`, and the sequential convention leaves it alone |

```text
uuid5(6ba7b810-9dad-11d1-80b4-00c04fd430c8, "example.com")     = cfbff0d1-9375-5685-968c-48ce8b15ae17
uuid5(6ba7b811-9dad-11d1-80b4-00c04fd430c8, "www.example.com") = b63cdfa4-3df9-568e-97ae-006c5b8fd652
uuid5(6ba7b812-9dad-11d1-80b4-00c04fd430c8, "1.2.3.4")         = 67448b45-6d15-536b-bef7-8a78c3d10ac6
uuid5(6ba7b814-9dad-11d1-80b4-00c04fd430c8, "CN=Bob")          = 0840e90d-9ca0-5f14-970e-26294d44cb1d
```

The hash path is big-endian. `Guid.ToByteArray()` and `new Guid(byte[])` use a mixed-endian layout and
produce a different hash; that is the most common way a "UUIDv5" implementation is wrong.

**UUIDv5 is not sequential.** Never use it for a high-write table's primary key. It is for a small,
known set of identities; everything else takes `guid-generation.md`.

RFC 4122 is the version-5 specification; RFC 9562 is the current UUID specification and retains the
algorithm. The .NET BCL has no UUIDv5 API, so the helper is a small hand-written function.

The Engineering Playbook's *Well-Known Identifiers* page carries the full helper, catalogue and
bootstrap samples, and their test shapes.
