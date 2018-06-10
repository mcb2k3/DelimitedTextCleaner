echo off
prompt $g
"C:\Program Files (x86)\Microsoft SDKs\Windows\v10.0A\bin\NETFX 4.6.2 Tools\gacutil.exe" /u  "Sql2Go.DelimitedTextCleaner"
"C:\Program Files (x86)\Microsoft SDKs\Windows\v10.0A\bin\NETFX 4.6.2 Tools\gacutil.exe" /i  "C:\Users\Megan Brooks\Source\Repos\DelimitedTextCleaner\DelimitedTextCleaner\bin\Release\Sql2Go.DelimitedTextCleaner.dll"
rem copy "C:\Users\Megan Brooks\Source\Repos\Proximity\Proximity\GedcomReader\bin\Release\Sql2Go.GedComreader.pdb" "C:\Windows\assembly\WhoKnowsWhat"
pause