namespace Demo.Application.Utils;

public static class StringUtil
{
    public static bool CheckPassword(string salt, string password, string storedPassword)
    {
        using var sha256Hash = System.Security.Cryptography.SHA256.Create();
        byte[] hashBytes = sha256Hash.ComputeHash(System.Text.Encoding.UTF8.GetBytes(salt + password));
        string hashedPassword = BitConverter.ToString(hashBytes).Replace("-", "");
        return string.Equals(hashedPassword, storedPassword);
    }
}
