# Windows Powershell에서 폴더 내 bmp 파일 이름을 0부터 개수만큼 +1 한 이름들로 바꾸는 커맨드 라인
## -> Dummy Camera 테스트 용 폴더 및 파일 생성 시 유용

$i = 0
Get-ChildItem -File | Sort-Object Name | ForEach-Object {
    Rename-Item -LiteralPath $_.FullName -NewName ("__TMP_{0:D6}.tmp" -f $i)
    $i++
}

$i = 0
Get-ChildItem -File -Filter "__TMP_*.tmp" | Sort-Object Name | ForEach-Object {
    Rename-Item -LiteralPath $_.FullName -NewName ("{0}.bmp" -f $i)
    $i++
}