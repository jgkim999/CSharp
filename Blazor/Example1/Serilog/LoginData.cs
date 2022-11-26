namespace Example1.Serilog
{
    public class LoginData
    {
        public string Username { get; set; }
        // ReSharper disable once NotAccessedField.Global
        public string Password { get; set; }

        public LoginData(string username = "", string password = "")
        {
            Username = username;
            Password = password;
        }
    }
}
