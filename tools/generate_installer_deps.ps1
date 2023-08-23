# Why not
Write-Host
"                                s*rss
                                rr^r)V
                             h{(?*r^rr
                           3Tv))Y\r^rr
                         V]v))))(?r^^**
                       V/())))))(|*^^rr
           [[[[[[   Fe(())]\(\])(|*^^rr
         [        1\/)))(\_ [ ma[?r^^rr
                   ?()))^   [ [ [rr^rxK
                   ,())()        +r^rr
      ee   [[      ,()))(*      _r^r11
222Ke((){!        '(()))))(r`"[[~rrrrv
rrr]((())(V      ,(()))))))(((rrr^rr
*r^rrv[())(((ar+()))))))))((|rr^r**
 rrr^r(/((())))))))))))((\|*rr^r2s
  srrr^r|*||\(()))))((V\Trrr^rr?G
    s*rr^^rrrrrr Z)rrL2rr^^r*?I
      Zq**rr^^^^^^^^^^^rr(|2o
         oI)rrrr^^^rrr*ssf
         
LemonLoader Installer Dependency Generator v0.1
         "

# Get MSBuild path
$allargs = @("-latest", "-requires Microsoft.Component.MSBuild", "-find MSBuild\**\Bin\MSBuild.exe")
Start-Process -FilePath "./vswhere.exe" -ArgumentList $allargs -Wait -NoNewWindow -RedirectStandardOutput vswhereoutput.txt
$msbuild = Get-Content vswhereoutput.txt
Remove-Item vswhereoutput.txt
Write-Host "MSBuild: $msbuild"

# Initialize folder
Write-Host "Initializing installer_deps folder..."

if (Test-Path "installer_deps") {
  Remove-Item -LiteralPath "installer_deps" -Force -Recurse
}

New-Item -ItemType Directory -Name "installer_deps"
New-Item -ItemType Directory -Name "installer_deps\assembly_generation"
New-Item -ItemType Directory -Name "installer_deps\core"
New-Item -ItemType Directory -Name "installer_deps\support_modules"
Copy-Item -Path "installer_deps_required\*" -Destination "installer_deps" -Recurse


# Compile MelonLoader
$baseMlPath = (Get-Item $pwd ).parent.FullName
Write-Host "BaseDirectory: $baseMlPath`n"
& $msbuild @("$baseMlPath/MelonLoader.sln", "/t:Rebuild", "/p:Configuration=Release", "/p:Platform=`"Android - ARM64`"")

Write-Host "`nCopying compiled files..."

# Copy Il2CppAssemblyGenerator
Copy-Item `
    -Path "$baseMlPath\Output\Release\Android\MelonLoader\Dependencies\Il2CppAssemblyGenerator\*.dll" `
    -Destination "installer_deps\assembly_generation"

# Copy MelonLoader
Copy-Item `
    -Path "$baseMlPath\Output\Release\Android\MelonLoader\*.dll" `
    -Destination "installer_deps\core"

# Copy Support Modules
Copy-Item `
    -Path "$baseMlPath\Output\Release\Android\MelonLoader\Dependencies\SupportModules\*.dll" `
    -Destination "installer_deps\support_modules"

Write-Host "Done`n"
Write-Host "Building libmain..."

# Build libmain
Start-Process -FilePath "$baseMlPath\libmain\build.bat" -ArgumentList "auto" -Wait -WorkingDirectory "$baseMlPath\libmain\"

# Copy libmain
Copy-Item `
    -Path "$baseMlPath\libmain\jniLibs\arm64-v8a\libmain.so" `
    -Destination "installer_deps\native"

Write-Host "Done`n"
Write-Host "Building Bootstrap..."

# Build Bootstrap
# TODO: find a way to not reuse CLion's build directory, idk why generating my own causes errors
$bsBuildDir = "../Bootstrap/android-build"
Start-Process -FilePath "bash.exe" -ArgumentList @("-c `"cd $bsBuildDir && dos2unix ../tools/build.sh`"") -Wait -NoNewWindow
Start-Process -FilePath "bash.exe" -ArgumentList @("-c `"cd $bsBuildDir && dos2unix ../tools/cmake_wrapper.sh`"") -Wait -NoNewWindow
Start-Process -FilePath "bash.exe" -ArgumentList @("-c `"cd $bsBuildDir && ../tools/build.sh`"") -Wait -NoNewWindow

# Copy Native Modules
Copy-Item `
    -Path "$baseMlPath\Bootstrap\android-build\libBootstrap.so" `
    -Destination "installer_deps\native"
Copy-Item `
    -Path "$baseMlPath\Bootstrap\android-build\capstone\libcapstone.so" `
    -Destination "installer_deps\native"
Copy-Item `
    -Path "$baseMlPath\Bootstrap\android-build\dobby\libdobby.so" `
    -Destination "installer_deps\native"
#Copy-Item `
#    -Path "$baseMlPath\Bootstrap\android-build\funchook\libfunchook.so" `
#    -Destination "installer_deps\native"

Write-Host "Done`n"
Write-Host "Compressing to ZIP..."

# Compress	
#Compress-Archive -Path ./installer_deps/* -CompressionLevel Optimal -DestinationPath ./installer_deps.zip
Start-Process -FilePath "C:\Program Files\7-Zip\7zG.exe" -ArgumentList @("a", "installer_deps.zip", "$baseMlPath\tools\installer_deps\*") -Wait

Write-Host "Done`n"
Write-Host "Cleaning up..."

# Cleanup
Remove-Item -LiteralPath "installer_deps" -Force -Recurse

Write-Host "Done, final zip was written to ./installer_deps.zip"