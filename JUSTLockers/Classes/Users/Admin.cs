namespace JUSTLockers.Classes;
using JUSTLockers.Services;
using System.Collections.Generic;
public class Admin : User
{
    public List<Reallocation> ReallocationRequests { get; set; }
    public List<Report> ReportList { get; set; }
    public HashSet<Supervisor> Supervisors { get; set; }

    public Admin(string id, string name, string email, string password)
        : base(id, name, email, password)
    {
        ReallocationRequests = new List<Reallocation>();
        ReportList = new List<Report>();
        Supervisors = new HashSet<Supervisor>();
    }


}
