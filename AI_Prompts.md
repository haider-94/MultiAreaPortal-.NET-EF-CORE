# AI Prompts

This file records the prompts used to build the MultiAreaPortal project with Claude Code.

---

## Prompt 1 — Initial project request

> Create a new .NET MVC event management portal with following overview
>
> In this comprehensive mini-project, you'll build a real-world, role-based .NET MVC Event Management Portal with three distinct areas (Admin, Customer, and Public) using ASP.NET Core Identity for authentication and authorization. You'll implement hybrid data access: Entity Framework Core for the event management CRUD in the Admin area, and raw ADO.NET for high-performance reporting in the Customer area. The entire portal will use SQL Server as its data store.
>
> The assignment mirrors a common enterprise scenario where an organisation needs a staff-facing admin panel to manage events and a customer-facing dashboard that queries the database directly for critical statistics – all while keeping a public area accessible to anonymous visitors.
>
> Create a new db in docker we already have downloaded Microsoft's official SQL Server 2022 container image to run in the docker so use it to create the instance and db.

---

## Prompt 2 — Project name

> Name the project MultiAreaPortal now continue

---

## Prompt 3 — Scope

> I will test the entire flow on my own, just complete the project architecture structure

---

## Prompt 4 — Documentation

> create an AI_Prompts.md file and add the prompts written in the chat

---

## Prompt 5 — Git baseline

> Run git init and just commit that code generated in the response of this command dotnet new mvc -n MultiAreaPortal.

---

## Prompt 6 — Gap analysis against the assignment

> now this is the detail of the task look into it step by step and have we missed anything from the given details. And whats left. [full assignment brief pasted]

---

## Prompt 7 — Alignment decision

> We have to follow the assignment

Chosen direction: rebuild on the exact `dotnet new mvc -au Individual` scaffold and match the
literal spec — Event fields `Name`/`TicketPrice`/`EventDate`, Customer dashboard as pure ADO.NET
(count + TOP 5 recent), Customer area restricted to the `Customer` role.

---

## Prompt 8 — Concept clarifications (asked during the rebuild)

> where razor pages is mentioned in the assignment?

> if we use Razor pages then what about areas? And also define what are Razor pages

(Answered: Razor Pages is the page-based model used by the built-in Identity UI that the
`-au Individual` template pulls in; MVC Areas and Razor Pages coexist — the Identity UI lives in
its own `Areas/Identity/Pages/…` area while Admin/Customer/Public stay pure MVC.)

---

## Prompt 9 — Deleted scaffold views

> why these files are deleted? index.cshtml and Privacy.cshtml?

(Explained: the root `Home/Index` and `Home/Privacy` views were removed because the root
`HomeController` now redirects to the Public area and the Privacy action was dropped.)

---

## Prompt 10 — Proceed with the rebuild

> yes go ahead with the rebuild

---

## Prompt 11 — Area Models folder / acceptance criterion 2

> I am checking project structure against acceptance criteria points and check does it fulfill this second point
>
> Three areas (Admin, Customer, Public) exist and follow the standard MVC area pattern.
>
> We do not have models folder inside all three areas Admin, Customer, Public why?

---

## Prompt 12 — Session-aware UI + logout button

> I have just register an account and I am seeing this UI, if there is a valid session should we show the login and register buttons? And there is no logout button add that as well.
>
> (with screenshot of the landing page)

(Fixed: `_LoginPartial` used `text-dark` on a dark navbar so the Logout button was invisible —
changed to `text-light`; and the Public landing now hides Login/Register when signed in and shows
a role-aware "Go to my area" link instead.)

---

## Prompt 13 — Verify role-based authorization

> Are we fulfilling this point?
>
> The Admin and Customer roles exist, and role-based authorization is correctly enforced: unauthenticated users are redirected to login when accessing Admin or Customer areas; Admin can access /Admin/Events but not /Customer/Dashboard (unless also in Customer role); Customer can access /Customer/Dashboard but not /Admin/Events.

---

## Prompt 14 — How to assign roles

> how can I assing Admin customer role

---

## Prompt 15 — Access denied after adding a role via SQL

> I ran this sql query to add customer role for admin as well but still getting access denied.
> [INSERT INTO AspNetUserRoles ... admin@portal.com / Customer]

(Explained: roles are cached in the auth cookie; the user must log out and back in for the new
role to take effect. Verified a fresh admin login then reaches both areas.)

---

## Prompt 16 — Verify ADO.NET dashboard criterion

> are we fulfilling this check point
>
> Customer area dashboard displays event count and recent events using ADO.NET raw SQL (no EF Core usage in that controller, not even via mixed context).

---

## Prompt 17 — Verify SQL Server persistence criterion

> Data is persisted in SQL Server (not in-memory). Migrations are applied, and the schema is generated correctly.
>
> Now verify this point

---

## Prompt 18 — Documentation

> Add rest of the prompts in the .md file

---

## Representative "vibe coding" prompts used during development

- "Scaffold an ASP.NET Core MVC app with Individual auth and convert it from SQLite to SQL Server (swap the EF provider, connection string, and regenerate the migration)."
- "Write a raw ADO.NET repository/controller action that runs `SELECT COUNT(*)` and `SELECT TOP 5 … ORDER BY CreatedAt DESC` with SqlConnection/SqlCommand/SqlDataReader — no EF Core."
- "Seed Admin and Customer roles plus one test user each in a startup seeding method using RoleManager/UserManager."
- "Override the Identity Register page so new sign-ups are added to the Customer role and signed in."
- "Add the `{area:exists}` route and role-aware navigation links in _Layout that only show for the matching role."
