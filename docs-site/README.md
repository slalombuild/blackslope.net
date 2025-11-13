# VitePress Documentation Site

## Quick Start

### Prerequisites

- Node.js and npm installed

### Steps to Launch

1. **Navigate to the docs-site directory**

   ```powershell
   cd docs-site
   ```

2. **Install dependencies**

   ```powershell
   npm install
   ```

3. **Start the development server**

   ```powershell
   npm run docs:dev
   ```

4. **Access the site**

   - Open your browser to: <http://localhost:5173/>
   - The site will auto-reload when you make changes to documentation files

### Available Scripts

- `npm run docs:dev` - Start development server with hot reload
- `npm run docs:build` - Build the site for production
- `npm run docs:preview` - Preview the production build locally

### Development Notes

- Documentation files are located in the `docs/` directory
- VitePress configuration is in `docs/.vitepress/config.mts`
- The site includes plugins for Mermaid diagrams and LLM-friendly output
