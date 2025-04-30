
namespace JUSTLockers.Classes;
using System.Collections.Generic;
public class Wing
{
    public required string Id { get; set; }
    public HashSet<Level> Levels { get; set; } = new();
    //public required string Section { get; set; }
    public List<Department> Departments { get; set; } = new();

 
}
