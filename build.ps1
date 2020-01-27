Write-Host -n "Building exe for win-x64..."

dotnet publish "src/SqlInliner.csproj" -c Release -o "bin/win-x64" -r "win-x64" > $null

rm -Force "bin/win-x64/*.pdb"

cp "bin/win-x64/SqlInliner.exe" "tests"

Write-Host "done!"
