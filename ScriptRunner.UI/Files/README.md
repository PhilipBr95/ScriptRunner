## Overview

ScriptRunner allows DevOps to quickly deploy scripts to live for users to manually run.  

Scripts are stored in Packages; Packages can contain multiple SQL/PowerShell script files (.sql and .ps1 files).  
Packages are either self contained `Nuget Packages`, `Folders` or specially formatted `ScriptRunner` files.

## Nuget Packages

`Nuget Packages` are normally pushed to a remote repo [<strong>@Model.GitRepo</strong>] and then imported into ScriptRunner via the Admin page.  
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
            { "Name": "Name", "Type": "text", "Required": false, "Tooltip": "Their new Name" },
            { "Name": "DataFile", "Type": "file", "Data": {"FileType": ".csv"} },
            { "Name": "Title", "Type": "combo", "Data": {"Mr": "Mr", "Mrs": "Mrs", "Dr": "Dr"}, "Required": false }
        ],
        "Options": {
            "Layout": "mr",
            "DataTableDom": "t",
            "Css": [ "#resultsTable0 > tbody > tr > td { background-color: orange; cursor: copy; }" ],
            "JQuery": [{ "Parent": "#results", "Selector": "#resultsTable0 > tbody > tr > td:nth-child(1)", "Event": "click", "Function": "let $text = $(evt.target).text();  window.copyText($text, `${evt.data.script.id} ${$text} Copied!`);" }] 
        }
    }
 ```

 #### Config Properties

| Property   | Description|
| ---------- | ---------- |
|`AllowedADGroups`|An array of AD groups allowed to run the script|
|`ConnectionString`|(Optional) - Only required for SQL scripts. If it isn't provided, then the folder structure will be used to create the ConnectionString.<br />The folder structure for the ConectionString is `\Server\Database\Script.sql`|
|`Params`|Params must be populated by the user (unless optional - `"Required": false`), before execution.<br /><mark>Reference them by surrounding their name with curley brackets</mark>, eg, `{Name}`<table><tbody><tr><td>`Type`</td><td>Allowed Types: `text/string/varchar`, `number/int`, `checkbox`, `combo/select`, `datetime` and `file` - The file will be base64 encoded when presented to the script.</td></tr><tr><td>`Tooltip`</td><td>(Optional) - Include additional instructions</td></tr><tr><td>`Data`</td><td>(Optional) - A dictioary to provide additional config, eg FileType, combo values</td></tr><tr><td>`Required`</td><td>(Optional) - Whether the must be populated or not</td></tr></tbody></table>|
|`Options`|Options allow you to customise the UX<br /><table><tbody><tr><td>`Layout`</td><td>(Optional) - The layout of the Messages and Results<br />eg, `HRm` means Show the Results(with Headers) first and then the Messages (lowercase, meaning "without the label")</td></tr><tr><td>`DataTableDom`</td><td>(Optional) - The DataTable DOM to use</td></tr><tr><td>`Css`</td><td>(Optional) - A list of CSS's to apply</td></tr><tr><td>`JQuery`</td><td>(Optional) - A list of JQuery functions</td></tr></tbody></table>|

## Folders

Script `Folder` Packages need to be placed in <strong>@Model.ScriptFolder</strong>.  
Each subfolder is the same as a 'Nuget Package', so needs to contain a `config.json` (as above) and scripts.

## ScriptRunner Files

`ScriptRunner` files are SQL files with the config as comments.  Either create them and copy them to <strong>@Model.ScriptFolder</strong> or upload `.sql` files via the Admin page and it will convert them into `ScriptRunner` files.

##### Importing SQL Files

`.sql` files **must** contain the config as a comment at the top of the file (See the example below).  
The Import wizard on the Admin page will convert the `.sql` file into a `.srunner` file.  
Script variables ending with **@** will be populated with the equivalent Paremeter value.  
E.g., 

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

becomes

```sql
DECLARE @MemberNumber@ int = '{MemberNumber}'
DECLARE @MemberName@ varchar(255) = '{MemberName}' 
DECLARE @MemberDOB@ datetime = '{MemberDOB}'

select Concat('Hello ', @MemberName@, ' (', @MemberNumber@, ') You''re DOB is ', @MemberDOB@)
```

Note: You may notice that we're passing strings into numerics - this is fine as the javascript will protect us against datatype mismatches and SQL can handle it.

## Security
Access to the Admin page can be restricted via `appsettings` and access to the scripts are restricted by the `AllowedADGroups` property per script.