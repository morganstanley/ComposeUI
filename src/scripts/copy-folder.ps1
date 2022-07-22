param($source, $target)

if(Test-Path $target) {
  Remove-Item $target/* -Recurse
}
else {
	md $target
}

Copy-Item $source/* -Destination $target -Recurse