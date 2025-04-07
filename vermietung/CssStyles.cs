namespace CarRentalHttpServer
{
    public static class CssStyles
    {
        public static string GetStyles()
        {
            return @"
:root {
    --primary: #2563eb;
    --secondary: #1e40af;
    --background: #f8fafc;
    --card: #ffffff;
    --text: #1e293b;
    --border: #e2e8f0;
    --light-bg: rgba(0, 0, 0, 0.02);
    --hover-bg: #f1f5f9;
    --muted-text: #64748b;
}

/* Base Styles */
* {
    margin: 0;
    padding: 0;
    box-sizing: border-box;
    font-family: 'Segoe UI', system-ui, sans-serif;
}

html {
    overflow-y: scroll; /* Always show scrollbar to prevent layout shifts */
    scrollbar-width: thin; /* For Firefox */
}

/* Webkit scrollbar styling */
::-webkit-scrollbar {
    width: 8px;
}

::-webkit-scrollbar-track {
    background: var(--background);
}

::-webkit-scrollbar-thumb {
    background: var(--border);
    border-radius: 4px;
}

::-webkit-scrollbar-thumb:hover {
    background: var(--muted-text);
}

body {
    background-color: var(--background);
    color: var(--text);
    line-height: 1.5;
    padding: 20px;
}

.container {
    max-width: 1200px;
    margin: 0 auto;
}

/* Typography */
h1 {
    font-size: 2rem;
    font-weight: 600;
}

/* Layout Components */
header {
    background: linear-gradient(135deg, var(--primary), var(--secondary));
    color: white;
    padding: 2rem;
    border-radius: 8px;
    margin-bottom: 2rem;
    box-shadow: 0 4px 6px -1px rgba(0, 0, 0, 0.1);
}

.card, .stat-card, .rental-card, .summary {
    background: var(--card);
    border-radius: 8px;
    border: 1px solid var(--border);
    box-shadow: 0 1px 3px 0 rgba(0, 0, 0, 0.1);
}

.card {
    padding: 1.5rem;
    margin-bottom: 1rem;
}

/* Tables */
.responsive-table {
    overflow-x: auto;
    margin: 1.5rem 0;
    border-radius: 8px;
    box-shadow: 0 1px 3px 0 rgba(0, 0, 0, 0.1);
}

table {
    width: 100%;
    border-collapse: collapse;
    margin: 1rem 0;
    background: white;
}

th, td {
    padding: 12px 16px;
    text-align: left;
    border-bottom: 1px solid var(--border);
}

th {
    background-color: var(--primary);
    color: white;
    font-weight: 500;
    position: sticky;
    top: 0;
}

tr:nth-child(even) {
    background-color: var(--light-bg);
}

tr:hover {
    background-color: var(--hover-bg);
}

.date-cell {
    font-family: monospace;
    white-space: nowrap;
}

.price-cell {
    font-weight: 600;
    color: var(--primary);
}

/* Forms */
.form-group {
    margin-bottom: 1.5rem;
}

label {
    display: block;
    margin-bottom: 0.5rem;
    font-weight: 500;
}

input, select {
    width: 100%;
    padding: 0.75rem;
    border: 1px solid var(--border);
    border-radius: 6px;
    font-size: 1rem;
}

/* Buttons */
.btn {
    display: inline-block;
    background: var(--primary);
    color: white;
    padding: 0.75rem 1.5rem;
    border-radius: 6px;
    text-decoration: none;
    font-weight: 500;
    transition: all 0.2s;
    border: none;
    cursor: pointer;
}

.btn:hover {
    background: var(--secondary);
    transform: translateY(-1px);
}

.btn.large {
    padding: 1.5rem;
    font-size: 1.1rem;
    display: flex;
    align-items: center;
    justify-content: center;
    gap: 0.75rem;
}

.btn.secondary {
    background: white;
    color: var(--primary);
    border: 2px solid var(--primary);
}

.btn.secondary:hover {
    background: #dbeafe;
}

/* Navigation */
.nav {
    display: flex;
    gap: 1rem;
    margin: 1rem 0;
}

/* Navigation Links */
.nav a {
    color: white;
    text-decoration: none;
    transition: all 0.2s;
    padding: 0.5rem 1rem;
    border-radius: 4px;
    background-color: var(--primary);
}

.nav a:visited {
    color: #e0f2fe;
    background-color: var(--secondary);
}

.nav a:hover {
    transform: translateY(-2px);
    box-shadow: 0 2px 4px rgba(0, 0, 0, 0.1);
}

.nav a:active {
    color: white;
    background-color: #1d4ed8;
    transform: translateY(1px);
}

/* Notification Components */
.success-message {
    background: #dcfce7;
    color: #166534;
    padding: 1rem;
    border-radius: 6px;
    margin: 1rem 0;
}

.notice {
    padding: 1rem;
    background: #f1f5f9;
    border-radius: 6px;
    text-align: center;
}

/* Dashboard Components */
.stats-grid {
    display: grid;
    grid-template-columns: repeat(auto-fit, minmax(250px, 1fr));
    gap: 1.5rem;
    margin: 2rem 0;
}

.stat-card {
    padding: 1.5rem;
    text-align: center;
}

.stat-card h3 {
    font-size: 2.5rem;
    color: var(--primary);
    margin-bottom: 0.5rem;
}

.actions {
    margin: 3rem 0;
}

.action-buttons {
    display: grid;
    grid-template-columns: repeat(auto-fit, minmax(300px, 1fr));
    gap: 1.5rem;
    margin-top: 1.5rem;
}

.icon {
    font-size: 1.5rem;
}

/* Rental Components */
.rental-cards {
    display: grid;
    gap: 1rem;
    margin-top: 1.5rem;
}

.rental-card {
    display: flex;
    justify-content: space-between;
    align-items: center;
    padding: 1rem;
}

.car-model {
    font-weight: 600;
}

.customer, .summary-item span {
    color: var(--muted-text);
    font-size: 0.9rem;
}

.date-range {
    font-family: monospace;
}

.price {
    font-weight: 600;
    color: var(--primary);
}

.customer-badge {
    background: #e0f2fe;
    color: #0369a1;
    padding: 4px 8px;
    border-radius: 12px;
    font-size: 0.85rem;
}

/* Page Layout */
.page-header {
    display: flex;
    justify-content: space-between;
    align-items: center;
    margin-bottom: 1.5rem;
    flex-wrap: wrap;
    gap: 1rem;
}

.header-actions {
    display: flex;
    gap: 0.75rem;
}

/* Summary Section */
.summary {
    display: flex;
    gap: 2rem;
    margin-top: 1.5rem;
    padding: 1rem;
}

.summary-item {
    display: flex;
    align-items: center;
    gap: 0.75rem;
}

.summary-item strong {
    font-size: 1.1rem;
}

/* User Roles */
.role-admin {
    color: #0056b3;
    font-weight: bold;
}

.role-ban {
    color: #b30000;
    font-weight: bold;
}

/* Add spacing between form elements */
form input + button,
form select + button,
form textarea + button {
    margin-top: 1rem;
}

";
        }
    }
}