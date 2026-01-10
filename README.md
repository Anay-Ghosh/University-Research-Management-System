# LifeNet Assist

**LifeNet Assist** is a **Smart Volunteer & Emergency Response System** built with **ASP.NET MVC 8**, **C#**, and **Entity Framework**. It allows users to register as **Admin, Requester, or Volunteer**, create help requests, and efficiently manage emergency situations with location-based volunteer assignment.

---

## Table of Contents
- [Features](#features)
- [Technologies Used](#technologies-used)
- [Project Structure](#project-structure)
- [Screenshots](#screenshots)
- [Getting Started](#getting-started)
- [Usage](#usage)
- [Future Enhancements](#future-enhancements)
- [License](#license)

---

## Features

### 1. User Roles
- **Admin:** Full control over system, manage requests and volunteers.
- **Requester:** Create help requests with location and contact information.
- **Volunteer:** View assigned tasks, update status, and manage profile/location.

### 2. Help Request Management
- Requesters can **create, edit, and delete help requests**.
- Requests automatically have **status tracking**: Pending → Assigned → Completed.
- Admins can **assign volunteers** to requests.

### 3. Volunteer Management
- Admins can **view all registered volunteers**, including location coordinates.
- Track the **closest volunteers to pending requests**.
- Volunteers can **update their profiles and location**.

### 4. Dashboard
- **Volunteer Dashboard:** Total assigned requests, pending/completed requests, quick links to tasks, and profile management.
- **Admin Dashboard:** Total requests, pending/completed requests, total volunteers, and a table showing closest volunteers to pending requests.
- **Requester Dashboard:** View all created requests with status and actions.

### 5. Real-time Location & Distance
- Track **volunteer location** and request location.
- Calculate **distance between volunteers and requests** for efficient assignment.

### 6. Notifications & Alerts
- Success and error alerts for all CRUD operations.
- Warning alerts if profiles are incomplete.

---

## Technologies Used
- **Backend:** ASP.NET MVC 8, C#, Entity Framework
- **Frontend:** Razor Views, Bootstrap 5, HTML5, CSS3
- **Database:** SQL Server / LocalDB
- **Libraries:** jQuery, Bootstrap
- **Tools:** Visual Studio 2022, Git & GitHub

---

## Project Structure
