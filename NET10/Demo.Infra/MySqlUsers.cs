using SqlKata;
using SqlKata.Compilers;

namespace Demo.Infra;

public static class MySqlUsers
{
    static MySqlCompiler Compiler = new MySqlCompiler();

    public static SqlResult SelectQuery(string email)
    {
        var q = new Query("Users").Where("email", email);
        return Compiler.Compile(q);
    }

    public static SqlResult InsertQuery(string email, string password, string salt, DateTime createdAt,
        DateTime updatedAt)
    {
        var q = new Query("Users").AsInsert(new
        {
            email = email,
            password = password,
            salt = salt,
            created_at = createdAt,
            updated_at = updatedAt
        });
        return Compiler.Compile(q);
    }
}
