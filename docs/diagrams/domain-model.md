# Domain Model — Entity Relationship Diagram

Three core entities linked by two junction tables for the many-to-many relationships.

```mermaid
erDiagram
    ORDER {
        Guid     Id          PK
        string   Name
        string   Description
        DateTime OrderDate
    }
    BOARD {
        Guid   Id          PK
        string Name
        string Description
    }
    COMPONENT {
        Guid   Id          PK
        string Name
        string Description
    }
    ORDER_BOARD {
        Guid OrderId     FK
        Guid BoardId     FK
    }
    BOARD_COMPONENT {
        Guid BoardId     FK
        Guid ComponentId FK
    }

    ORDER       ||--o{ ORDER_BOARD      : "has"
    BOARD       ||--o{ ORDER_BOARD      : "assigned to"
    BOARD       ||--o{ BOARD_COMPONENT  : "contains"
    COMPONENT   ||--o{ BOARD_COMPONENT  : "placed on"
```

## Notes

- All primary keys are auto-generated `Guid` values.
- `ORDER_BOARD` and `BOARD_COMPONENT` are pure junction tables with no additional payload — EF Core's `UsingEntity` maps them without a dedicated entity class.
- One `Board` can appear in many `Orders`; one `Component` can appear on many `Boards`.
