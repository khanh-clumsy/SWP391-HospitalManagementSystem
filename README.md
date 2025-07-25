<h1 align="center">Hospital Management System</h1>
<h4 align="center">A web-based DBMS project for basic <strong>CRUD</strong> operations in a hospital system.</h4>


## About
The Hospital Management System (HMS) is a comprehensive web-based application designed to streamline and manage core hospital operations, including patient registration, appointment scheduling, medical records, billing, and administrative control. The system enhances the efficiency and accuracy of healthcare services by allowing patients and administrators to interact with hospital data in a structured and secure environment.

### Key functionalities of this Hospital Management System include:

1. **Book Appointment**: Patients can create a new booking by entering their personal details (patient ID, full name, date of birth, gender, contact information) and selecting a preferred doctor and time slot. The system checks availability and confirms the appointment.

2. **Manage Schedule**: Administrators have full control over appointments. They can update, reschedule, cancel, or confirm appointments. This ensures flexibility in daily hospital operations and improved communication with patients.

3. **Appointment Management**: Users can update the information of an existing student by providing their ID and modifying their first name, last name, date of birth, and department ID.

4. **Patient Registration and Records**: New patients can be registered into the system, and existing records can be updated. The system stores essential medical information such as allergies, diagnosis history, and treatment plans.

5. **Assign Doctor to Patient**: Patients can be assigned or referred to specific doctors based on their condition, department, or preference. This helps in maintaining a clear association between patient and caregiver throughout treatment.

6. **Diagnosis and Billing**: Doctors can input diagnosis and treatment details, prescribe medications or lab tests, and the system will automatically generate the bill. The billing module supports itemized services, payment tracking, and invoice generation.

7. **Manage Doctors and Departments**: Admins can add, update, or remove doctor profiles and department details. This ensures an up-to-date structure of hospital departments and specialties.

The application interacts with a SQL Server database using SQL queries for inserting, updating, retrieving, and deleting data. Robust error handling ensures reliable execution of operations and provides users with real-time feedback.

Overall, this Hospital Management System offers a structured and scalable solution for hospitals and clinics seeking to modernize their operations. It improves efficiency, minimizes paperwork, enhances patient care, and allows administrators to make data-driven decisions. Future enhancements may include integration with insurance systems, electronic prescriptions, and mobile accessibility.

## Dependencies

- ASP.NET core MVC
- .NET SDK (8.0.403)
- Microsoft SQL Server (SQL Server 2022)

## How to use

1. Clone the repository
2. Create Database _or_ Import Database from backup
3. Open the project in Visual Studio Code
4. Install C# Dev Kit
5. Run the project

>[!Note]
>You can also use Visual Studio _or_ your favorite IDE to run this project.
