namespace JUSTLockers.Classes;
using System.Collections.Generic;

public class Student : User
{
    public int LockerId { get; set; }
    public Reservation? Reservation { get; set; }
    public HashSet<Report> Reports { get; set; }
    public Department Department { get; set; }

    public Student(string id, string name, string email, string password, int lockerId, Department department)
        : base(id, name, email, password)
    {
        LockerId = lockerId;
        Department = department;
        Reports = new HashSet<Report>();
    }


}
