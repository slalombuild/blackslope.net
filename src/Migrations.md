# Entity Framework Migrations

https://entityframeworkcore.com/migrations

## Description

This is a tool with two halves: 
 1. a command line tool - super helpful in CI contexts - `dotnet-ef`
 2. an installed module - very helpful in development contexts - `Microsoft.EntityFrameworkCore.Migrations`

## Adding a migration

https://entityframeworkcore.com/migrations#add-migration
```
PM> Add-Migration MigrationName
```
or
```
> dotnet ef migrations add MigrationName
```

This will produce a file called `YYYYMMDDHHmmSS_MigrationName.cs` in `./Repositories/Migrations` which contains a programmatic version of the database contents.

Feel free to modify this file, and then use the "Apply changes" steps below to update the database.

## Apply changes

Now apply it to the database!
```
PM> Update-Database
```
or
```
> dotnet ef database update
```

## Roll backward

Normally, this is not something you would want to manually invoke, but it IS possible:
```
Remove-Migration; Update-Database LastGoodMigration
```
or
```
> dotnet ef migrations remove
```

## Export database contents

If you ever need a raw T-SQL script that can be used to seed a database that's based off migrations, then you can also use EFCore Migrations for that too!
```
> dotnet ef migrations script -o ./mig.sql
```