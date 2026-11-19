using Microsoft.Data.Sqlite;

namespace Claims;

public class ClaimsRepository
{
    private readonly SqliteConnection _db;

    public ClaimsRepository(SqliteConnection db) => _db = db;

    // Search the claims of one member. Called by the claims page of the member website and by the support tool.
    public IReadOnlyList<Claim> Search(int memberId, string? text, string? sortBy = "date", string? sortDir = "desc", int page = 0)
    {
        // MYT-3108: every value is a parameter; sort column and direction come from fixed whitelists.
        var column = (sortBy ?? "date") switch
        {
            "date" => "submitted_at",
            "amount" => "amount",
            "status" => "status",
            _ => throw new ArgumentException("sortBy must be date, amount or status.", nameof(sortBy)),
        };
        if (page < 0) throw new ArgumentOutOfRangeException(nameof(page), "page must be 0 or greater.");
        var direction = sortDir?.ToLowerInvariant() == "asc" ? "ASC" : "DESC";

        // Partial, case-insensitive match; user-typed \, %, _ and [ are escaped so they match literally.
        var pattern = "%" + (text ?? "").Replace(@"\", @"\\").Replace("%", @"\%").Replace("_", @"\_").Replace("[", @"\[") + "%";

        using var cmd = _db.CreateCommand();
        cmd.CommandText =
            "SELECT id, provider, description, amount, status, submitted_at FROM claims " +
            @"WHERE member_id = @member AND (description LIKE @text ESCAPE '\' OR provider LIKE @text ESCAPE '\') " +
            $"ORDER BY {column} {direction} LIMIT 20 OFFSET @offset";
        cmd.Parameters.AddWithValue("@member", memberId);
        cmd.Parameters.AddWithValue("@text", pattern);
        cmd.Parameters.AddWithValue("@offset", page * 20);

        var results = new List<Claim>();
        using var reader = cmd.ExecuteReader();
        while (reader.Read())
            results.Add(new Claim(reader.GetInt64(0), reader.GetString(1), reader.GetString(2),
                                  reader.GetDouble(3), reader.GetString(4), reader.GetString(5)));
        return results;
    }
}
