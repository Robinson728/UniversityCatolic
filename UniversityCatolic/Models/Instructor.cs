using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Web;

namespace UniversityCatolic.Models
{
    public class Instructor : Person
    {
        [DataType(DataType.Date)]
        [Display(Name = "Hire Date")]
        [DisplayFormat(DataFormatString = "{0:yyyy-MM-dd}", ApplyFormatInEditMode = true)]
        public DateTime HireDate { get; set; }

        //Sin este codigo tendría que inicializar instructor.Courses = new List<Course>();
        //en el InstructorController cada vez que se vaya a crear un nuevo instructor
        ICollection<Course> _courses;
        public virtual ICollection<Course> Courses
        {
            get
            {
                return _courses ?? (_courses = new List<Course>());
            }

            set
            {
                _courses = value;
            }
        }
        public virtual OfficeAssignment OfficeAssignment { get; set; }
    }
}
