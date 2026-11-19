# MYT-3108 notes

## What the agent proposed

The agent made the search text a query parameter, which is correct and closes the injection in the text. But it left the sort option joined into the SQL after only removing quotes and semicolons, so `amount, (SELECT name FROM members)` still injects. It also stripped apostrophes from the search text, so "O'Brien" was never found, and it switched to exact matching, which breaks the "contains" rule in FINDING.md. Its test Search_ExactMatchOnly_BlocksWildcardEnumeration locked in that wrong behavior.

## What I changed and why

Sorting now uses a whitelist: date, amount or status map to fixed column names, and anything else throws ArgumentException, so no user input reaches the SQL text. The search text goes only through a parameter, with no stripping, so apostrophes work. I match with LIKE and wrap the text in percent signs for partial, case-insensitive search. I escape %, _ and backslash in the text with an ESCAPE clause so "100%" is matched literally. I replaced the agent's wrong exact-match test with one that expects partial match.

## Evidence

Tests in MyTests.cs cover partial match, O'Brien, rejected sort values (subquery, semicolon, raw column, empty), literal percent and member isolation. The partial-match, O'Brien and sort tests fail on the agent's version. Run dotnet test tests/Claims.Tests and dotnet run --project check. Caveat: production is SQL Server, where LIKE ESCAPE works but I only ran this on SQLite, so verify there.
