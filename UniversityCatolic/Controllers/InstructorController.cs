using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Entity;
using System.Data.Entity.Infrastructure;
using System.Linq;
using System.Net;
using System.Web;
using System.Web.Mvc;
using UniversityCatolic.DAL;
using UniversityCatolic.Models;
using UniversityCatolic.ViewModels;

namespace UniversityCatolic.Controllers
{
    public class InstructorController : Controller
    {
        private SchoolContext db = new SchoolContext();

        // GET: Instructor
        public ActionResult Index(int? id, int? courseID)
        {
            var viewModel = new InstructorIndexData();
            viewModel.Instructors = db.Instructors
                .Include(i => i.OfficeAssignment) ////Fast/eager loading - carga rapida/ansiosa
                .Include(i => i.Courses.Select(c => c.Department)) ////Fast/eager loading - carga rapida/ansiosa
                .OrderBy(i => i.LastName);

            if(id != null)
            {
                ViewBag.InstructorID = id.Value;
                viewModel.Courses = viewModel.Instructors.Where(i => i.ID == id.Value).Single().Courses; //Single: solo tendrá un elemento
            }

            if (courseID != null)
            {
                //Lazy Loading
                /*ViewBag.CourseID = courseID.Value;
                viewModel.Enrollments = viewModel.Courses.Where(x => x.CourseID == courseID).Single().Enrollments;*/

                //Explicit Loading - 
                var selectedCourse = viewModel.Courses.Where(x => x.CourseID == courseID).Single();
                db.Entry(selectedCourse).Collection(x => x.Enrollments).Load();
                foreach (Enrollment enrollment in selectedCourse.Enrollments)
                {
                    db.Entry(enrollment).Reference(x => x.Student).Load();
                }

                viewModel.Enrollments = selectedCourse.Enrollments;
            }

            return View(viewModel);
        }

        // GET: Instructor/Details/5
        public ActionResult Details(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            Instructor instructor = db.Instructors.Find(id);
            if (instructor == null)
            {
                return HttpNotFound();
            }
            return View(instructor);
        }

        // GET: Instructor/Create
        public ActionResult Create()
        {
            var instructor = new Instructor();
            //instructor.Courses = new List<Course>(); -> ver modelo de Instructor

            //se llama para crear una coleccion vacia de los cursos, para que no genere una excepcion de referencia nula
            PopulateAssignedCourseData(instructor); 
            return View();
        }

        // POST: Instructor/Create
        // To protect from overposting attacks, enable the specific properties you want to bind to, for 
        // more details see https://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Create([Bind(Include = "LastName,FirstMidName,HireDate,OfficeAssignment")] Instructor instructor, string[] selectedCourses)
        {
            if(selectedCourses != null)
            {
                //instructor.Courses = new List<Course>(); -> ver modelo de Instructor
                foreach (var course in selectedCourses)
                {
                    var courseToAdd = db.Courses.Find(int.Parse(course)); //se obtiene el ID del curso
                    instructor.Courses.Add(courseToAdd); //se agrega el curso a la coleccion 
                }
            }
            if (ModelState.IsValid)
            {
                db.Instructors.Add(instructor);
                db.SaveChanges();
                return RedirectToAction("Index");
            }

            PopulateAssignedCourseData(instructor);
            return View(instructor);
        }

        // GET: Instructor/Edit/5
        public ActionResult Edit(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }

            Instructor instructor = db.Instructors
                .Include(i => i.OfficeAssignment)
                .Include(i => i.Courses)
                .Where(x => x.ID == id)
                .Single();

            if (instructor == null)
            {
                return HttpNotFound();
            }

            PopulateAssignedCourseData(instructor);
            return View(instructor);
        }

        // POST: Instructor/Edit/5
        // To protect from overposting attacks, enable the specific properties you want to bind to, for 
        // more details see https://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Edit(int? id, string[] selectedCourses)
        {
            if(id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }

            //Gets the current Instructor entity from the database using eager loading
            //for the OfficeAssignment navigation property
            var instructorToUpdate = db.Instructors
                .Include(i => i.OfficeAssignment)
                .Include(i => i.Courses)
                .Where(x => x.ID == id)
                .Single();

            //
            if(TryUpdateModel(instructorToUpdate, "", //se pasan los parametros en los que se pueden realizar cambios
                new string[] { "LastName", "FirstMidName", "HireDate", "OfficeAssignment" })) 
            {
                try
                {
                    //si no se selecciona ninguna locacion se iguala a null
                    if (string.IsNullOrWhiteSpace(instructorToUpdate.OfficeAssignment.Location))
                    {
                        instructorToUpdate.OfficeAssignment = null;
                    }

                    UpdateInstructorCourses(selectedCourses, instructorToUpdate);

                    db.SaveChanges();
                    return RedirectToAction("Index");
                }
                catch (RetryLimitExceededException /* dex */)
                {
                    //Log the error (uncomment dex variable name and add a line here to write a log.
                    ModelState.AddModelError("", "Unable to save changes. Try again, and if the problem persists, see your system administrator.");
                }
            }

            PopulateAssignedCourseData(instructorToUpdate);
            return View(instructorToUpdate);
        }

        // GET: Instructor/Delete/5
        public ActionResult Delete(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            Instructor instructor = db.Instructors.Find(id);
            if (instructor == null)
            {
                return HttpNotFound();
            }
            return View(instructor);
        }

        // POST: Instructor/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public ActionResult DeleteConfirmed(int id)
        {
            Instructor instructor = db.Instructors
                .Include(i => i.OfficeAssignment)
                .Where(i => i.ID == id)
                .Single(); //se obtiene el insturctor (incluyendo su oficina) que coincide con el id

            db.Instructors.Remove(instructor);

            //se obtiene el departamento que coincide con el Id del instructor
            var department = db.Departments.Where(d => d.InstructorID == id).SingleOrDefault();
            //si existe un Id del instructor, entonces se anula para que no esté asociado al departamento
            if(department != null)
            {
                department.InstructorID = null;
            }

            db.SaveChanges();
            return RedirectToAction("Index");
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                db.Dispose();
            }
            base.Dispose(disposing);
        }

        //metodo para rellenar los datos de los cursos asignados al instructor
        private void PopulateAssignedCourseData(Instructor instructor)
        {
            var allCourses = db.Courses;
            //se seleccionan los cursos asociados al instructor
            var instructorCourses = new HashSet<int>(instructor.Courses.Select(i => i.CourseID)); //HashSet: Para crear una busqueda eficiente
            var viewModel = new List<AssignedCourseData>();

            foreach (var course in allCourses)
            {
                viewModel.Add(new AssignedCourseData
                {
                    CourseID = course.CourseID,
                    Title = course.Title,
                    Assigned = instructorCourses.Contains(course.CourseID)
                });
            }
            ViewBag.Courses = viewModel;
        }

        //metodo para agregar o quitar cursos al instructor
        private void UpdateInstructorCourses(string[] selectedCourses, Instructor instructorToUpdate)
        {
            if (selectedCourses == null)
            {
                instructorToUpdate.Courses = new List<Course>();
                return;
            }

            var selectedCoursesHS = new HashSet<string>(selectedCourses);
            var instructorCourses = new HashSet<int>(instructorToUpdate.Courses.Select(c => c.CourseID));

            foreach (var course in db.Courses)
            {
                //Si se seleccionó la casilla de verificación de un curso pero el curso no está en la
                //propiedad de navegación Instructor.Courses, el curso se agrega a la colección en la
                //propiedad de navegación
                if (selectedCoursesHS.Contains(course.CourseID.ToString()))
                {
                    if (!instructorCourses.Contains(course.CourseID))
                    {
                        instructorToUpdate.Courses.Add(course);
                    }
                }
                //Si no se seleccionó la casilla de verificación de un curso, pero el curso está en la
                //propiedad de navegación Instructor.Courses, el curso se elimina de la propiedad de navegación.
                else
                {
                    if (instructorCourses.Contains(course.CourseID))
                    {
                        instructorToUpdate.Courses.Remove(course);
                    }
                }
            }
        }
    }
}
