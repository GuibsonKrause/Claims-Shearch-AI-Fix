# Claims Search: fix for MYT-3108 (SQL injection)

A small C# (.NET 8) project that simulates the claims search of a health-benefits website.
It was used to practice reviewing and fixing a security fix made by an AI agent.

## What the program does

`ClaimsRepository.Search` (in `src/Claims/ClaimsRepository.cs`) searches the claims of **one member**:

- Returns only the claims of the given member.
- Finds claims whose description **or** provider **contains** the text, in any case
  (`dental` finds "Dental cleaning" and "Dental X-ray").
- Supports names with apostrophes (`O'Brien` finds "O'Brien Eye Clinic").
- Sorts by `date`, `amount` or `status` (`sortBy`), in `asc` or `desc` order (`sortDir`).
  Any other `sortBy` is rejected with `ArgumentException`.
- Paginates at 20 results per page (`page` starts at 0; a negative value throws `ArgumentOutOfRangeException`).

Sample data comes from an in-memory SQLite database (`Db.CreateSeeded`). In production the database is SQL Server.

## The problem (MYT-3108)

The original search built its SQL by joining the search text and the sort option into the query.
Searching for `' OR 1=1 --` returned the claims of **other members** (SQL injection, CWE-89).

An AI agent tried to fix it, but its fix was wrong:

| Problem in the agent's fix | Consequence |
|---|---|
| `sortBy` was still joined into the SQL (it only removed quotes and `;`) | Still injectable, for example `amount, (SELECT name FROM members)` |
| It removed apostrophes from the search text | `O'Brien` was no longer found |
| It changed the search to exact match | Broke the partial search the product needs |
| The test `Search_ExactMatchOnly_BlocksWildcardEnumeration` protected the wrong behavior | It fails as soon as the search is fixed |

## What was fixed

- **Sorting:** a fixed list of options (`date`, `amount`, `status`) mapped to fixed column names.
  No user input reaches the SQL text.
- **Search:** the text goes only as a parameter, with nothing removed, using a partial `LIKE`.
  The characters `\`, `%`, `_` and `[` typed by the user are escaped (`ESCAPE '\'`), so `100%` is matched literally.
- **Pagination:** a negative `page` is rejected before it reaches the database.
- **Tests:** the agent's exact-match test was replaced by a partial-match test.
  `MyTests.cs` adds tests that pass on the fixed code and fail on the agent's version.

More details in `NOTES.md`.

## Layout

- `FINDING.md`: the problem and the rules for how the search must behave.
- `AGENT_NOTES.md`: what the agent says it did, and the original code.
- `src/Claims/`: the code (`ClaimsRepository.cs` and `Db.cs`).
- `tests/Claims.Tests/`: the agent's tests (`AgentTests.cs`) and the new tests (`MyTests.cs`).
- `check/`: program that scores the fix (do not edit).
- `NOTES.md`: what the agent proposed, what changed, and the evidence.

## How to run

Tests:

```
dotnet test tests/Claims.Tests
```

Scoring check:

```
dotnet run --project check
```

## Caveat

The tests run on SQLite. Since production uses SQL Server, validate `LIKE ... ESCAPE` there,
including the escaping of `[` (a character-set wildcard in SQL Server) and the collation's case sensitivity.
