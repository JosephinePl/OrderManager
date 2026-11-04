"""
End-to-end scenario test for OrderManager API.

Walks through a realistic user journey:
  1. Create components (resistors, capacitors, ICs)
  2. Create boards and wire components onto them
  3. Create an order and add boards to it
  4. Read / search entities back
  5. Update entities
  6. Export the order to the production line
  7. Tear down (delete) everything

Run with:
    python tests/e2e_test.py [--base-url http://localhost:5168]
"""

import argparse
import json
import sys
import uuid
import requests

# ---------------------------------------------------------------------------
# Config
# ---------------------------------------------------------------------------

DEFAULT_BASE_URL = "http://localhost:8080"
TIMEOUT = 10  # seconds


# ---------------------------------------------------------------------------
# Helpers
# ---------------------------------------------------------------------------

PASS = "\033[92mPASS\033[0m"
FAIL = "\033[91mFAIL\033[0m"
INFO = "\033[94mINFO\033[0m"

failures: list[str] = []


def _status(ok: bool, label: str, detail: str = "") -> None:
    tag = PASS if ok else FAIL
    suffix = f"  ({detail})" if detail else ""
    print(f"  [{tag}] {label}{suffix}")
    if not ok:
        failures.append(label)


def check(label: str, condition: bool, detail: str = "") -> None:
    _status(condition, label, detail)


def expect_status(resp: requests.Response, expected: int, label: str) -> bool:
    ok = resp.status_code == expected
    _status(ok, label, f"expected {expected}, got {resp.status_code}")
    if not ok:
        try:
            print(f"       body: {resp.text[:300]}")
        except Exception:
            pass
    return ok


def section(title: str) -> None:
    print(f"\n{'=' * 60}")
    print(f"  {title}")
    print(f"{'=' * 60}")


# ---------------------------------------------------------------------------
# API client wrappers
# ---------------------------------------------------------------------------


class ApiClient:
    def __init__(self, base: str) -> None:
        self.base = base.rstrip("/")
        self.session = requests.Session()
        self.session.headers.update({"Content-Type": "application/json", "Accept": "application/json"})

    def _url(self, path: str) -> str:
        return f"{self.base}{path}"

    def get(self, path: str) -> requests.Response:
        return self.session.get(self._url(path), timeout=TIMEOUT)

    def post(self, path: str, body: dict) -> requests.Response:
        return self.session.post(self._url(path), data=json.dumps(body), timeout=TIMEOUT)

    def put(self, path: str, body: dict) -> requests.Response:
        return self.session.put(self._url(path), data=json.dumps(body), timeout=TIMEOUT)

    def delete(self, path: str) -> requests.Response:
        return self.session.delete(self._url(path), timeout=TIMEOUT)


# ---------------------------------------------------------------------------
# Scenario
# ---------------------------------------------------------------------------


def run(base_url: str) -> None:
    api = ApiClient(base_url)

    # Verify the API is reachable before doing anything
    section("0. Health check")
    try:
        r = api.get("/api/components")
        check("API is reachable", r.status_code < 500, f"HTTP {r.status_code}")
    except requests.ConnectionError as exc:
        print(f"  [{FAIL}] Cannot connect to {base_url}: {exc}")
        print("\nIs the API running?  dotnet run --project src/OrderManager.Api")
        sys.exit(1)

    # ------------------------------------------------------------------
    # 1. Components
    # ------------------------------------------------------------------
    section("1. Create components")

    r = api.post("/api/components", {"name": "10kΩ Resistor", "description": "SMD 0402 resistor", "stockQuantity": 10000})
    expect_status(r, 201, "POST /api/components – resistor")
    resistor = r.json()
    resistor_id: str = resistor["id"]
    check("Resistor has id", bool(resistor_id))
    check("Resistor name matches", resistor["name"] == "10kΩ Resistor")

    r = api.post("/api/components", {"name": "100nF Capacitor", "description": "SMD 0402 decoupling cap", "stockQuantity": 5000})
    expect_status(r, 201, "POST /api/components – capacitor")
    capacitor_id: str = r.json()["id"]

    r = api.post("/api/components", {"name": "STM32F4", "description": "ARM Cortex-M4 MCU", "stockQuantity": 200})
    expect_status(r, 201, "POST /api/components – MCU")
    mcu_id: str = r.json()["id"]

    # ------------------------------------------------------------------
    # 2. Read components back
    # ------------------------------------------------------------------
    section("2. Read components")

    r = api.get("/api/components")
    expect_status(r, 200, "GET /api/components")
    all_components = r.json()
    ids_in_list = {c["id"] for c in all_components}
    check("Resistor appears in list", resistor_id in ids_in_list)
    check("MCU appears in list", mcu_id in ids_in_list)

    r = api.get(f"/api/components/{resistor_id}")
    expect_status(r, 200, "GET /api/components/{id}")
    check("Single-fetch name matches", r.json()["name"] == "10kΩ Resistor")

    fake_id = str(uuid.uuid4())
    r = api.get(f"/api/components/{fake_id}")
    expect_status(r, 404, "GET /api/components/{unknown-id} → 404")

    # ------------------------------------------------------------------
    # 3. Update a component
    # ------------------------------------------------------------------
    section("3. Update component")

    r = api.put(f"/api/components/{resistor_id}", {
        "name": "10kΩ Resistor (revised)",
        "description": "SMD 0402, ±1% tolerance",
        "stockQuantity": 12000,
    })
    expect_status(r, 200, "PUT /api/components/{id}")
    check("Updated name reflects change", r.json()["name"] == "10kΩ Resistor (revised)")

    # ------------------------------------------------------------------
    # 4. Create boards
    # ------------------------------------------------------------------
    section("4. Create boards")

    r = api.post("/api/boards", {
        "name": "Main Controller Board",
        "description": "Primary MCU board with power regulation",
        "length": 100.0,
        "width": 80.0,
    })
    expect_status(r, 201, "POST /api/boards – controller board")
    main_board = r.json()
    main_board_id: str = main_board["id"]
    check("Board has id", bool(main_board_id))

    r = api.post("/api/boards", {
        "name": "Sensor Breakout Board",
        "description": "Passive sensor interface board",
        "length": 40.0,
        "width": 30.0,
    })
    expect_status(r, 201, "POST /api/boards – sensor board")
    sensor_board_id: str = r.json()["id"]

    # ------------------------------------------------------------------
    # 5. Wire components onto boards
    # ------------------------------------------------------------------
    section("5. Add components to boards")

    r = api.put(f"/api/boards/{main_board_id}/components/{mcu_id}", {"quantityPerBoard": 1})
    expect_status(r, 204, "PUT board/components – add MCU to main board")

    r = api.put(f"/api/boards/{main_board_id}/components/{capacitor_id}", {"quantityPerBoard": 10})
    expect_status(r, 204, "PUT board/components – add 10 caps to main board")

    r = api.put(f"/api/boards/{main_board_id}/components/{resistor_id}", {"quantityPerBoard": 4})
    expect_status(r, 204, "PUT board/components – add 4 resistors to main board")

    r = api.put(f"/api/boards/{sensor_board_id}/components/{resistor_id}", {"quantityPerBoard": 8})
    expect_status(r, 204, "PUT board/components – add 8 resistors to sensor board")

    # Verify board shows components
    r = api.get(f"/api/boards/{main_board_id}")
    expect_status(r, 200, "GET /api/boards/{id} with components")
    board_data = r.json()
    component_ids_on_board = {bc["component"]["id"] for bc in board_data.get("boardComponents", [])}
    check("MCU is on main board", mcu_id in component_ids_on_board)
    check("Capacitor is on main board", capacitor_id in component_ids_on_board)

    # ------------------------------------------------------------------
    # 6. Update a board
    # ------------------------------------------------------------------
    section("6. Update board")

    r = api.put(f"/api/boards/{main_board_id}", {
        "name": "Main Controller Board v2",
        "description": "Revised layout with better power delivery",
        "length": 110.0,
        "width": 85.0,
    })
    expect_status(r, 200, "PUT /api/boards/{id}")
    check("Board name updated", r.json()["name"] == "Main Controller Board v2")

    # ------------------------------------------------------------------
    # 7. Create an order
    # ------------------------------------------------------------------
    section("7. Create order")

    r = api.post("/api/orders", {
        "name": "Production Run #001",
        "description": "Initial pilot run for the new product line",
        "orderDate": "2026-11-04T08:00:00Z",
    })
    expect_status(r, 201, "POST /api/orders")
    order = r.json()
    order_id: str = order["id"]
    check("Order has id", bool(order_id))
    check("Order name matches", order["name"] == "Production Run #001")

    # ------------------------------------------------------------------
    # 8. Add boards to order
    # ------------------------------------------------------------------
    section("8. Add boards to order")

    r = api.put(f"/api/orders/{order_id}/boards/{main_board_id}", {"quantity": 50})
    expect_status(r, 204, "PUT order/boards – add 50x main board")

    r = api.put(f"/api/orders/{order_id}/boards/{sensor_board_id}", {"quantity": 50})
    expect_status(r, 204, "PUT order/boards – add 50x sensor board")

    # Update quantity
    r = api.put(f"/api/orders/{order_id}/boards/{main_board_id}", {"quantity": 75})
    expect_status(r, 204, "PUT order/boards – update main board qty to 75")

    # Verify order has the boards
    r = api.get(f"/api/orders/{order_id}")
    expect_status(r, 200, "GET /api/orders/{id} with boards")
    order_data = r.json()
    board_ids_in_order = {ob["board"]["id"] for ob in order_data.get("orderBoards", [])}
    check("Main board is in order", main_board_id in board_ids_in_order)
    check("Sensor board is in order", sensor_board_id in board_ids_in_order)

    # Verify updated quantity
    main_entry = next((ob for ob in order_data.get("orderBoards", []) if ob["board"]["id"] == main_board_id), None)
    check("Main board quantity is 75", main_entry is not None and main_entry.get("quantity") == 75)

    # ------------------------------------------------------------------
    # 9. Read orders list
    # ------------------------------------------------------------------
    section("9. Read orders")

    r = api.get("/api/orders")
    expect_status(r, 200, "GET /api/orders")
    all_orders = r.json()
    check("Order appears in list", any(o["id"] == order_id for o in all_orders))

    # ------------------------------------------------------------------
    # 10. Update order
    # ------------------------------------------------------------------
    section("10. Update order")

    r = api.put(f"/api/orders/{order_id}", {
        "name": "Production Run #001 (revised)",
        "description": "Revised quantities after engineering review",
        "orderDate": "2026-11-05T08:00:00Z",
    })
    expect_status(r, 200, "PUT /api/orders/{id}")
    check("Order name updated", r.json()["name"] == "Production Run #001 (revised)")

    # ------------------------------------------------------------------
    # 11. Export order to production line
    # ------------------------------------------------------------------
    section("11. Export order to production line")

    r = api.post(f"/api/orders/{order_id}/export", {})
    expect_status(r, 204, "POST /api/orders/{id}/export")

    # Exporting an unknown order
    r = api.post(f"/api/orders/{fake_id}/export", {})
    expect_status(r, 404, "POST /api/orders/{unknown-id}/export → 404")

    # ------------------------------------------------------------------
    # 12. Remove board from order
    # ------------------------------------------------------------------
    section("12. Remove board from order")

    r = api.delete(f"/api/orders/{order_id}/boards/{sensor_board_id}")
    expect_status(r, 204, "DELETE order/boards – remove sensor board")

    r = api.get(f"/api/orders/{order_id}")
    order_data = r.json()
    remaining_board_ids = {ob["board"]["id"] for ob in order_data.get("orderBoards", [])}
    check("Sensor board no longer in order", sensor_board_id not in remaining_board_ids)
    check("Main board still in order", main_board_id in remaining_board_ids)

    # ------------------------------------------------------------------
    # 13. Remove component from board
    # ------------------------------------------------------------------
    section("13. Remove component from board")

    r = api.delete(f"/api/boards/{main_board_id}/components/{capacitor_id}")
    expect_status(r, 204, "DELETE board/components – remove capacitor")

    r = api.get(f"/api/boards/{main_board_id}")
    board_data = r.json()
    remaining_component_ids = {bc["component"]["id"] for bc in board_data.get("boardComponents", [])}
    check("Capacitor removed from board", capacitor_id not in remaining_component_ids)
    check("MCU still on board", mcu_id in remaining_component_ids)

    # ------------------------------------------------------------------
    # 14. Delete entities (cleanup & 404 guard)
    # ------------------------------------------------------------------
    section("14. Delete entities")

    r = api.delete(f"/api/orders/{order_id}")
    expect_status(r, 204, "DELETE /api/orders/{id}")

    r = api.delete(f"/api/orders/{order_id}")
    expect_status(r, 404, "DELETE /api/orders/{id} (already gone) → 404")

    r = api.delete(f"/api/boards/{main_board_id}")
    expect_status(r, 204, "DELETE /api/boards/{id}")

    r = api.delete(f"/api/boards/{sensor_board_id}")
    expect_status(r, 204, "DELETE /api/boards/{sensor board id}")

    r = api.delete(f"/api/components/{resistor_id}")
    expect_status(r, 204, "DELETE /api/components/{resistor id}")

    r = api.delete(f"/api/components/{capacitor_id}")
    expect_status(r, 204, "DELETE /api/components/{capacitor id}")

    r = api.delete(f"/api/components/{mcu_id}")
    expect_status(r, 204, "DELETE /api/components/{mcu id}")

    # ------------------------------------------------------------------
    # Summary
    # ------------------------------------------------------------------
    section("Summary")
    if failures:
        print(f"\n  {len(failures)} check(s) FAILED:")
        for f in failures:
            print(f"    - {f}")
        print()
        sys.exit(1)
    else:
        print(f"\n  All checks passed.\n")


# ---------------------------------------------------------------------------
# Entry point
# ---------------------------------------------------------------------------

if __name__ == "__main__":
    parser = argparse.ArgumentParser(description="OrderManager API end-to-end test")
    parser.add_argument("--base-url", default=DEFAULT_BASE_URL, help="Base URL of the API (default: %(default)s)")
    args = parser.parse_args()
    run(args.base_url)
