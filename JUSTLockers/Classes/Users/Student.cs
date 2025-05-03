namespace JUSTLockers.Classes;
using System.Collections.Generic;

public class Student : User
{
    public String LockerId { get; set; }
    public Reservation? Reservation { get; set; }
    public HashSet<Report> Reports { get; set; }
    public string Department { get; set; }
    public string? Location { get; set; }
    public bool IsBlocked { get; set; }

    //public Student(string id, string name, string email, string password, int lockerId, Department department)
    //    : base(id, name, email, password)
    //{
    //    LockerId = lockerId;
    //    Reports = new HashSet<Report>();
    //}

    public Student(int id, string name, string email/*, string password*/, string department, string? location)
        : base(id, name, email/*, password*/)
    {
        Department = department;
        Reports = new HashSet<Report>();
        Location = location;
    }

    public Student(int id, string name, string email/*, string password*/, string department)
       : base(id, name, email/*, password*/)
    {
        Department = department;
        Reports = new HashSet<Report>();
      
    }
}
