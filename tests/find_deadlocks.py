"""
Deadlock finder for OrderManager API.

Strategy: deadlocks occur when two transactions acquire locks in opposite orders.
We provoke this by running concurrent operations on the same shared rows:

  Thread A: AddBoardToOrder(order=X, board=Y)   → locks orders row X, then order_boards
  Thread B: RemoveBoardFromOrder(order=X, board=Y) → locks order_boards, then orders row X

Similarly for board/component relations.
Postgres detects the cycle and kills one transaction with "deadlock detected" (HTTP 500).
"""

import concurrent.futures
import random
import threading
import time
import uuid
import requests

BASE_URL = "http://localhost:8080"
WORKERS = 40          # Concurrent threads during contention phase
ROUNDS = 200          # How many conflicting operation pairs to fire
TIMEOUT = 5           # Request timeout in seconds — a hang here is a deadlock signal
SEED_ORDERS = 5       # Shared orders to fight over (never deleted)
SEED_BOARDS = 5       # Shared boards to fight over (never deleted)
SEED_COMPONENTS = 5   # Shared components to fight over (never deleted)
DELETE_ORDERS = 10    # Separate pool of orders used only for delete contention

stats_lock = threading.Lock()
stats = {
    "deadlock":  0,   # HTTP 500 with "deadlock" in body — confirmed deadlock
    "timeout":   0,   # requests.Timeout — likely deadlock victim hanging
    "not_found": 0,   # HTTP 404 — expected when a delete raced and won
    "error_5xx": 0,   # Other 5xx — unexpected server errors
    "success":   0,
}


# ---------------------------------------------------------------------------
# Helpers
# ---------------------------------------------------------------------------

def post(path, payload):
    resp = requests.post(f"{BASE_URL}{path}", json=payload, timeout=TIMEOUT)
    resp.raise_for_status()
    return resp.json()


def record(category):
    with stats_lock:
        stats[category] += 1


def classify_response(resp):
    if resp.status_code in (200, 201, 204):
        record("success")
    elif resp.status_code == 404:
        record("not_found")
    elif resp.status_code == 500 and "deadlock" in resp.text.lower():
        record("deadlock")
        print(f"\n[DEADLOCK] {resp.text[:200]}")
    else:
        record("error_5xx")
        print(f"\n[{resp.status_code}] {resp.text[:200]}")


# ---------------------------------------------------------------------------
# Setup: create shared entities to contend over
# ---------------------------------------------------------------------------

def create_order(tag):
    return post("/api/orders", {
        "name": f"DL_{tag}_{uuid.uuid4().hex[:4]}",
        "description": "deadlock seed",
        "orderDate": time.strftime("%Y-%m-%dT%H:%M:%SZ"),
    })["id"]


def setup():
    print("Creating seed data...")
    orders = [create_order("Order") for _ in range(SEED_ORDERS)]

    boards = []
    for i in range(SEED_BOARDS):
        b = post("/api/boards", {
            "name": f"DL_Board_{i}_{uuid.uuid4().hex[:4]}",
            "description": "deadlock seed",
            "length": 100.0,
            "width": 50.0,
        })
        boards.append(b["id"])

    components = []
    for i in range(SEED_COMPONENTS):
        c = post("/api/components", {
            "name": f"DL_Comp_{i}_{uuid.uuid4().hex[:4]}",
            "description": "deadlock seed",
            "quantity": 50,
        })
        components.append(c["id"])

    # Create a separate pool of orders used only for delete contention.
    # These are pre-linked to boards so the delete races against an add.
    delete_pool = [create_order("Del") for _ in range(DELETE_ORDERS)]

    # Pre-link all boards to all shared orders and all components to all boards
    # so Remove operations have something to remove.
    for order_id in orders + delete_pool:
        for board_id in boards:
            try:
                requests.put(
                    f"{BASE_URL}/api/orders/{order_id}/boards/{board_id}",
                    json={"quantity": 1},
                    timeout=TIMEOUT,
                )
            except Exception:
                pass

    for board_id in boards:
        for comp_id in components:
            try:
                requests.put(
                    f"{BASE_URL}/api/boards/{board_id}/components/{comp_id}",
                    json={"quantityPerBoard": 1},
                    timeout=TIMEOUT,
                )
            except Exception:
                pass

    print(f"  {len(orders)} shared orders, {len(boards)} boards, {len(components)} components")
    print(f"  {len(delete_pool)} expendable orders for delete contention\n")
    return orders, boards, components, delete_pool


# ---------------------------------------------------------------------------
# Contention operations
# ---------------------------------------------------------------------------

def update_order(order_id):
    try:
        resp = requests.put(
            f"{BASE_URL}/api/orders/{order_id}",
            json={
                "name": f"Updated_{uuid.uuid4().hex[:6]}",
                "description": "concurrent update",
            },
            timeout=TIMEOUT,
        )
        classify_response(resp)
    except requests.Timeout:
        record("timeout")
        print("\n[TIMEOUT] update_order — possible deadlock victim")
    except requests.RequestException as e:
        record("error_5xx")
        print(f"\n[CONN ERROR] update_order: {e}")


def update_board(board_id):
    try:
        resp = requests.put(
            f"{BASE_URL}/api/boards/{board_id}",
            json={
                "name": f"Updated_{uuid.uuid4().hex[:6]}",
                "description": "concurrent update",
                "length": random.uniform(50.0, 200.0),
                "width": random.uniform(25.0, 100.0),
            },
            timeout=TIMEOUT,
        )
        classify_response(resp)
    except requests.Timeout:
        record("timeout")
        print("\n[TIMEOUT] update_board — possible deadlock victim")
    except requests.RequestException as e:
        record("error_5xx")
        print(f"\n[CONN ERROR] update_board: {e}")


def update_component(comp_id):
    try:
        resp = requests.put(
            f"{BASE_URL}/api/components/{comp_id}",
            json={
                "name": f"Updated_{uuid.uuid4().hex[:6]}",
                "description": "concurrent update",
                "quantity": random.randint(1, 100),
            },
            timeout=TIMEOUT,
        )
        classify_response(resp)
    except requests.Timeout:
        record("timeout")
        print("\n[TIMEOUT] update_component — possible deadlock victim")
    except requests.RequestException as e:
        record("error_5xx")
        print(f"\n[CONN ERROR] update_component: {e}")


def add_board_to_order(order_id, board_id):
    try:
        resp = requests.put(
            f"{BASE_URL}/api/orders/{order_id}/boards/{board_id}",
            json={"quantity": random.randint(1, 10)},
            timeout=TIMEOUT,
        )
        classify_response(resp)
    except requests.Timeout:
        record("timeout")
        print("\n[TIMEOUT] add_board_to_order — possible deadlock victim")
    except requests.RequestException as e:
        record("error_5xx")
        print(f"\n[CONN ERROR] add_board_to_order: {e}")


def remove_board_from_order(order_id, board_id):
    try:
        resp = requests.delete(
            f"{BASE_URL}/api/orders/{order_id}/boards/{board_id}",
            timeout=TIMEOUT,
        )
        classify_response(resp)
    except requests.Timeout:
        record("timeout")
        print("\n[TIMEOUT] remove_board_from_order — possible deadlock victim")
    except requests.RequestException as e:
        record("error_5xx")
        print(f"\n[CONN ERROR] remove_board_from_order: {e}")


def add_component_to_board(board_id, comp_id):
    try:
        resp = requests.put(
            f"{BASE_URL}/api/boards/{board_id}/components/{comp_id}",
            json={"quantityPerBoard": random.randint(1, 10)},
            timeout=TIMEOUT,
        )
        classify_response(resp)
    except requests.Timeout:
        record("timeout")
        print("\n[TIMEOUT] add_component_to_board — possible deadlock victim")
    except requests.RequestException as e:
        record("error_5xx")
        print(f"\n[CONN ERROR] add_component_to_board: {e}")


def remove_component_from_board(board_id, comp_id):
    try:
        resp = requests.delete(
            f"{BASE_URL}/api/boards/{board_id}/components/{comp_id}",
            timeout=TIMEOUT,
        )
        classify_response(resp)
    except requests.Timeout:
        record("timeout")
        print("\n[TIMEOUT] remove_component_from_board — possible deadlock victim")
    except requests.RequestException as e:
        record("error_5xx")
        print(f"\n[CONN ERROR] remove_component_from_board: {e}")


def delete_order(order_id):
    try:
        resp = requests.delete(f"{BASE_URL}/api/orders/{order_id}", timeout=TIMEOUT)
        classify_response(resp)
    except requests.Timeout:
        record("timeout")
        print("\n[TIMEOUT] delete_order — possible deadlock victim")
    except requests.RequestException as e:
        record("error_5xx")
        print(f"\n[CONN ERROR] delete_order: {e}")


# ---------------------------------------------------------------------------
# Main contention loop
# ---------------------------------------------------------------------------

def build_task_list(orders, boards, components, delete_pool):
    """
    Four contention patterns, each targeting the same rows from multiple threads:

    Pattern 1 — add vs remove on the same (order, board): classic lock-order inversion
    Pattern 2 — add vs remove on the same (board, component): same, on the other junction
    Pattern 3 — add_board vs delete_order on the same order from the expendable pool:
                 FK/cascade contention; 404s here are expected and counted separately
    Pattern 4 — concurrent updates on the same order/board/component:
                 exercises UpdateAsync which has no concurrency exception handling
    """
    tasks = []
    delete_iter = iter(delete_pool * ((ROUNDS // DELETE_ORDERS) + 1))

    for _ in range(ROUNDS):
        order_id = random.choice(orders)
        board_id = random.choice(boards)
        comp_id  = random.choice(components)
        del_order_id = next(delete_iter)

        # Pattern 1
        tasks.append((add_board_to_order,      (order_id, board_id)))
        tasks.append((remove_board_from_order,  (order_id, board_id)))

        # Pattern 2
        tasks.append((add_component_to_board,      (board_id, comp_id)))
        tasks.append((remove_component_from_board,  (board_id, comp_id)))

        # Pattern 3 — expendable order, 404 after first delete is expected
        tasks.append((add_board_to_order, (del_order_id, board_id)))
        tasks.append((delete_order,       (del_order_id,)))

        # Pattern 4 — concurrent updates on the same shared entities
        tasks.append((update_order,     (order_id,)))
        tasks.append((update_order,     (order_id,)))
        tasks.append((update_board,     (board_id,)))
        tasks.append((update_board,     (board_id,)))
        tasks.append((update_component, (comp_id,)))
        tasks.append((update_component, (comp_id,)))

    random.shuffle(tasks)
    return tasks


def run_contention(orders, boards, components, delete_pool):
    tasks = build_task_list(orders, boards, components, delete_pool)
    total = len(tasks)
    done = 0

    print(f"Firing {total} conflicting operations across {WORKERS} threads...\n")

    with concurrent.futures.ThreadPoolExecutor(max_workers=WORKERS) as executor:
        futures = {
            executor.submit(fn, *args): (fn.__name__, args)
            for fn, args in tasks
        }
        for future in concurrent.futures.as_completed(futures):
            done += 1
            if done % 100 == 0:
                with stats_lock:
                    snapshot = dict(stats)
                print(f"  {done}/{total} done | {snapshot}", end="\r")
            try:
                future.result()
            except Exception as exc:
                print(f"\n[UNHANDLED] {exc}")

    print()


# ---------------------------------------------------------------------------
# Entry point
# ---------------------------------------------------------------------------

def main():
    print(f"Deadlock finder → {BASE_URL}\n")

    try:
        orders, boards, components, delete_pool = setup()
    except Exception as exc:
        print(f"[ERROR] Setup failed: {exc}")
        print("Is the API running?")
        return

    start = time.time()
    run_contention(orders, boards, components, delete_pool)
    elapsed = time.time() - start

    print("\n" + "=" * 45)
    print("  DEADLOCK FINDER RESULTS")
    print("=" * 45)
    total = sum(stats.values())
    for key, val in stats.items():
        pct = f"({val/total*100:.1f}%)" if total else ""
        print(f"  {key:<12} {val:>5}  {pct}")
    print(f"\n  Total requests : {total}")
    print(f"  Elapsed        : {elapsed:.1f}s")
    print("=" * 45)

    if stats["deadlock"] > 0 or stats["timeout"] > 0:
        print("\n[!] Deadlock signals detected — check output above for details.")
    elif stats["error_5xx"] > 0:
        print(f"\n[!] {stats['error_5xx']} unexpected 5xx errors — check [NNN] lines above.")
    else:
        print("\n[OK] No deadlocks detected in this run.")


if __name__ == "__main__":
    main()
