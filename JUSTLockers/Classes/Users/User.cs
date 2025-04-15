namespace JUSTLockers.Classes;

public class User
{
    public int Id { get; set; }
    public string Name { get; set; }
    public string Email { get; set; }
    //public string Password { get; set; }
    
    public User()
    {
        // Default constructor
    }
    public User(int id, string name, string email/*, string password*/)
    {
        Id = id;
        Name = name;
        Email = email;
        //Password = password;
    }
}
