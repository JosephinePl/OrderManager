# Tests

`dotnet test` runs the unit and in-memory adapter tests from the solution root.

The two Python scripts below do manual black-box validation against a live API. Start the API first (`dotnet run --project src/OrderManager.Api`), or run `scripts/reset-db.ps1` for a clean slate.

## e2e_test.py

Full CRUD journey: components → boards → orders → associations → export → delete. Asserts HTTP status and response body at each step.

```bash
python tests/e2e_test.py
python tests/e2e_test.py --base-url http://localhost:5168
```

## find_deadlocks.py

Fires concurrent add/remove operations on the same rows to provoke deadlocks. Reports any HTTP 500s where PostgreSQL killed a deadlock cycle.

```bash
python tests/find_deadlocks.py
```
