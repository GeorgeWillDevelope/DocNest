# DocNest

## Description

This is a file management application built using React.js for the frontend and ASP.NET Core for the backend. The application allows users to upload, download, and manage various file types, including text, PDFs, images, Word documents, and Excel sheets.

## Prerequisites

Ensure you have the following prerequisites installed before running the application:

- [Node.js](https://nodejs.org/)
- [npm](https://www.npmjs.com/)
- [.NET SDK](https://dotnet.microsoft.com/download)
- [PostgreSQL](https://www.postgresql.org/) database

## Features

- **File Upload:** Users can easily upload multiple files, including text documents, PDFs, images, Word documents, and Excel sheets.

- **File Download:** Download files with a simple click, ensuring a smooth user experience.

- **File Type Filtering:** The application filters files based on type, allowing users to quickly locate specific file types.

- **Thumbnail Generation:** Thumbnails are automatically generated for image files, providing a visual preview of the content.

- **Secure and Efficient:** The application ensures secure file handling and leverages ASP.NET Core for efficient server-side processing.

## Getting Started

### Running the Application

1. Install dependencies:

   ```bash
   npm install

2. Set up the database:

```bash
# Replace the placeholders with your PostgreSQL credentials
dotnet ef database update

3. Start the application:

```bash
# Replace the placeholders with your PostgreSQL credentials
dotnet run

## Testing
Run tests with:

```bash
npm test
Architecture and Design Decisions
The project uses React.js for the frontend, ASP.NET Core for the backend, and PostgreSQL as the database. The application employs a modular and scalable architecture to handle various file types efficiently.

Improvements
While the current implementation covers all the specified requirements, potential improvements include:

Enhanced user interface for a more intuitive experience.
Additional file type support.
Integration of cloud storage for scalability.
...
Feel free to contribute and suggest improvements!

License
This project is licensed under the MIT License.
