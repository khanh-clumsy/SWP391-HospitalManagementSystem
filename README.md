Hospital Management System
A web-based DBMS project for basic CRUD operations in a hospital system.
About
The Hospital Management System (HMS) is a web-based application designed to manage hospital records, enabling efficient creation, retrieval, updating, and deletion of patient, doctor, and appointment information. The system also supports managing hospital departments and patient medical records, providing a streamlined solution for healthcare institutions.
Key functionalities of the Hospital Management System include:

Create Patient: Users can input patient details such as ID, first name, last name, date of birth, and contact information to create a new patient record in the database.

Show Patients: Retrieves and displays a list of all patients stored in the database, including their basic information like name, date of birth, and contact details.

Update Patient: Allows users to update an existing patient’s information by providing their ID and modifying fields such as first name, last name, date of birth, or contact details.

Delete Patient: Enables the removal of a patient record by specifying the patient’s ID.

Manage Appointments: Users can schedule, update, or cancel appointments by selecting a patient ID, doctor ID, and appointment date.

Insert Medical Records: Allows users to add and manage medical records for a patient, including diagnosis, treatment, and prescription details.

Search Patient: Enables searching for a specific patient by their ID, displaying detailed information such as medical history and scheduled appointments.

View Doctors and Departments: Users can view a list of available doctors and hospital departments, with the ability to add new doctors or departments.


The system interacts with a database using SQL commands for data retrieval, insertion, updating, and deletion. It includes robust error handling to provide feedback on the success or failure of database operations.
Overall, the Hospital Management System serves as an efficient tool for managing hospital operations and patient data, making it suitable for healthcare facilities seeking a straightforward solution for data management. Future enhancements could include a more advanced user interface, analytics for patient care trends, and integration with billing systems.
Dependencies

C#
.NET SDK (8.0)
Microsoft SQL Server (SQL Server 2022)
Microsoft.Data.SqlClient (5.2.0)

How to Use

Clone the repository.
Create a new database or import the database from a provided backup.
Open the project in Visual Studio Code.
Install the C# Dev Kit extension.
Run the project.


[!Note]You can also use Visual Studio or your preferred IDE to run this project.

Error Handling

Be patient while all packages are restored in the project root folder.

Microsoft.Data.SqlClient error:If packages are not restored automatically, run dotnet restore in the terminal or install the Microsoft.Data.SqlClient package using:  
dotnet add package Microsoft.Data.SqlClient

Alternatively, download it from NuGet.

Database Connection error:  

Verify your database connection settings.  
Update the connectionString in the Program.cs file to match your database configuration:  // Database connection string
static string connectionString = "Data Source=localhost;Initial Catalog=HospitalManagementSystem;Integrated Security=True;Encrypt=False";




