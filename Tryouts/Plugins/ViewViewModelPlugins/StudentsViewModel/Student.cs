using NP.Concepts.Behaviors;
using System;

namespace MorganStanley.GridSelectionPrototype
{
    public class Student : SelectableItem<Student>
    {
        public int StudentID { get; set; } 

        public string? FirstName { get; set; }

        public string? LastName { get; set; }   
        
        public string? Major { get; set; }

        public override string ToString()
        {
            return $"{FirstName} {LastName}";
        }
    }
}
