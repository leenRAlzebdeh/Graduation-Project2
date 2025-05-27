namespace JUSTLockers.Classes;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

public class Employee : User
{
   // public string Location { get; set; }
   // public string DepartmentName { get; set; }
    //public Department SupervisedDepartment { get; set; }
    
   

    public Employee(int id, string name, string email, Department department,string location)
        : base(id, name, email)
    {
       // SupervisedDepartment = department;
      // this.Location = location;
    }
}
