# AI-Powered Conversational SQL Agent

[![License: MIT](https://img.shields.io/badge/License-MIT-yellow.svg)](https://opensource.org/licenses/MIT)

An advanced, stateful AI agent that translates natural language into T-SQL queries through a conversational interface. This project is a complete, end-to-end solution demonstrating how to build a professional-grade AI application using a modern .NET Clean Architecture backend and a React frontend.

The agent maintains conversational context, understands follow-up questions, and dynamically reads your live database schema to generate accurate and relevant SQL queries.

---

### ✨ Key Features

*   **Natural Language to SQL:** Ask complex questions in plain English and get back ready-to-use T-SQL queries.
*   **Stateful Conversation:** The agent remembers the context of your conversation. You can ask it to refine or modify the previous query it generated.
*   **Dynamic Schema Awareness:** On startup, the application connects to your live SQL Server database to read the schema, including tables, columns, data types, and even primary/foreign key relationships. The AI's knowledge is always up-to-date.
*   **Persistent History:** Chat history is saved to the browser's `localStorage`, so conversations are retained even after a page refresh.
*   **Clean Architecture Backend:** The .NET backend is structured using Clean Architecture principles, CQRS with MediatR, and the Chain of Responsibility pattern for robust, scalable, and testable code.
*   **Modern UI:** A sleek, responsive frontend built with React, featuring a dark theme inspired by professional developer tools.

---

### 🏛️ Architectural Overview

This project is a practical implementation of professional software design patterns.

1.  **Clean Architecture:** The solution is divided into separate layers (`Domain`, `Application`, `Infrastructure`, `Presentation/Web`), ensuring a clear separation of concerns. The core rule is that dependencies only point inwards, making the business logic independent of UI or database concerns.
2.  **CQRS with MediatR:** Every request to the application is treated as a Command. This decouples the API controllers from the business logic handlers, leading to cleaner and more focused code.
3.  **Chain of Responsibility:** Implemented via MediatR Pipeline Behaviors. We use a `ValidationBehavior` to ensure every command passes a validation checkpoint before it's processed, making the system more robust.
4.  **Repository Pattern:** The `SchemaRepository` abstracts away the data access logic for retrieving the database schema, making it easy to swap out the data source if needed.
5.  **Stateful AI with Assistants API:** We leverage the OpenAI Assistants API to manage conversation state. The backend creates a persistent Assistant once, and a new `Thread` is created for each conversation, which is the key to the agent's "memory."

---

### 💻 Technology Stack

| Backend (.NET 8) | Frontend (React) |
| :--- | :--- |
| ASP.NET Core Web API | React 18 (Create React App) |
| MediatR (for CQRS) | `useState`, `useEffect`, `useRef` |
| FluentValidation | `fetch` API for HTTP requests |
| Dapper (for Schema Reading) | CSS for styling |
| `Microsoft.Data.SqlClient` | |
| OpenAI Assistants API | |

---

### 🚀 Getting Started

Follow these steps to get the project running on your local machine.

#### Prerequisites

*   [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)
*   [Node.js and npm](https://nodejs.org/en/)
*   An accessible SQL Server instance.
*   An OpenAI API Key.

#### **Backend Setup (`SqlAgentApi.Web`)**

1.  **Clone the repository:**
    ```bash
    git clone <your-repo-url>
    cd <your-repo-folder>
    ```

2.  **Configure `appsettings.json`:**
    Open `src/SqlAgentApi.Web/appsettings.json` and fill in your details:
    *   **`ConnectionStrings.DefaultConnection`**: Your full connection string for the SQL Server database you want the agent to read.
    *   **`OpenAIApiKey`**: Your secret key from the OpenAI Platform.
    *   Leave `OpenAIAssistantId` empty for the first run.

3.  **Generate the Assistant ID (First Run Only):**
    This is a crucial one-time setup step.
    a. Run the backend API from your IDE or terminal:
       ```bash
       cd src/SqlAgentApi.Web
       dotnet run
       ```
    b. Watch the console output. The application will connect to your database, read the schema, and create a new Assistant. It will then print a message in green.
    c. **Copy the `asst_...` ID** from the console.
    d. **Stop the application** (Ctrl + C).
    e. **Paste the copied ID** into the `OpenAIAssistantId` field in `appsettings.json`.

4.  **Run the Backend:**
    Run the application again. This time, it will find and reuse your permanent Assistant.
    ```bash
    dotnet run
    ```
    The backend is now running, typically on `https://localhost:7123`.

#### **Frontend Setup (`sql-agent-ui`)**

1.  **Navigate to the UI directory:**
    Open a **new terminal** and navigate to the React app's folder:
    ```bash
    cd src/sql-agent-ui 
    ```

2.  **Install dependencies:**
    ```bash
    npm install
    ```

3.  **Run the frontend:**
    ```bash
    npm start
    ```
    This will open the application in your browser, usually at `http://localhost:3000`.

> **Note:** Ensure the `API_URL` constant in `src/sql-agent-ui/src/App.js` matches the port your backend is running on.

---

### 📖 How to Use

1.  The application opens to a welcome screen.
2.  Type a question in the input box (e.g., "show me the top 5 users") and press Enter or click the send button.
3.  The agent will generate and display the T-SQL query.
4.  Ask a follow-up question (e.g., "now add their email address"). The agent will use the conversation history to refine the previous query.
5.  Click the `+` button in the history panel to start a new, separate conversation.
6.  Click on any past conversation in the history panel to view its contents. The history persists even if you refresh the page.

---

### 📄 Related Blog Post

For a detailed walkthrough of the journey and the architectural decisions behind this project, check out my LinkedIn article:

[**Peeling the AI Onion: My Mind-Blowing Leap from Stateless to Stateful with the Assistants API**](<https://www.linkedin.com/pulse/peeling-ai-onion-my-mind-blowing-leap-from-stateless-stateful-garg-ljxwc>)

---

### ⚖️ License

This project is licensed under the MIT License. See the [LICENSE](LICENSE.md) file for details.

---

### 👤 Author & Contact

This project was built by **[Your Name]**. I'm passionate about building professional, scalable applications with modern .NET and AI.

I share my learning journey, tutorials, and deep dives into software architecture on my YouTube channel. If you found this project helpful, consider checking it out!

*   📺 **YouTube:** [Link to Your YouTube Channel](https://www.youtube.com/c/nitin-garg)
*   💼 **LinkedIn:** [Link to Your LinkedIn Profile](https://www.linkedin.com/in/nitin-grg/)
*   🐙 **GitHub:** [Link to Your GitHub Profile](https://github.com/nitingrg18)
