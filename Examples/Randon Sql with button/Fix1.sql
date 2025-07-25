--Use this structure if you need to reference the variable
DECLARE @Name@ varchar(50)
DECLARE @MemberNumberVariable varchar(50) = '{Name}'

select data as [Filenames], data as [Filenames2], data as [Filenames3], data as [Filenames4], data as [Filenames5] from PK WHERE data like '%{Name}%'
GO
