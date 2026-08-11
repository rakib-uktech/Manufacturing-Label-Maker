# Manufacturing Label Maker

**Enterprise web-based label configuration and printing system for standardising production label generation in manufacturing environments.**

---

## Overview

Manufacturing environments require accurate and consistent label printing for products, packaging, inventory, and logistics operations. Manufacturing Label Maker provides a centralised platform where operators can configure label data, select approved templates, and send print jobs to connected label printers — maintaining standard operating procedures and full production traceability across every label printed.

The system reduces human error, improves traceability, and standardises production labelling across manufacturing, packaging, warehouse, and logistics operations.

The system was independently designed, developed, and deployed by me, and is currently in active daily use.

---

## Key Features

**Label Configuration**
- Enter Work Order numbers and define label quantities
- Configure label details before printing

**Template-Based Label Printing**
- Select from predefined, approved label templates
- Prevent unauthorised template modifications
- Ensure standardised label format across operations

**Printer Device Management**
- Connect to approved network label printers
- Select the target printer device and configure settings before printing

**Production Label Printing**
- Send print jobs directly from the application
- Print single labels or full batches
- Minimise label configuration errors

**Label Verification Workflow**
- Preview labels before printing
- Validate data fields and formatting
- Ensure barcode and QR code readability

**Training and Help Support**
- User training documentation
- Video demonstration support
- Step-by-step instructions for label operators

---

## System Architecture

The application follows a standard web-based architecture: users interact with a web interface, which communicates with backend services responsible for processing label data and managing print jobs.
<img width="883" height="588" alt="image" src="https://github.com/user-attachments/assets/f3d0f048-282f-49d2-a91a-dc9419c274ea" />


**Frontend**
- Razor Pages
- HTML, JavaScript

**Backend**
- C#
- ASP.NET Core

**Printing**
- Network label printers

**Deployment**
- Internal web server

---

## Technologies Used

| Layer         | Technology                         |
|---------------|--------------------------------------|
| Backend       | C#                                  |
| Framework     | ASP.NET Core                       |
| Frontend      | Razor Pages, HTML, JavaScript      |
| Printing      | Network Label Printers             |
| Deployment    | Internal Web Server                |
| Documentation | Markdown                           |

---

## Video Walkthrough

[Full Demonstration on YouTube](https://youtu.be/C8OcmeLTRG8)

> The live system runs on the organisation's internal network for security reasons and is not publicly accessible — the video above is the best way to see it in action.

---

## Screenshots

The following screenshots demonstrate the main label management, template configuration, label generation, preview, and production printing capabilities of the Manufacturing Label Maker.

### 1. Label Management & Dashboard

#### Label Management Dashboard

Provides a centralised interface for managing production labels, label configurations, and printing activities.

<img width="891" height="426" alt="Label Management Dashboard" src="https://github.com/user-attachments/assets/129c1aac-5dae-4da6-b306-9c1fcae6b436" />

#### Label List & Search

Allows users to search, review, and manage configured labels and associated production information.

<img width="869" height="473" alt="Label List and Search" src="https://github.com/user-attachments/assets/4277e12c-d219-4bf9-8ed4-813f148c65d4" />

---

### 2. Template Management & Label Generation

#### Label Template Selection

Allows operators to select approved label templates before generating production labels, helping maintain standardised labelling across manufacturing operations.

<img width="480" height="767" alt="Label Template Selection" src="https://github.com/user-attachments/assets/73403535-1970-4780-a91c-5eb322decfca" />

---

### 3. Label Preview & Verification

#### Label Preview

Provides a preview of the generated label before printing, allowing operators to verify label content, formatting, and production information.

<img width="505" height="627" alt="Label Preview Screen" src="https://github.com/user-attachments/assets/dfff3b6b-3b77-4fcf-b68d-2398eb1d175b" />

---

### 4. Production Label Printing

#### Production Label Printing

Provides the final production printing workflow, allowing operators to generate and send approved labels to connected network label printers.

<img width="449" height="601" alt="Production Label Printing" src="https://github.com/user-attachments/assets/17fcddd8-1b76-480f-b846-5ce40afe1f7d" />

---

## Installation

Clone the repository:

```bash
git clone https://github.com/rakib-usw/Manufacturing_Label_Maker.git
```

Open the project in Visual Studio and run:

```bash
dotnet run
```

---

## About This Project

This project forms part of a wider portfolio of enterprise software systems designed and built to support manufacturing digital transformation, including a Manufacturing Task Management System, a Laboratory Information Management System (LIMS), a Consumable Asset Management System, and a Vehicle Inspection & Booking System — each independently architected, developed, and deployed to production.

## Author

**Rakib Ahmed**
Senior Software Engineer | Full-Stack .NET Developer | Manufacturing Systems Engineer

[LinkedIn](https://www.linkedin.com/in/rakibuddinahmed) · [GitHub](https://github.com/rakib-usw) · [YouTube](https://www.youtube.com/@RakibsTechStudio)

---

## License

This project is shared for demonstration and portfolio purposes.
