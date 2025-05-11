using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;

namespace JUSTLockers.Classes;

public class Department
{
    public string Name { get; set; }


    public int Total_Wings { get; set; }

 
    public string Location { get; set; }

}