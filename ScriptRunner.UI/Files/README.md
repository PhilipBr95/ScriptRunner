## Overview

ScriptRunner allows DevOps to quickly deploy scripts to live for users to manually run.  

Scripts are stored in Packages; Packages can contain multiple SQL/PowerShell script files (.sql and .ps1 extensions).  
Packages are either self contained `Nuget Packages`, `Folders` or specially formatted `ScriptRunner files`.

## Nuget Packages

Nuget Packages are normally pushed to a remote repo [<strong>@Model.GitRepo</strong>] and then imported into ScriptRunner via the Admin page.  
The Nuget Package must have the Tag <strong>@Model.Tag</strong> and contain a config.json and the actual scripts.


Example <a href="/files/MyApp_Fix_Name.1.1.3.nupkg">Nuget Package</a> and config.json:

```json
    {
        "System": "MyApp",
        "Description": "Fixes the member's Name",
        "ConnectionString": "Server=localhost;Database=Test;Trusted_Connection=True;",
        "Title": "Fix Member's Name",
        "Tags":["Member", "Name"],
        "Params": [
            { "Name": "MemberNumber", "Type": "number" },
            { "Name": "Name", "Type": "text", "Required": false, "Tooltip": "Their new Name" }
        ]
    }
 ```

 #### Config Properties

| Property   | Description|
| ---------- | ---------- |
|ConnectionString|Optional and only required for SQL scripts. If not provided, then the folder structure will be used to create the ConnectionString. <br />If both are provided, then the folder sturcture will override the ConnectionString.<br />The folder structure for the ConectionString is `\Server\Database\Script.sql`
|Params|Params must be populated by the user (unless optional - `"Required": false`) before execution.<br />Reference them by surrounding their name with curley brackets in the script files, eg `{Name}`<br /> Check <a href="https://www.w3schools.com/html/html_form_input_types.asp">Input Types</a> to see possible input types<br />Keep the `Name` short and simple.  Use the optional `Tooltip` property to add detail

<br />

## Folders

Script Packages need to be placed in <strong>@Model.ScriptFolder</strong>.  
Each subfolder is the same as a 'Nuget Package', so needs to contain a config.json (as above) and scripts.

## ScriptRunner Files

ScriptRunner files are SQL files with the config as comments.  
Example ScriptRunner file below (.srunner)
```sql
/*
{	
	"Id": "Package1",
	"Version": "1.0.0",
	"System": "MyApp", 
	"Description": "Random SQL file", 
	"ConnectionString": "Server=localhost;Database=Test;Trusted_Connection=True;",
	"Title": "Random SQL .package file", 
	"Tags":["Member", "Name"], 

	//Params are optional
	"Params": [
		{ "Name": "MemberNumber", "Type": "number", "Value": "1000", "Tooltip": "The MemberNumber" }, 
		{ "Name": "MemberName", "Type": "text", "Required": true, "Value": "Smith", "Tooltip": "Their new Name" },
		{ "Name": "MemberDOB", "Type": "datetime", "Required": false, "Value": "01/01/2000" }
	]
}
*/

DECLARE @MemberNumber@ int = 1000 
DECLARE @MemberName@ varchar(255) = 'Smith' 
DECLARE @MemberDOB@ datetime = '01/01/2000'

select Concat('Hello ', @MemberName@, ' (', @MemberNumber@, ') You''re DOB is ', @MemberDOB@)
```
