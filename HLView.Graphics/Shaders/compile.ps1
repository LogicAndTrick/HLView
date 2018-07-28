$fileNames = Get-ChildItem -Path $scriptPath -Recurse

foreach ($file in $fileNames)
{
    if ($file.Name.EndsWith(".vk.vert") -Or $file.Name.EndsWith(".vk.frag"))
    {
        Write-Host "Compiling $file"
        .\glslangvalidator -V $file -o $file".spv"
    }
    if ($file.Name.EndsWith(".frag.hlsl"))
    {
        Write-Host "Compiling $file"
        .\fxc /E main /T ps_5_0 $file /Fo $file".bytes"
    }
    if ($file.Name.EndsWith(".vert.hlsl"))
    {
        Write-Host "Compiling $file"
        .\fxc /E main /T vs_5_0 $file /Fo $file".bytes"
    }
}