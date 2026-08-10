# Manufacturing Label Maker

**Enterprise web-based label configuration and printing system for standardising production label generation in manufacturing environments.**

<img width="1536" height="1024" alt="Manufacturing Label Maker — Software Architecture Diagram" src="https://github.com/user-attachments/assets/6706ce29-54b0-49a9-9850-8b3893418e20" />

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

<img width="503" height="430" alt="Label Configuration Screen" src="https://github.com/user-attachments/assets/faea2292-5d1d-4418-a231-9f1751dfcff8" />

<img width="446" height="580" alt="Label Template Selection" src="https://github.com/user-attachments/assets/651bbf37-636e-4ff7-949f-cfc122646402" />

<img width="1881" height="657" alt="Label Management Dashboard" src="https://github.com/user-attachments/assets/883a1d6a-ab57-4e63-95d9-0734af57e6bd" />

<img width="456" height="641" alt="Label Preview Screen" src="https://github.com/user-attachments/assets/8580c9ce-95ae-4c55-98b0-956d67ca86f5" />

<img width="509" height="707" alt="Barcode and QR Code Generation" src="https://github.com/user-attachments/assets/13f62932-d398-402d-baf3-3c5ded1e7177" />

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
