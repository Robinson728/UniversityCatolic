namespace UniversityCatolic.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class Inheritance : DbMigration
    {
        public override void Up()
        {
            // Drop foreign keys and indexes that point to tables we're going to drop.
            //Elimina las claves externas (foraneas) y los índices que apuntan a las tablas que vamos a eliminar.
            DropForeignKey("dbo.Enrollment", "StudentID", "dbo.Student");
            DropIndex("dbo.Enrollment", new[] { "StudentID" });

            RenameTable(name: "dbo.Instructor", newName: "Person");
            AddColumn("dbo.Person", "EnrollmentDate", c => c.DateTime());
            AddColumn("dbo.Person", "Discriminator", c => c.String(nullable: false, maxLength: 128));
            AlterColumn("dbo.Person", "HireDate", c => c.DateTime());
            AddColumn("dbo.Person", "OldId", c => c.Int(nullable: true));//columna agregada

            // Copy existing Student data into new Person table.
            //Copiar los datos existentes de Students en la nueva tabla Person
            Sql("INSERT INTO dbo.Person (LastName, FirstName, HireDate, EnrollmentDate, Discriminator, OldId) SELECT LastName, FirstName, null AS HireDate, EnrollmentDate, 'Student' AS Discriminator, ID AS OldId FROM dbo.Student");

            // Fix up existing relationships to match new PK's (primary key).
            //Corregir las relaciones existentes para que coincidan con las nuevas claves primarias
            Sql("UPDATE dbo.Enrollment SET StudentId = (SELECT ID FROM dbo.Person WHERE OldId = Enrollment.StudentId AND Discriminator = 'Student')");

            // Remove temporary key
            // Eliminar la clave temporal
            DropColumn("dbo.Person", "OldId");

            DropTable("dbo.Student");

            // Re-create foreign keys and indexes pointing to new table.
            // Vuelva a crear claves externas e índices que apunten a la nueva tabla.
            AddForeignKey("dbo.Enrollment", "StudentID", "dbo.Person", "ID", cascadeDelete: true);
            CreateIndex("dbo.Enrollment", "StudentID");
        }
        
        public override void Down()
        {
            CreateTable(
                "dbo.Student",
                c => new
                    {
                        ID = c.Int(nullable: false, identity: true),
                        LastName = c.String(nullable: false, maxLength: 50),
                        FirstName = c.String(nullable: false, maxLength: 50),
                        EnrollmentDate = c.DateTime(nullable: false),
                    })
                .PrimaryKey(t => t.ID);
            
            AlterColumn("dbo.Person", "HireDate", c => c.DateTime(nullable: false));
            DropColumn("dbo.Person", "Discriminator");
            DropColumn("dbo.Person", "EnrollmentDate");
            RenameTable(name: "dbo.Person", newName: "Instructor");
        }
    }
}
