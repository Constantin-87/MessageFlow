# **MessageFlow**

**MessageFlow** is a microservices-based omnichannel messaging platform that enables businesses to manage and automate customer conversations across multiple channels, including **Facebook Messenger**, **WhatsApp**, and other integrations to come soon.  

The application is designed with scalability, maintainability, and security in mind, using **.NET 9**, **Blazor WebAssembly**, **SignalR**, **Azure services**, and **MediatR** for CQRS-based command/query handling.  

---

## **Features**

### **Multi-channel Messaging**
- Send and receive messages from Facebook Messenger and WhatsApp.
- Real-time updates using SignalR WebSockets.

### **User Management**
- Role-based access control (**SuperAdmin**, **Admin**, **Manager**, **Agent**).
- Company-based user scoping and permissions.

### **Team & Organization Management**
- Create, update, and delete organizations, teams, and members.
- Manage contact points like emails and phone numbers.

### **AI Integration**
- Uses Azure OpenAI and Azure AI Search for automated responses and contextual data retrieval.
- Pretraining data file upload, processing, and indexing for AI chat capabilities.

### **Security**
- JWT-based authentication.
- CSRF protection, CSP compliance, and secure headers.

---

## **Architecture**

**Gateway API** – Entry point for the client application, routing requests to respective services.  

**Microservices:**
- `MessageFlow.Identity` – Authentication & authorization, identity management.
- `MessageFlow.User` – User profiles and company association.
- `MessageFlow.Organization` – Organization and team management.
- `MessageFlow.Server` – Work in proggress to be refactored down to microservices.

**Azure Services:**
- Azure Blob Storage – File storage.
- Azure Cognitive Search – AI search indexing.
- Azure OpenAI – Natural language processing.

---

## **Tech Stack**
- **Backend:** ASP.NET Core 9, MediatR, Entity Framework Core, AutoMapper, SignalR
- **Database:** Azure SQL Database
- **Hosting:** Azure VM with IIS
- **Messaging:** Planned Azure Service Bus
- **Authentication:** JWT, ASP.NET Identity

---

## **Getting Started**

### **Prerequisites**
- .NET 9 SDK
- Azure account with access to the key vault

### **Installation**

**Step 1:** Clone the repository  
- `git clone https://github.com/yourusername/messageflow.git`  
- `cd messageflow`  

**Step 2:** Log in to Azure  
- Run in **Command Prompt**:  
  az login  
  *(This will open a browser window — log in using your Azure credentials.)*  

**Step 3:** Retrieve your Azure user object ID  
- Run:  
  az ad signed-in-user show --query id --output tsv  

**Step 4:** Assign Key Vault role  
- Replace `XXXXX-XXXXX-XXXX` with your Azure account ID from the previous step, then run:  
  az role assignment create --role "Key Vault Secrets Officer" --assignee-object-id "XXXXX-XXXXX-XXXX" --scope "/subscriptions/57e8e112-b730-46b6-b986-7570412a9ea5/resourceGroups/ResourceGroup-MF/providers/Microsoft.KeyVault/vaults/KeyVault-MF"  

**Step 5:** Set environment variables  
- Run in **Command Prompt as Administrator**:  
  setx KEY_VAULT_NAME "KeyVault-MF" /M  

**Step 6:** Configure service-specific settings  
- Each service has its own `appsettings.Development.json`.

**Step 7:** Run database migrations if running a local db(example for Organization service)  
- `dotnet ef database update --project MessageFlow.Organization.Infrastructure`


Run services:

dotnet run --project MessageFlow.Identity
dotnet run --project MessageFlow.User
dotnet run --project MessageFlow.Organization

## **Future Roadmap**
- Event-driven service communication with **Azure Service Bus**.
- Additional channel integrations (**SMS**, **WebChat**, **Email**, **Instagram**).
- Enhanced analytics and reporting dashboards.
- Mobile client application.

---

## **License & Intellectual Property**
**Copyright © 2025 [Constantin Gavrila]. All Rights Reserved.**

This repository and its contents are **proprietary**.  
No part of this project, including but not limited to source code, designs, and documentation, may be copied, modified, distributed, or used for commercial purposes without prior written permission from the copyright owner.

**Contributors** agree that all contributions become the property of the copyright owner and grant the owner a **perpetual, worldwide, royalty-free license** to use, modify, and distribute their contributions as part of the project.
