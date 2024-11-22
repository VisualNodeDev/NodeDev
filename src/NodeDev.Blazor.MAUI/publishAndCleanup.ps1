
dotnet publish -c Release --runtime win-x64

# Remove all folders except wwwroot and en-us
# Set array of folders to keep

$foldersToKeep = @("wwwroot", "en-us")

# Get all folders in the publish folder
$folders = Get-ChildItem -Path .\bin\Release\net9.0-windows10.0.19041.0\win-x64\publish -Directory

# Delete folders that are not in the foldersToKeep array
foreach ($folder in $folders) {
	if ($foldersToKeep -notcontains $folder.Name) {
		Write-Host "Deleting folder: $($folder.FullName)"
		Remove-Item -Path $folder.FullName -Recurse -Force
	}
}