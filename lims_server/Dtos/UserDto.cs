namespace LimsServer.Dtos
{
    public class UserDto
    {
        public int Id { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public string Username { get; set; }
        public bool Enabled { get; set; } = true;
        public string Password { get; set; }
    }
}