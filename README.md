<p align="center"><h1 align="center">DOMINI SHOP</h1></p>
<p align="center">
	<em>WINDOW DESKTOP APP</em>
</p>
<br>

## 🔗 Table of Contents

- [📍 Overview](#-overview)
- [👥 Team Members](#-team-members)
- [🛠 Tech Stack](#-tech-stack)
- [📁 Project Structure](#-project-structure)
- [👾 Features](#-features)
  - [✅ Completed Functions](#-completed-functions)
  - [❌ Uncompleted Functions](#-uncompleted-functions)
  - [⭐ Functions That Have Been Invested in Significant Research Time](#-functions-that-have-been-invested-in-significant-research-time)
- [🎥 Demo](#-demo)
- [📊 Member Self-Assessment](#-member-self-assessment)
- [📜 License](#-license)
---

# 📍 Overview

This is a winUI3 app provides sales management features for a small food store.  
The system is designed for a single owner who also acts as the salesperson, inventory manager, and delivery staff.

---

# 👥 Team members

| Fullname | Student ID | Role |
|---|---|---|
| Tran Khon Chi | 23127032 | Fullstack |
| Pham Thanh Dat | 23127170 | Fullstack |
| Mai Xuan Hung | 23127372 | Fullstack |
| Nguyen Van Minh | 23127422 | Fullstack |
| Giao Thai Bao | 23127526 | Fullstack |

---

# Tech Stack
 
| Layer | Technology |
|---|---|
| UI Framework | WinUI 3 (Windows App SDK 2.0) |
| Language | C# 14 / .NET 10 |
| Architecture | MVVM (CommunityToolkit.Mvvm 8.4) |
| ORM | Entity Framework Core 10 + Npgsql |
| Database | PostgreSQL via Supabase |
| Auth/Realtime | Supabase SDK 1.1 |
| DI Container | Microsoft.Extensions.DependencyInjection 10 |
| Configuration | Microsoft.Extensions.Configuration + JSON |
| Data Grid | CommunityToolkit.WinUI.UI.Controls.DataGrid |
| Target OS | Windows 10 1903+ (build 17763+) |
| Platforms | x86, x64, ARM64 |

---

# 📁 Project Structure

```
DominiShop/                          ← Solution root
├── DominiShop.slnx                  ← Solution file
└── client/                          ← Main project folder
    ├── DominiShop.csproj            ← Project file (NuGet refs, build config)
    ├── App.xaml                     ← Application resources & theme
    ├── App.xaml.cs                  ← App entry point + DI service registration
    ├── app.manifest                 ← Windows app manifest
    ├── appsettings.json             ← Connection strings & Supabase config
    │
    ├── Assets/                      ← App icons and splash screen images
    │
    ├── Model/                       ← EF Core entity models
    │   ├── BaseModel.cs             ← INotifyPropertyChanged + ICloneable base
    │
    ├── DataAccess/                  ← EF Core DbContext
    │   ├── PostgresContext.cs       ← DbSets + Fluent API model configuration
    │   └── PostgresContext.Extension.cs  ← OnConfiguring (reads appsettings.json)
    │
    ├── Repository/                  ← Data access layer (raw DB queries)
    │
    ├── Service/                     ← Business logic layer
    │
    ├── ViewModel/                   ← Presentation logic (MVVM)
    │
    ├── View/                        ← XAML pages & windows
    │
    ├── Converter/                   ← IValueConverter implementations (XAML binding)
    │
    └── Core/                        ← (Reserved — empty folder for future shared utilities)
```

---

# 👾 Features

## ✅ Completed Functions

- Account Login/Registration
- User Information Management
- Data Search
- Main Data CRUD
- User Permissions
- Responsive UI
- Database Connection
- Input Data Validation

---

## ❌ Uncompleted Functions

- None

---

## ⭐ Functions That Have Been Invested in Significant Research Time

> The following functions have been extensively researched and implemented by the team, and we hope the instructor will consider awarding points:

- System Architecture Design using MVVM Model

- User Interface and User Experience Optimization (UI/UX)

- Exception Handling

- Database Integration and Query Optimization

- Responsive Layout on Multiple Screen Sizes

- Researching Docker/CI-CD/ Authentication (if applicable)

---

# 🎥 Demo

Watch the demo video here:

[▶️ Click to watch demo](https://your-demo-link-here.com)

---

# 📊 Member Self-Assessment

| Member | Contribution Level | Self-Evaluation Score |
|:---:|:---:|:---:|
| Tran Khon Chi | 100% | 10 |
| Pham Thanh Dat | 100% | 10 |
| Mai Xuan Hung | 100% | 10 |
| Nguyen Van Minh | 100% | 10 |
| Giao Thai Bao | 100% | 10 |

---

# 📜 License

This project is developed for educational purposes only.

Copyright © 2026  
All rights reserved by the project team.

This software may not be copied, modified, or distributed for commercial purposes without permission from the authors.

---