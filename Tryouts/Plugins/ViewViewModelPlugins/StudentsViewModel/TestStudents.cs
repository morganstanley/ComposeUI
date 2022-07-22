using NP.Concepts.Behaviors;
using NP.Utilities.Attributes;
using NP.Utilities.PluginUtils;

namespace MorganStanley.GridSelectionPrototype
{
    [Implements(typeof(IPlugin), partKey:"StudentsViewModel", IsSingleton = true)]
    public class TestStudents : SingleSelectionObservableCollection<Student>, IPlugin
    {
        private int _currentId = 0;

        private void AddStudent(string firstName, string lastName, string major)
        {
            _currentId++;

            Student student = new Student { StudentID = _currentId, FirstName = firstName, LastName = lastName, Major = major };

            this.Add(student);
        }

        public TestStudents()
        {
            AddStudent("Joe", "Doe", "Math");
            AddStudent("Sonny", "Corleone", "Liberal Arts");
            AddStudent("Johny", "Fontane", "Singing");
        }

        public override string? ToString()
        {
            return TheSelectedItem?.ToString() ?? base.ToString();
        }
    }
}
