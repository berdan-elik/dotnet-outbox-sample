# dotnet-outbox-sample

A small order API that shows two things in working code: the transactional outbox pattern and idempotent `POST` handling.

.NET 10, ASP.NET Core minimal API, MediatR, FluentValidation, EF Core, PostgreSQL.

## What it does

- `POST /orders` creates an order. The order and an `order.placed` outbox message are written in the same database transaction.
- A background dispatcher picks up outbox messages and sends the confirmation email. A failed send is retried with exponential backoff (5s, 10s, 20s, 40s) and marked dead-lettered after five attempts.
- `POST /orders` requires an `Idempotency-Key` header. The same key with the same body returns the first response again. The same key with a different body returns 422.
- `GET /orders/{id}` reads an order back.

The email sender is a stand-in that writes to the log.

## Run it

```
docker compose up --build
```

The API listens on `http://localhost:8080`. On the first start the log shows one `fail:` line from EF Core probing for its migrations table before creating it; that is expected.

PostgreSQL is not published to the host; to look at the tables, run `docker compose exec db psql -U orders orders`.

Create an order:

```
curl -i -X POST http://localhost:8080/orders \
  -H 'Content-Type: application/json' \
  -H 'Idempotency-Key: demo-1' \
  -d '{"customerEmail":"ada@example.com","lines":[{"sku":"SKU-1","quantity":2,"unitPrice":19.90}]}'
```

A second or so later the API log shows the dispatcher handling the outbox message:

```
Order confirmation for 01a1ff6d-... sent to ada@example.com, total 39.80
```

Run the same command again and you get the same `201` with the same order id; no second order is created and no second email is sent.

Reuse the key with a different body and you get `422`:

```
curl -i -X POST http://localhost:8080/orders \
  -H 'Content-Type: application/json' \
  -H 'Idempotency-Key: demo-1' \
  -d '{"customerEmail":"ada@example.com","lines":[{"sku":"SKU-1","quantity":3,"unitPrice":19.90}]}'
```

Read the order back, using the id from the first response:

```
curl http://localhost:8080/orders/<id>
```

## Tests

```
dotnet test
```

The integration tests start PostgreSQL through Testcontainers, so Docker has to be running. They cover the cases that matter here: eight concurrent requests with one key produce one order and one outbox message; a failed send leaves the message in place and it goes out on the next run; a message that keeps failing is dead-lettered; a second processor skips rows the first one has locked.

## Layout

| Project | Contents |
|---|---|
| `OutboxSample.Domain` | `Order`, `OutboxMessage`, `IdempotencyRecord`. No dependencies. |
| `OutboxSample.Application` | Commands, queries, validators, the validation and idempotency pipeline behaviors. |
| `OutboxSample.Infrastructure` | EF Core context and migration, the outbox dispatcher, the email sender. |
| `OutboxSample.Api` | Endpoints and error mapping. |

Dependencies point inward: Api references Application and Infrastructure, Infrastructure references Application, Application references Domain, and Domain references nothing.

## Design notes

**Why an outbox.** Saving the order and sending the email are two different systems, and there is no transaction that spans both. Send first and the commit can fail, so a customer gets a confirmation for an order that does not exist. Commit first and the process can die before the send, so the email is lost with nothing left to retry. Writing the message into a table in the same transaction as the order turns "send an email" into a row that is either committed with the order or not at all. Sending becomes a separate step that can be retried.

**Why `FOR UPDATE SKIP LOCKED`.** The dispatcher selects pending rows with a row lock and holds it until the batch is done. With plain `FOR UPDATE`, a second instance would block behind the first. With no lock, both would send the same message. `SKIP LOCKED` lets each instance take the rows nobody else holds, so running more than one copy of the API is safe.

**At-least-once, not exactly-once.** If the process dies after a send but before the commit that marks the message processed, the message is sent again on the next run. That window cannot be closed from this side. Whoever receives the message has to cope with seeing it twice, usually by remembering the message id. The outbox row id is there for that.

**How the idempotency race is settled.** The idempotency key is the primary key of its table, and the record is inserted in the same transaction as the order with `ON CONFLICT DO NOTHING`. When two requests with the same key arrive together, both may run the handler, but only one insert succeeds. The other rolls its transaction back, order included, and returns the stored response. The decision is made by the database, so it holds across several API instances.

## Left out on purpose

- A real email provider, authentication, and a message broker.
- Leasing. The row lock is held while the email is sent, which keeps the code short but ties a transaction to the speed of the provider. A slow provider would call for a "claimed until" column instead.
- Jitter on the retry delay, and an endpoint to requeue dead-lettered messages.
- Cleanup. Processed outbox rows and old idempotency records stay in their tables.
- A separate migration step. The API applies migrations at startup, which is fine for one instance and wrong for several.
- Canonical request hashing. The hash is taken over the parsed request, so whitespace and property order do not matter, but `10` and `10.00` count as different bodies.

MediatR is pinned to 12.5.0, the last release under Apache-2.0.

## License

MIT
